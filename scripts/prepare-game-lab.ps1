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
$modsRoot = Join-Path $moRoot 'mods'
foreach ($dir in @('mods', 'downloads', 'overwrite', 'profiles/SkyMPTR/saves')) {
    New-Item -ItemType Directory -Path (Join-Path $moRoot $dir) -Force | Out-Null
}

# 01_SKSE_Scripts
$mod1 = Join-Path $modsRoot '01_SKSE_Scripts'
New-Item -ItemType Directory -Path $mod1 -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $skseRoot 'Data/Scripts') -Destination $mod1 -Recurse -Force

# 02_Address_Library
$mod2Plugins = Join-Path $modsRoot '02_Address_Library/SKSE/Plugins'
New-Item -ItemType Directory -Path $mod2Plugins -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $labRoot 'extracted/address-library/SKSE/Plugins') -Destination (Join-Path $modsRoot '02_Address_Library/SKSE') -Recurse -Force

# 03_SSE_Display_Tweaks
$displayTweaksArchive = 'C:\Users\kerim\Games\Faalgrin\Downloads\SSE Display Tweaks-34705-0-5-16-1703410713.zip'
if (Test-Path -LiteralPath $displayTweaksArchive) {
    $mod3 = Join-Path $modsRoot '03_SSE_Display_Tweaks'
    New-Item -ItemType Directory -Path $mod3 -Force | Out-Null
    tar -xf $displayTweaksArchive -C $mod3
}

# 04_Skyrim_Souls_RE
$skyrimSoulsArchive = 'C:\Users\kerim\Games\Faalgrin\Downloads\Skyrim Souls RE - Unpaused Menus-27859-3-1-2-1779355258.zip'
if (Test-Path -LiteralPath $skyrimSoulsArchive) {
    $mod4 = Join-Path $modsRoot '04_Skyrim_Souls_RE'
    New-Item -ItemType Directory -Path $mod4 -Force | Out-Null
    tar -xf $skyrimSoulsArchive -C $mod4
    $soulsIni = Join-Path $mod4 'SKSE/Plugins/SkyrimSoulsRE.ini'
    if (Test-Path -LiteralPath $soulsIni) {
        (Get-Content -LiteralPath $soulsIni -Raw).Replace('bHideEngineFixesWarning = false', 'bHideEngineFixesWarning = true') | Set-Content -LiteralPath $soulsIni -NoNewline
    }
}

# 05_SkyMP_Client
$mod5 = Join-Path $modsRoot '05_SkyMP_Client'
New-Item -ItemType Directory -Path $mod5 -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $clientRoot 'Data/Platform') -Destination $mod5 -Recurse -Force
Copy-Item -LiteralPath $frontRoot -Destination (Join-Path $mod5 'Platform') -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $mod5 'Platform/PluginsDev') -Force | Out-Null

$mod5Plugins = Join-Path $mod5 'SKSE/Plugins'
New-Item -ItemType Directory -Path $mod5Plugins -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $clientRoot 'Data/SKSE/Plugins/SkyrimPlatform.dll') -Destination $mod5Plugins -Force
Copy-Item -LiteralPath (Join-Path $clientRoot 'Data/SKSE/Plugins/MpClientPlugin.dll') -Destination $mod5Plugins -Force

$mod5Interface = Join-Path $mod5 'Interface'
New-Item -ItemType Directory -Path $mod5Interface -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $clientRoot 'Data/Interface/CombatAlertOverlayMenu.swf') -Destination $mod5Interface -Force

# 06_Engine_Fixes
$engineFixesPreloader = 'C:\Users\kerim\Games\Faalgrin\Modlist\Stock Game\d3dx9_42.dll'
if (Test-Path -LiteralPath $engineFixesPreloader) {
    Copy-Item -LiteralPath $engineFixesPreloader -Destination $gameRoot -Force
} else {
    $engineFixesPreloaderArchive = 'C:\Users\kerim\Games\Faalgrin\Downloads\Engine Fixes - SKSE64 Preloader-17230-7-1771936758.7z'
    if (Test-Path -LiteralPath $engineFixesPreloaderArchive) {
        $tempPreload = Join-Path $labRoot 'extracted/engine-fixes-preloader'
        New-Item -ItemType Directory -Path $tempPreload -Force | Out-Null
        & tar.exe -xf $engineFixesPreloaderArchive -C $tempPreload
        if (Test-Path -LiteralPath (Join-Path $tempPreload 'd3dx9_42.dll')) {
            Copy-Item -LiteralPath (Join-Path $tempPreload 'd3dx9_42.dll') -Destination $gameRoot -Force
        }
    }
}
$engineFixesMod = 'C:\Users\kerim\Games\Faalgrin\Modlist\mods\EngineFixes\SKSE'
if (Test-Path -LiteralPath $engineFixesMod) {
    $mod6 = Join-Path $modsRoot '06_Engine_Fixes'
    New-Item -ItemType Directory -Path $mod6 -Force | Out-Null
    Copy-Item -Path $engineFixesMod -Destination $mod6 -Recurse -Force
} else {
    $engineFixesArchive = 'C:\Users\kerim\Games\Faalgrin\Downloads\Engine Fixes - Main File-17230-7-0-20-1772078239.7z'
    if (Test-Path -LiteralPath $engineFixesArchive) {
        $mod6 = Join-Path $modsRoot '06_Engine_Fixes'
        $mod6Plugins = Join-Path $mod6 'SKSE/Plugins'
        New-Item -ItemType Directory -Path $mod6Plugins -Force | Out-Null
        $tempMain = Join-Path $labRoot 'extracted/engine-fixes-main'
        New-Item -ItemType Directory -Path $tempMain -Force | Out-Null
        & tar.exe -xf $engineFixesArchive -C $tempMain
        Copy-Item -Path (Join-Path $tempMain 'EngineFixes FOMOD Installer/Required/SKSE/Plugins/*') -Destination $mod6Plugins -Force
        Copy-Item -Path (Join-Path $tempMain 'EngineFixes FOMOD Installer/AE/SKSE/Plugins/*') -Destination $mod6Plugins -Force
    }
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
profile_local_saves=false
"@
Write-LabText (Join-Path $profileRoot 'settings.ini') "[General]`r`nLocalSaves=false`r`nLocalSettings=true`r`n"
$modList = @(
    '+06_Engine_Fixes',
    '+05_SkyMP_Client',
    '+04_Skyrim_Souls_RE',
    '+03_SSE_Display_Tweaks',
    '+02_Address_Library',
    '+01_SKSE_Scripts'
) -join "`r`n"
Write-LabText (Join-Path $profileRoot 'modlist.txt') $modList
Write-LabText (Join-Path $profileRoot 'plugins.txt') (($masters | ForEach-Object { "*$_" }) -join "`r`n")
Write-LabText (Join-Path $profileRoot 'loadorder.txt') ($masters -join "`r`n")
$gameIni = Get-Content -LiteralPath (Join-Path $sourceRoot 'Skyrim_Default.ini') -Raw
$gameIni = $gameIni.Replace('[General]', "[General]`r`nsLocalSavePath=Saves\")
Write-LabText (Join-Path $profileRoot 'skyrim.ini') $gameIni
$preferences = Get-Content -LiteralPath (Join-Path $sourceRoot 'Medium.ini') -Raw
$preferences = $preferences.Replace('[General]', "[General]`r`nbFreebiesSeen=1")
$preferences = $preferences.Replace('[Display]', "[Display]`r`nbFull Screen=0`r`nbBorderless=0`r`niSize W=1280`r`niSize H=720")
Write-LabText (Join-Path $profileRoot 'skyrimprefs.ini') $preferences
Write-LabText (Join-Path $profileRoot 'skyrimcustom.ini') "[General]`r`nsLocalSavePath=Saves\`r`n"

$provenance = [ordered]@{
    preparedAtUtc = [DateTime]::UtcNow.ToString('o')
    gameVersion = $version
    profileId = $ProfileId
    sourceGameDirectory = $sourceRoot
    copiedDataFiles = @($dataFiles | ForEach-Object { @{ name = $_.Name; bytes = $_.Length } })
    sourceExeSha256 = (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'SkyrimSE.exe')).Hash
    clientScriptSha256 = (Get-FileHash -LiteralPath (Join-Path $modsRoot '05_SkyMP_Client/Platform/Plugins/skymp5-client.js')).Hash
    frontendSha256 = (Get-FileHash -LiteralPath (Join-Path $modsRoot '05_SkyMP_Client/Platform/UI/build.js')).Hash
    upstreamCommit = $lock.skymp.commit
    nativeArtifactRunId = $lock.clientArtifact.runId
    dependencies = $lock.gameLab
    status = 'Prepared; native compatibility, profile isolation at runtime and connection require verification'
}
Write-LabText (Join-Path $labRoot 'lab-provenance.json') ($provenance | ConvertTo-Json -Depth 6)
Write-Output "Prepared separate game and MO2 profile: $labRoot"
Write-Output 'Use scripts/start-game-lab.ps1. Starting the loader directly bypasses profile isolation.'
