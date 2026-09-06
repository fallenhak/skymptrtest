param(
    [Parameter(Mandatory = $true)]
    [string]$SkyrimDirectory,
    [ValidateRange(1, 2147483647)]
    [int]$ProfileId = 1
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceLock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$clientRoot = Join-Path (Join-Path $projectRoot $sourceLock.clientArtifact.profilesDirectory) "player-$ProfileId"
$gameRoot = (Resolve-Path -LiteralPath $SkyrimDirectory).Path
$executable = Join-Path $gameRoot 'SkyrimSE.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'SkyrimSE.exe was not found.' }
$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($executable)
$versionParts = @($versionInfo.FileMajorPart, $versionInfo.FileMinorPart, $versionInfo.FileBuildPart, $versionInfo.FilePrivatePart)
$addressLibrary = "versionlib-$($versionParts -join '-').bin"
$skseRuntime = "skse64_$($versionParts[0..2] -join '_').dll"
$missing = @()
foreach ($file in @('skse64_loader.exe', $skseRuntime)) {
    if (-not (Test-Path -LiteralPath (Join-Path $gameRoot $file) -PathType Leaf)) { $missing += $file }
}
foreach ($file in @("Data/SKSE/Plugins/$addressLibrary", 'Data/Platform/UI/index.html', 'Data/SKSE/Plugins/SkyrimPlatform.dll', 'Data/SKSE/Plugins/MpClientPlugin.dll')) {
    $inGame = Test-Path -LiteralPath (Join-Path $gameRoot $file) -PathType Leaf
    $inProfile = Test-Path -LiteralPath (Join-Path $clientRoot $file) -PathType Leaf
    if (-not $inGame -and -not $inProfile) { $missing += $file }
}
$report = [ordered]@{
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    gameVersion = ($versionParts -join '.')
    profileId = $ProfileId
    stagedClientExists = (Test-Path -LiteralPath $clientRoot)
    requiredFilesPresent = ($missing.Count -eq 0)
    missing = $missing
    scope = 'File prerequisite check only; presence does not prove native runtime compatibility or game connectivity'
}
$json = $report | ConvertTo-Json -Depth 4
if (Test-Path -LiteralPath $clientRoot) {
    [System.IO.File]::WriteAllText((Join-Path $clientRoot 'client-preflight.json'), $json, [System.Text.UTF8Encoding]::new($false))
}
Write-Output $json
if ($missing.Count -gt 0) { exit 2 }
