param(
    [Parameter(Mandatory = $true)]
    [string]$SkyrimDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceLock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$serverRoot = Join-Path $projectRoot $sourceLock.serverArtifact.runtimeDirectory
$artifactRoot = Join-Path $projectRoot $sourceLock.serverArtifact.cacheDirectory
$expectedCommit = $sourceLock.skymp.commit
$runId = $sourceLock.serverArtifact.runId

if (Test-Path -LiteralPath $serverRoot) {
    throw 'The local server directory already exists. Existing settings and world data were preserved.'
}

$ghCommand = Get-Command gh.exe -ErrorAction Stop
$nodeCommand = Get-Command node.exe -ErrorAction Stop
$gameDataRoot = Join-Path (Resolve-Path -LiteralPath $SkyrimDirectory).Path 'Data'
$masterFiles = @('Skyrim.esm', 'Update.esm', 'Dawnguard.esm', 'HearthFires.esm', 'Dragonborn.esm')
$loadOrder = @($masterFiles | ForEach-Object {
    $masterPath = Join-Path $gameDataRoot $_
    if (-not (Test-Path -LiteralPath $masterPath -PathType Leaf)) {
        throw "Missing game data: $masterPath"
    }
    $masterPath.Replace('\', '/')
})

$runJson = & $ghCommand.Source api "repos/$($sourceLock.skymp.repository)/actions/runs/$runId"
if ($LASTEXITCODE -ne 0) { throw 'Could not read the upstream CI run. Check gh auth status.' }
$run = $runJson | ConvertFrom-Json
if ($run.head_sha -ne $expectedCommit -or $run.conclusion -ne 'success') {
    throw 'The CI run does not match the expected successful source revision.'
}

if (-not (Test-Path -LiteralPath $artifactRoot)) {
    & $ghCommand.Source run download $runId --repo $sourceLock.skymp.repository --name $sourceLock.serverArtifact.name --dir $artifactRoot
    if ($LASTEXITCODE -ne 0) { throw 'Artifact download failed. The pinned CI artifact may have expired.' }
}

foreach ($requiredFile in @('scam_native.node', 'dist_back/skymp5-server.js', 'dist_back/skymp5-server.js.map')) {
    if (-not (Test-Path -LiteralPath (Join-Path $artifactRoot $requiredFile) -PathType Leaf)) {
        throw "The cached artifact is incomplete: $requiredFile"
    }
}

$nativeHash = (Get-FileHash -LiteralPath (Join-Path $artifactRoot 'scam_native.node') -Algorithm SHA256).Hash
if ($nativeHash -ne $sourceLock.serverArtifact.nativeSha256) {
    throw 'The native artifact differs from the verified baseline recorded in sources.lock.json.'
}

New-Item -ItemType Directory -Path $serverRoot -Force | Out-Null
Get-ChildItem -LiteralPath $artifactRoot | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $serverRoot -Recurse
}

$settings = [ordered]@{
    name = 'SkyMP TR Local Lab'
    port = 7777
    maxPlayers = 4
    offlineMode = $true
    master = ''
    dataDir = 'data'
    loadOrder = $loadOrder
    databaseDriver = 'file'
    databaseName = 'world'
    npcEnabled = $false
    npcSettings = @{}
    gamemodePath = 'gamemode.js'
    uiListenHost = '0.0.0.0'
    enableGamemodeDataUpdatesBroadcast = $false
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText((Join-Path $serverRoot 'server-settings.json'), ($settings | ConvertTo-Json -Depth 6), $utf8)
$gamemode = 'console.log("[SKYMP_LAB_READY]", JSON.stringify({ onlinePlayers: mp.get(0, "onlinePlayers") }));'
[System.IO.File]::WriteAllText((Join-Path $serverRoot 'gamemode.js'), $gamemode, $utf8)

$provenance = [ordered]@{
    repo = $sourceLock.skymp.repository
    commit = $expectedCommit
    runId = $runId
    artifact = $sourceLock.serverArtifact.name
    preparedAtUtc = [DateTime]::UtcNow.ToString('o')
    nativeSha256 = (Get-FileHash -LiteralPath (Join-Path $serverRoot 'scam_native.node') -Algorithm SHA256).Hash
    nodeVersion = (& $nodeCommand.Source --version)
    runtimeSettings = 'offline, two players, NPCs disabled, HTTP on loopback'
    originalArtifactGamemode = 'CI integration test replaced with local readiness gamemode'
}
[System.IO.File]::WriteAllText((Join-Path (Split-Path -Parent $serverRoot) 'provenance.json'), ($provenance | ConvertTo-Json), $utf8)
Write-Output "Local server prepared: $serverRoot"
