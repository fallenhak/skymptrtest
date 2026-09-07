param([ValidateRange(1, 2147483647)][int]$ProfileId = 1)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-$ProfileId"
$moRoot = Join-Path $labRoot 'mod-organizer'
if (-not (Test-Path -LiteralPath (Join-Path $labRoot 'lab-provenance.json'))) { throw 'Prepare the game lab first.' }
if (Get-Process SkyrimSE, skse64_loader, ModOrganizer -ErrorAction SilentlyContinue) { throw 'Close Skyrim and Mod Organizer before the profile probe.' }
$myGamesPath = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games/Skyrim Special Edition'
$appGamePath = Join-Path $env:LOCALAPPDATA 'Skyrim Special Edition'
$protectedPaths = @(
    (Join-Path $myGamesPath 'Skyrim.ini'), (Join-Path $myGamesPath 'SkyrimPrefs.ini'),
    (Join-Path $myGamesPath 'SkyrimCustom.ini'), (Join-Path $appGamePath 'plugins.txt'), (Join-Path $appGamePath 'loadorder.txt')
)
$before = @{}
foreach ($file in $protectedPaths) {
    $before[$file] = if (Test-Path -LiteralPath $file) { (Get-FileHash -LiteralPath $file).Hash } else { $null }
}
function Convert-LabArgument([string]$Value) {
    return '"' + ($Value -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
}
$marker = [Guid]::NewGuid().ToString('N')
$probeOutput = Join-Path $labRoot "profile-probe-$marker.json"
$probeArguments = (@((Join-Path $PSScriptRoot 'probe-game-profile.mjs'), $myGamesPath, $appGamePath, $probeOutput, $marker) |
    ForEach-Object { Convert-LabArgument $_ }) -join ' '
$arguments = (@('--profile', 'SkyMPTR', 'run', '--cwd', (Join-Path $labRoot 'game'), '--arguments', $probeArguments, (Get-Command node.exe).Source) |
    ForEach-Object { Convert-LabArgument $_ }) -join ' '
$process = Start-Process -FilePath (Join-Path $moRoot 'ModOrganizer.exe') -ArgumentList $arguments `
    -WorkingDirectory $moRoot -WindowStyle Hidden -PassThru
# Avoid Start-Process -Wait: its job prevents MO2's child process breakaway.
$process.WaitForExit()
if (-not (Test-Path -LiteralPath $probeOutput)) { throw 'MO2 profile probe produced no report; inspect the lab logs.' }
$probe = Get-Content -LiteralPath $probeOutput -Raw | ConvertFrom-Json
if ($probe.error) { throw "MO2 profile probe failed: $($probe.error)" }
if ($process.ExitCode -ne 0) { throw "MO2 profile probe exited with code $($process.ExitCode)." }
if (-not $probe.settingsMapped -or -not $probe.pluginsMapped -or -not $probe.savePathAligned -or $probe.marker -ne $marker) { throw 'Probe response did not match this run.' }
foreach ($file in $protectedPaths) {
    $after = if (Test-Path -LiteralPath $file) { (Get-FileHash -LiteralPath $file).Hash } else { $null }
    if ($before[$file] -ne $after) { throw "Original settings changed: $file" }
}
$report = [ordered]@{
    checkedAtUtc = [DateTime]::UtcNow.ToString('o')
    profileId = $ProfileId
    settingsMapped = $true
    pluginsMapped = $true
    savePathAligned = $true
    originalSettingsUnchanged = $true
    scope = 'Actual MO2 filesystem probe through Node; Skyrim gameplay/native compatibility is a separate test'
}
$json = $report | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $labRoot 'profile-isolation.json'), $json, [Text.UTF8Encoding]::new($false))
Write-Output $json
