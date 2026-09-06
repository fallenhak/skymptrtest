param(
    [ValidateRange(1, 2147483647)]
    [int]$ProfileId = 1,
    [string]$ServerAddress = '127.0.0.1',
    [ValidateRange(1, 65534)]
    [int]$ServerPort = 7777
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceLock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$artifactRoot = Join-Path $projectRoot $sourceLock.clientArtifact.cacheDirectory
$clientRoot = Join-Path (Join-Path $projectRoot $sourceLock.clientArtifact.profilesDirectory) "player-$ProfileId"
$builtClient = Join-Path $projectRoot 'build/dist/client/Data/Platform/Plugins/skymp5-client.js'
if (Test-Path -LiteralPath $clientRoot) { throw "Existing client profile was preserved: $clientRoot" }
if (-not (Test-Path -LiteralPath $builtClient -PathType Leaf)) { throw 'Build the client first with scripts/build-client.ps1.' }

$ghCommand = Get-Command gh.exe -ErrorAction Stop
$runId = $sourceLock.clientArtifact.runId
$runJson = & $ghCommand.Source api "repos/$($sourceLock.skymp.repository)/actions/runs/$runId"
if ($LASTEXITCODE -ne 0) { throw 'Could not verify the upstream client CI run.' }
$run = $runJson | ConvertFrom-Json
if ($run.head_sha -ne $sourceLock.skymp.commit -or $run.conclusion -ne 'success') {
    throw 'Client artifact CI run does not match the expected successful baseline.'
}
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    & $ghCommand.Source run download $runId --repo $sourceLock.skymp.repository --name $sourceLock.clientArtifact.name --dir $artifactRoot
    if ($LASTEXITCODE -ne 0) { throw 'Client artifact download failed; the pinned artifact may have expired.' }
}
$clientArtifact = Join-Path $artifactRoot 'client'
$metadata = Get-Content -LiteralPath (Join-Path $clientArtifact 'Data/Platform/Distribution/build-metadata-public.json') -Raw | ConvertFrom-Json
if ($metadata.commitSha -ne $sourceLock.skymp.commit) { throw 'Client artifact metadata does not match the pinned source.' }
foreach ($requiredFile in @('Data/SKSE/Plugins/SkyrimPlatform.dll', 'Data/SKSE/Plugins/MpClientPlugin.dll', 'Data/Platform/Distribution/RuntimeDependencies/SkyrimPlatformImpl.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $clientArtifact $requiredFile) -PathType Leaf)) { throw "Incomplete artifact: $requiredFile" }
}

$httpPort = if ($ServerPort -eq 7777) { 3000 } else { $ServerPort + 1 }
$serverHttpUrl = [System.UriBuilder]::new('http', $ServerAddress, $httpPort).Uri.GetLeftPart([System.UriPartial]::Authority)
New-Item -ItemType Directory -Path $clientRoot -Force | Out-Null
Get-ChildItem -LiteralPath $clientArtifact | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $clientRoot -Recurse
}
Copy-Item -LiteralPath $builtClient -Destination (Join-Path $clientRoot 'Data/Platform/Plugins/skymp5-client.js') -Force
$settings = [ordered]@{
    'gameData' = @{ profileId = $ProfileId }
    'master' = ''
    'server-ip' = $ServerAddress
    'server-port' = $ServerPort
    'server-http-url' = $serverHttpUrl
    'server-info-ignore' = $true
    'server-master-key' = $null
    'ignoreLoadOrderMismatch' = $false
}
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText((Join-Path $clientRoot 'Data/Platform/Plugins/skymp5-client-settings.txt'), ($settings | ConvertTo-Json), $utf8)
$provenance = [ordered]@{
    preparedAtUtc = [DateTime]::UtcNow.ToString('o')
    upstreamCommit = $sourceLock.skymp.commit
    nativeArtifactRunId = $runId
    clientScriptSha256 = (Get-FileHash -LiteralPath $builtClient -Algorithm SHA256).Hash
    profileId = $ProfileId
    serverHttpUrl = $serverHttpUrl
    status = 'Staged mod files only; SKSE, Address Library and game/UI compatibility still require validation'
}
[System.IO.File]::WriteAllText((Join-Path $clientRoot 'lab-provenance.json'), ($provenance | ConvertTo-Json), $utf8)
Write-Output "Client profile staged: $clientRoot"
Write-Output 'Game files were not changed. Run scripts/check-client-prerequisites.ps1 before game installation.'
