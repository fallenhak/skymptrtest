param(
    [string]$ServerAddress = '127.0.0.1',
    [ValidateRange(1, 65535)][int]$ServerPort = 7777,
    [ValidateRange(1, 2147483647)][int]$ProfileId = 2,
    [string]$OutputPath = 'dist/SkyMPTR-Client-Pack.zip'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json

# Kaynak olarak hazir lab-player-1 veya lab-player-2 dosyalarini kullaniyoruz
$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-1"
if (-not (Test-Path -LiteralPath $labRoot)) {
    throw "Hazir lab bulunamadi: $labRoot. Once lab-player-1 ortamini hazirlayin."
}

$tempStage = Join-Path $projectRoot '.local/client-package-staging'
if (Test-Path -LiteralPath $tempStage) {
    Remove-Item -LiteralPath $tempStage -Recurse -Force
}
New-Item -ItemType Directory -Path $tempStage -Force | Out-Null

Write-Output "Istemci paketi hazirlaniyor (Sunucu: $ServerAddress, Port: $ServerPort, Profil: $ProfileId)..."

# 1. Kok Dizin Dosyalari (SKSE loader, dll ve Engine Fixes preloader)
$gameRoot = Join-Path $labRoot 'game'
$rootFiles = @('skse64_loader.exe', 'skse64_1_6_1170.dll', 'd3dx9_42.dll')
foreach ($rf in $rootFiles) {
    $src = Join-Path $gameRoot $rf
    if (Test-Path -LiteralPath $src -PathType Leaf) {
        Copy-Item -LiteralPath $src -Destination $tempStage -Force
    } else {
        throw "Gerekli kok dosya bulunamadi: $src"
    }
}

# 2. Data Klasoru
$stageData = Join-Path $tempStage 'Data'
New-Item -ItemType Directory -Path $stageData -Force | Out-Null

# lab-player-1 altindaki mods/ klasorundeki modlari toparla
$modsRoot = Join-Path $labRoot 'mod-organizer/mods'
if (-not (Test-Path -LiteralPath $modsRoot)) {
    throw "Mod klasoru bulunamadi: $modsRoot"
}

$mods = Get-ChildItem -LiteralPath $modsRoot -Directory | Sort-Object Name
foreach ($mod in $mods) {
    Get-ChildItem -Path $mod.FullName -Recurse -File | Where-Object { $_.Extension -ne '.pdb' } | ForEach-Object {
        $rel = $_.FullName.Substring($mod.FullName.Length + 1)
        $dest = Join-Path $stageData $rel
        $parent = Split-Path -Parent $dest
        if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
    }
}

# 3. skymp5-client-settings.txt Yapilandirmasi
$httpPort = if ($ServerPort -eq 7777) { 3000 } else { $ServerPort + 1 }
$serverHttpUrl = [System.UriBuilder]::new('http', $ServerAddress, $httpPort).Uri.GetLeftPart([System.UriPartial]::Authority)
$clientSettings = [ordered]@{
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
$settingsFile = Join-Path $stageData 'Platform/Plugins/skymp5-client-settings.txt'
[System.IO.File]::WriteAllText($settingsFile, ($clientSettings | ConvertTo-Json -Depth 4), $utf8)

# 4. SSE Display Tweaks Ayari: Cercevesiz Pencere (1280x720)
$displayTweaksIni = Join-Path $stageData 'SKSE/Plugins/SSEDisplayTweaks.ini'
if (Test-Path -LiteralPath $displayTweaksIni) {
    $iniContent = Get-Content -LiteralPath $displayTweaksIni -Raw
    $iniContent = $iniContent -replace '(?m)^#?\s*Fullscreen\s*=.*$', 'Fullscreen=false'
    $iniContent = $iniContent -replace '(?m)^#?\s*Borderless\s*=.*$', 'Borderless=true'
    $iniContent = $iniContent -replace '(?m)^#?\s*Resolution\s*=.*$', 'Resolution=1280x720'
    [System.IO.File]::WriteAllText($displayTweaksIni, $iniContent, $utf8)
}

# 5. Sunucuya_Baglan.bat (Arkadasin cift tiklayacagi baslatici)
$launcherBat = @"
@echo off
chcp 65001 >nul
title SkyMP TR - Oyuna Baglan
echo =======================================================
echo          SkyMP TR Cok Oyunculu Test Baslatici
echo =======================================================
echo.

if not exist SkyrimSE.exe (
    echo [HATA] SkyrimSE.exe bu klasorde bulunamadi!
    echo Lutfen bu paketin icindeki tum dosyalari Skyrim Special Edition
    echo (1.6.1170) oyununun kurulu oldugu ana klasore kopyalayin.
    echo.
    pause
    exit /b 1
)

echo [1/3] SKSE ve mod dosyalari kontrol ediliyor...
if not exist skse64_loader.exe (
    echo [HATA] skse64_loader.exe eksik!
    pause
    exit /b 1
)

set DEFAULT_IP=$ServerAddress
set /p TARGET_IP="Sunucu IP Adresini Girin (Enter: %DEFAULT_IP%): "
if "%TARGET_IP%"=="" set TARGET_IP=%DEFAULT_IP%

set DEFAULT_PID=$ProfileId
set /p TARGET_PID="Oyuncu Profil No (Enter: %DEFAULT_PID%): "
if "%TARGET_PID%"=="" set TARGET_PID=%DEFAULT_PID%

echo.
echo [2/3] Baglanti ayarlari guncelleniyor (IP: %TARGET_IP%, Profil: %TARGET_PID%)...

powershell -NoProfile -Command "`$p = 'Data\Platform\Plugins\skymp5-client-settings.txt'; if (Test-Path `$p) { `$j = Get-Content `$p -Raw | ConvertFrom-Json; `$j.gameData.profileId = [int]'%TARGET_PID%'; `$j.'server-ip' = '%TARGET_IP%'; `$j.'server-http-url' = 'http://' + '%TARGET_IP%' + ':3000'; [IO.File]::WriteAllText((Resolve-Path `$p).Path, (`$j | ConvertTo-Json -Depth 4)); }"

echo [3/3] Skyrim baslatiliyor...
start "" skse64_loader.exe
echo Oyun aciliyor, keyifli oyunlar!
timeout /t 5 >nul
exit /b 0
"@
[System.IO.File]::WriteAllText((Join-Path $tempStage 'Sunucuya_Baglan.bat'), $launcherBat, [System.Text.Encoding]::GetEncoding(1254))

# 6. BENI_OKU_KURULUM.txt
$readme = @"
===========================================================
                SkyMP TR - Cok Oyunculu Test Paketi
===========================================================

Gereksinim:
- Bilgisayarinizda temiz Skyrim Special Edition 1.6.1170 kurulu olmalidir.
- Radmin VPN (veya sunucuyu kuran arkadasinizin belirttigi VPN programi).

Kurulum Adimlari:
1. Radmin VPN programini acip arkadasinizin kurdugu odaya katilin.
2. Bu ZIP arsivinin icindeki TUM DOSYALARI ve Data klasorunu
   SkyrimSpecialEdition.exe'nin bulundugu ana oyun klasorunuze kopyalayin
   (Dosyalarin degistirilmesini onaylayin).
3. Oyun klasorunuzdeki 'Sunucuya_Baglan.bat' dosyasina cift tiklayin.
4. Arkadasinizin Radmin IP adresini onaylayin (veya Enter'a basin).
5. Oyun acilacak ve dogrudan sunucuya baglanacaksiniz!
"@
[System.IO.File]::WriteAllText((Join-Path $tempStage 'BENI_OKU_KURULUM.txt'), $readme, [System.Text.Encoding]::GetEncoding(1254))

# 7. Zip Paketi Olusturma
$destZipFull = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputPath))
$destDir = Split-Path -Parent $destZipFull
if (-not (Test-Path -LiteralPath $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}
if (Test-Path -LiteralPath $destZipFull) {
    Remove-Item -LiteralPath $destZipFull -Force
}

Write-Output "Zip paketi olusturuluyor: $destZipFull..."
Compress-Archive -Path "$tempStage\*" -DestinationPath $destZipFull -CompressionLevel Optimal

# Temizlik
Remove-Item -LiteralPath $tempStage -Recurse -Force

$zipSizeMb = [math]::Round(((Get-Item -LiteralPath $destZipFull).Length / 1MB), 2)
Write-Output "Tebrikler! Istemci paketi olusturuldu ($zipSizeMb MB):"
Write-Output $destZipFull
