param(
    [string]$ServerAddress = '127.0.0.1',
    [int]$ProfileId = 2,
    [string]$OutputPackage = 'dist/SkyMPTR-StockGame-Installer.zip'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json

$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-1"
if (-not (Test-Path -LiteralPath $labRoot)) {
    throw "Gerekli lab bulunamadi: $labRoot. Once lab-player-1 hazirlanmalidir."
}

$buildDir = Join-Path $projectRoot 'dist/launcher-build'
if (Test-Path -LiteralPath $buildDir) {
    Remove-Item -LiteralPath $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null

Write-Output "1. SkyMP TR Launcher derleniyor (C# / WPF)..."
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'
$launcherExe = Join-Path $buildDir 'SkyMPTR-Launcher.exe'
$sourceCs = Join-Path $projectRoot 'src/launcher/Program.cs'

& $csc /nologo /target:winexe /out:$launcherExe /lib:$wpf `
    /r:PresentationFramework.dll /r:PresentationCore.dll /r:WindowsBase.dll `
    /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
    $sourceCs

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $launcherExe)) {
    throw "Launcher derlemesi basarisiz oldu."
}
Write-Output "Launcher derlendi: $launcherExe"

Write-Output "2. Stock Game bilesenleri ve 1.6.1170 downgrade paketi toparlaniyor..."
$stagingData = Join-Path $projectRoot '.local/launcher-data-staging'
if (Test-Path -LiteralPath $stagingData) {
    Remove-Item -LiteralPath $stagingData -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingData -Force | Out-Null

# Pinned 1.6.1170 binaries for downgrade
$gameRoot = Join-Path $labRoot 'game'
foreach ($f in @('SkyrimSE.exe', 'bink2w64.dll', 'skse64_loader.exe', 'skse64_1_6_1170.dll', 'd3dx9_42.dll')) {
    $src = Join-Path $gameRoot $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $stagingData -Force
    }
}

# Mod dosyalarini toparla
$stageDataDir = Join-Path $stagingData 'Data'
New-Item -ItemType Directory -Path $stageDataDir -Force | Out-Null

$modsRoot = Join-Path $labRoot 'mod-organizer/mods'
$mods = Get-ChildItem -LiteralPath $modsRoot -Directory | Sort-Object Name
foreach ($mod in $mods) {
    Get-ChildItem -Path $mod.FullName -Recurse -File | Where-Object { $_.Extension -ne '.pdb' } | ForEach-Object {
        $rel = $_.FullName.Substring($mod.FullName.Length + 1)
        $dest = Join-Path $stageDataDir $rel
        $parent = Split-Path -Parent $dest
        if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
    }
}

# Varsayilan baglanti ayari
$httpPort = if ($lock.serverArtifact.port) { 3000 } else { 3000 }
$settingsJson = @"
{
  "gameData": {
    "profileId": $ProfileId
  },
  "master": "",
  "server-ip": "$ServerAddress",
  "server-port": 7777,
  "server-http-url": "http://$($ServerAddress):3000",
  "server-info-ignore": true,
  "server-master-key": null,
  "ignoreLoadOrderMismatch": false
}
"@
$settingsPath = Join-Path $stageDataDir 'Platform/Plugins/skymp5-client-settings.txt'
[IO.File]::WriteAllText($settingsPath, $settingsJson, [Text.UTF8Encoding]::new($false))

# SSEDisplayTweaks 1280x720 windowed ayari
$dtIni = Join-Path $stageDataDir 'SKSE/Plugins/SSEDisplayTweaks.ini'
if (Test-Path -LiteralPath $dtIni) {
    $c = Get-Content -LiteralPath $dtIni -Raw
    $c = $c -replace '(?m)^#?\s*Fullscreen\s*=.*$', 'Fullscreen=false'
    $c = $c -replace '(?m)^#?\s*Borderless\s*=.*$', 'Borderless=true'
    $c = $c -replace '(?m)^#?\s*Resolution\s*=.*$', 'Resolution=1280x720'
    [IO.File]::WriteAllText($dtIni, $c, [Text.UTF8Encoding]::new($false))
}

# Installed version bilgisini de paket icine ekle
$modpackVer = Join-Path $projectRoot "$($lock.serverArtifact.runtimeDirectory)/data/modpack-version.json"
if (Test-Path -LiteralPath $modpackVer) {
    Copy-Item -LiteralPath $modpackVer -Destination (Join-Path $stagingData 'installed-version.json') -Force
}

Write-Output "3. SkyMPTR-Data.zip arsivi olusturuluyor..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
$dataZip = Join-Path $buildDir 'SkyMPTR-Data.zip'
if (Test-Path -LiteralPath $dataZip) { Remove-Item -LiteralPath $dataZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingData, $dataZip, [System.IO.Compression.CompressionLevel]::Fastest, $false)
Remove-Item -LiteralPath $stagingData -Recurse -Force

# Beni Oku dosyasi
$readme = @"
========================================================================
            SkyMP TR - Stock Game Cok Oyunculu Baslatici
========================================================================

Bu paket, arkadasinizla veya toplulukla SkyMP TR oynamaniz icin gereken
otomatik Stock Game kurulumunu ve guncelleyiciyi icerir.

Ozellikler:
- Orijinal Steam oyununuza kesinlikle dokunmaz; ayri bir Stock Game kurar.
- Eger Steam'deki Skyrim surumunuz farkliysa (1.6.1179 vb.), otomatik
  olarak 1.6.1170 surumune dusurur (downgrade).
- SKSE, Engine Fixes, Display Tweaks ve SkyMP modlarini tek tikla yukler.
- Otomatik Oyuncu Kimligi: Profil ID'niz otomatik uretilir ve kalici saklanir.
- Otomatik Mod Guncelleyici: Sunucu sahibi mod eklediginde baslatici yeni
  surumu aninda gorur ve tek tikla modlarinizi sunucuyla esitler.

Nasil Kullanilir?
1. Bu zip paketini bilgisayarinizda herhangi bir yere cikarin (orn: Masaustu).
2. 'SkyMPTR-Launcher.exe' uygulamasina cift tiklayin.
3. Baslatici Steam oyununuzu otomatik bulacaktir (bulamazsa Gozat ile secin).
4. Sunucu IP adresini (Radmin VPN IP) girin.
5. 'KURULUMU YAP VE OYNA' butonuna basin.
6. Kurulum tamamlandiginda 'OYUNA BASLA' butonuna basarak dogrudan sunucuya
   baglanin!
7. Sunucuda yeni modlar eklendiginde baslatici 'GUNCELLEMEYI INDIR' uyarisi
   verecektir. Guncellemeyi yaparak oyuna devam edebilirsiniz.
"@
[IO.File]::WriteAllText((Join-Path $buildDir 'BENI_OKU.txt'), $readme, [Text.Encoding]::GetEncoding(1254))

Write-Output "4. Dagitim paketi paketleniyor ($OutputPackage)..."
$finalZip = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputPackage))
$finalDir = Split-Path -Parent $finalZip
if (-not (Test-Path -LiteralPath $finalDir)) { New-Item -ItemType Directory -Path $finalDir -Force | Out-Null }
if (Test-Path -LiteralPath $finalZip) { Remove-Item -LiteralPath $finalZip -Force }

[System.IO.Compression.ZipFile]::CreateFromDirectory($buildDir, $finalZip, [System.IO.Compression.CompressionLevel]::Fastest, $false)
Remove-Item -LiteralPath $buildDir -Recurse -Force

$mb = [math]::Round(((Get-Item -LiteralPath $finalZip).Length / 1MB), 2)
Write-Output "========================================================"
Write-Output "Tebrikler! SkyMP TR Stock Game Baslatici Paketi Hazir!"
Write-Output "Konum: $finalZip ($mb MB)"
Write-Output "========================================================"
