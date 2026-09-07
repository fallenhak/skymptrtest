param(
    [ValidateRange(1, 2147483647)][int]$ProfileId = 1,
    [switch]$ViaMO2
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-$ProfileId"
$gameRoot = Join-Path $labRoot 'game'
$moRoot = Join-Path $labRoot 'mod-organizer'
foreach ($required in @('game/skse64_loader.exe', 'lab-provenance.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $labRoot $required) -PathType Leaf)) { throw "Incomplete lab: $required" }
}
$runningInLab = @(Get-Process SkyrimSE, skse64_loader -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($gameRoot, [StringComparison]::OrdinalIgnoreCase) })
if ($runningInLab.Count -gt 0) {
    throw "Close the running Skyrim/SKSE session for lab-player-$ProfileId before starting."
}

# Fast-sync mods from mod-organizer/mods to game/Data so files are available without USVFS hooks
$modsRoot = Join-Path $moRoot 'mods'
$gameData = Join-Path $gameRoot 'Data'
if (Test-Path -LiteralPath $modsRoot) {
    $mods = Get-ChildItem -LiteralPath $modsRoot -Directory | Sort-Object Name
    foreach ($mod in $mods) {
        Get-ChildItem -Path $mod.FullName -Recurse -File | ForEach-Object {
            $rel = $_.FullName.Substring($mod.FullName.Length + 1)
            $dest = Join-Path $gameData $rel
            $parent = Split-Path -Parent $dest
            if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
            if (-not (Test-Path -LiteralPath $dest) -or (Get-Item -LiteralPath $dest).LastWriteTimeUtc -lt $_.LastWriteTimeUtc) {
                Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
            }
        }
    }
}

if ($ViaMO2) {
    function Convert-LabArgument([string]$Value) {
        return '"' + ($Value -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
    }
    $arguments = (@('--multiple', '--profile', 'SkyMPTR', 'run', '--cwd', $gameRoot, (Join-Path $gameRoot 'skse64_loader.exe')) |
        ForEach-Object { Convert-LabArgument $_ }) -join ' '
    $process = Start-Process -FilePath (Join-Path $moRoot 'ModOrganizer.exe') -ArgumentList $arguments `
        -WorkingDirectory $moRoot -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "MO2/game startup failed with exit code $($process.ExitCode). Inspect the lab logs." }
} else {
    $loader = Join-Path $gameRoot 'skse64_loader.exe'
    $process = Start-Process -FilePath $loader -WorkingDirectory $gameRoot -PassThru
    $process.WaitForExit(10000) | Out-Null
    Write-Output "Started Skyrim SE (Player $ProfileId) directly via SKSE."
}
