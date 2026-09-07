param(
    [Parameter(Mandatory = $true)][string]$SkyrimDirectory,
    [Parameter(Mandatory = $true)][string]$SkseArchive,
    [Parameter(Mandatory = $true)][string]$AddressLibraryArchive,
    [Parameter(Mandatory = $true)][string]$ModOrganizerArchive,
    [ValidateRange(1, 2147483647)][int]$ProfileId = 1
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$labRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-$ProfileId"))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot '.local')) + [IO.Path]::DirectorySeparatorChar
if (-not $labRoot.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Lab must stay inside the ignored .local directory.' }
if (Test-Path -LiteralPath $labRoot) { throw "Existing lab was preserved: $labRoot" }
$sourceRoot = (Resolve-Path -LiteralPath $SkyrimDirectory).Path
$gameVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $sourceRoot 'SkyrimSE.exe'))
$version = @($gameVersion.FileMajorPart, $gameVersion.FileMinorPart, $gameVersion.FileBuildPart, $gameVersion.FilePrivatePart) -join '.'
if ($version -ne $lock.gameLab.gameVersion) { throw "These pinned dependencies require Skyrim $($lock.gameLab.gameVersion); found $version." }
$clientRoot = Join-Path $projectRoot "$($lock.clientArtifact.profilesDirectory)/player-$ProfileId"
$frontRoot = Join-Path $projectRoot 'build/dist/client/Data/Platform/UI'
foreach ($required in @((Join-Path $clientRoot 'Data/Platform/Plugins/skymp5-client.js'), (Join-Path $frontRoot 'index.html'))) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Missing build/profile file: $required. Build client/front and prepare-local-client first." }
}
$archives = @(
    @{ Name = 'skse'; Path = (Resolve-Path -LiteralPath $SkseArchive).Path; Hash = $lock.gameLab.skse.sha256 },
    @{ Name = 'address-library'; Path = (Resolve-Path -LiteralPath $AddressLibraryArchive).Path; Hash = $lock.gameLab.addressLibrary.sha256 },
    @{ Name = 'mod-organizer'; Path = (Resolve-Path -LiteralPath $ModOrganizerArchive).Path; Hash = $lock.gameLab.modOrganizer.sha256 }
)
foreach ($archive in $archives) {
    if ((Get-FileHash -LiteralPath $archive.Path -Algorithm SHA256).Hash -ne $archive.Hash) { throw "Archive hash mismatch: $($archive.Name)" }
}
$masters = @('Skyrim.esm', 'Update.esm', 'Dawnguard.esm', 'HearthFires.esm', 'Dragonborn.esm')
$dataRoot = Join-Path $sourceRoot 'Data'
$dataFiles = @(foreach ($master in $masters) { Get-Item -LiteralPath (Join-Path $dataRoot $master) })
$dataFiles += @(Get-ChildItem -LiteralPath $dataRoot -File | Where-Object { $_.Extension -eq '.bsa' -and $_.Name -match '^(Skyrim|Dawnguard|HearthFires|Dragonborn)( -|\.)' })
if (($dataFiles | Where-Object Extension -eq '.bsa').Count -eq 0) { throw 'Base game BSA assets were not found.' }
$requiredBytes = ($dataFiles | Measure-Object Length -Sum).Sum + 2GB
$drive = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($labRoot))
if ($drive.AvailableFreeSpace -lt $requiredBytes) { throw 'Insufficient free space for an independent game copy and dependencies.' }

$gameRoot = Join-Path $labRoot 'game'
$moRoot = Join-Path $labRoot 'mod-organizer'
$profileRoot = Join-Path $moRoot 'profiles/SkyMPTR'
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-LabText([string]$Path, [string]$Value) { [IO.File]::WriteAllText($Path, $Value, $utf8) }
New-Item -ItemType Directory -Path (Join-Path $gameRoot 'Data'), $profileRoot -Force | Out-Null

# A fresh destination and ordinary copies keep game assets independent of Steam.
Write-Output "Copying $($dataFiles.Count) base game files to $gameRoot"
foreach ($name in @('SkyrimSE.exe', 'SkyrimSELauncher.exe', 'steam_api64.dll', 'bink2w64.dll', 'Skyrim_Default.ini')) {
    Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination $gameRoot
}
foreach ($file in $dataFiles) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $gameRoot 'Data') }
Write-LabText (Join-Path $gameRoot 'Skyrim.ccc') ''

foreach ($archive in $archives) {
    $destination = if ($archive.Name -eq 'mod-organizer') { $moRoot } else { Join-Path $labRoot "extracted/$($archive.Name)" }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    & tar.exe -xf $archive.Path -C $destination
    if ($LASTEXITCODE -ne 0) { throw "Could not extract $($archive.Name). Partial lab is preserved for inspection." }
}
$skseRoot = Join-Path $labRoot "extracted/skse/$($lock.gameLab.skse.archiveRoot)"
Get-ChildItem -LiteralPath $skseRoot -File | Where-Object { $_.Extension -in @('.dll', '.exe') } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $gameRoot
}
Copy-Item -LiteralPath (Join-Path $skseRoot 'Data') -Destination $gameRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $clientRoot 'Data') -Destination $gameRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $labRoot 'extracted/address-library/SKSE') -Destination (Join-Path $gameRoot 'Data') -Recurse -Force
Copy-Item -LiteralPath $frontRoot -Destination (Join-Path $gameRoot 'Data/Platform') -Recurse -Force

foreach ($dir in @('mods', 'downloads', 'overwrite', 'profiles/SkyMPTR/saves')) {
    New-Item -ItemType Directory -Path (Join-Path $moRoot $dir) -Force | Out-Null
}
$qtGameRoot = $gameRoot.Replace('\', '/')
Write-LabText (Join-Path $moRoot 'portable.txt') 'SkyMP TR isolated instance'
Write-LabText (Join-Path $moRoot 'ModOrganizer.ini') @"
[General]
gameName=Skyrim Special Edition
gamePath=@ByteArray($qtGameRoot)
game_edition=Steam
selected_profile=@ByteArray(SkyMPTR)
first_start=false
version=2.5.2

[Settings]
profile_local_inis=true
profile_local_saves=true
"@
Write-LabText (Join-Path $profileRoot 'settings.ini') "[General]`r`nLocalSaves=true`r`nLocalSettings=true`r`n"
Write-LabText (Join-Path $profileRoot 'modlist.txt') "# SkyMP files are installed in this isolated game's Data directory.`r`n"
Write-LabText (Join-Path $profileRoot 'plugins.txt') (($masters | ForEach-Object { "*$_" }) -join "`r`n")
Write-LabText (Join-Path $profileRoot 'loadorder.txt') ($masters -join "`r`n")
$gameIni = Get-Content -LiteralPath (Join-Path $sourceRoot 'Skyrim_Default.ini') -Raw
$gameIni = $gameIni.Replace('[General]', "[General]`r`nsLocalSavePath=__MO_Saves\")
Write-LabText (Join-Path $profileRoot 'skyrim.ini') $gameIni
$preferences = Get-Content -LiteralPath (Join-Path $sourceRoot 'Medium.ini') -Raw
$preferences = $preferences.Replace('[Display]', "[Display]`r`nbFull Screen=0`r`nbBorderless=0`r`niSize W=1280`r`niSize H=720")
Write-LabText (Join-Path $profileRoot 'skyrimprefs.ini') $preferences
Write-LabText (Join-Path $profileRoot 'skyrimcustom.ini') "[General]`r`nsLocalSavePath=__MO_Saves\`r`n"

$provenance = [ordered]@{
    preparedAtUtc = [DateTime]::UtcNow.ToString('o')
    gameVersion = $version
    profileId = $ProfileId
    sourceGameDirectory = $sourceRoot
    copiedDataFiles = @($dataFiles | ForEach-Object { @{ name = $_.Name; bytes = $_.Length } })
    sourceExeSha256 = (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'SkyrimSE.exe')).Hash
    clientScriptSha256 = (Get-FileHash -LiteralPath (Join-Path $gameRoot 'Data/Platform/Plugins/skymp5-client.js')).Hash
    frontendSha256 = (Get-FileHash -LiteralPath (Join-Path $gameRoot 'Data/Platform/UI/build.js')).Hash
    upstreamCommit = $lock.skymp.commit
    nativeArtifactRunId = $lock.clientArtifact.runId
    dependencies = $lock.gameLab
    status = 'Prepared; native compatibility, profile isolation at runtime and connection require verification'
}
Write-LabText (Join-Path $labRoot 'lab-provenance.json') ($provenance | ConvertTo-Json -Depth 6)
Write-Output "Prepared separate game and MO2 profile: $labRoot"
Write-Output 'Use scripts/start-game-lab.ps1. Starting the loader directly bypasses profile isolation.'
