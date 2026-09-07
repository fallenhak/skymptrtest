param([ValidateRange(1, 2147483647)][int]$ProfileId = 1)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-$ProfileId"
$gameRoot = Join-Path $labRoot 'game'
$moRoot = Join-Path $labRoot 'mod-organizer'
foreach ($required in @('game/skse64_loader.exe', 'mod-organizer/ModOrganizer.exe', 'mod-organizer/portable.txt', 'lab-provenance.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $labRoot $required) -PathType Leaf)) { throw "Incomplete lab: $required" }
}
if (Get-Process SkyrimSE, skse64_loader, ModOrganizer -ErrorAction SilentlyContinue) {
    throw 'Close the running Skyrim/SKSE/Mod Organizer session before starting this isolated test.'
}
$profileSettings = Get-Content -LiteralPath (Join-Path $moRoot 'profiles/SkyMPTR/settings.ini') -Raw
if ($profileSettings -notmatch '(?m)^LocalSettings=true\s*$') {
    throw 'Enable profile-specific game INI files in the SkyMPTR profile before starting.'
}
if ($profileSettings -match '(?m)^LocalSaves=true\s*$') {
    throw 'SkyMP native runtime requires standard save path (LocalSaves=false in settings.ini).'
}
function Convert-LabArgument([string]$Value) {
    return '"' + ($Value -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
}
# Start-Process -Wait creates a job that conflicts with MO2's CREATE_BREAKAWAY_FROM_JOB.
# Wait on MO2 itself instead; GUI executables do not reliably set LASTEXITCODE.
$arguments = (@('--profile', 'SkyMPTR', 'run', '--cwd', $gameRoot, (Join-Path $gameRoot 'skse64_loader.exe')) |
    ForEach-Object { Convert-LabArgument $_ }) -join ' '
$process = Start-Process -FilePath (Join-Path $moRoot 'ModOrganizer.exe') -ArgumentList $arguments `
    -WorkingDirectory $moRoot -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "MO2/game startup failed with exit code $($process.ExitCode). Inspect the lab logs." }
