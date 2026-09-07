param(
    [string]$TagName = "",
    [string]$Title = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

Write-Output "=========================================="
Write-Output " SkyMP TR GitHub Release Yayinci"
Write-Output "=========================================="

# 1. GitHub CLI denetimi
try {
    $ghVersion = & gh --version
    Write-Output "[OK] GitHub CLI aktif."
} catch {
    throw "GitHub CLI (gh) bulunamadi. Lutfen 'winget install GitHub.cli' ile kurun veya gh oturumu acin."
}

# 2. Mod paketi ve version bilgisi
$serverData = Join-Path $projectRoot '.local/skymp-756fb86/server/data'
$versionFile = Join-Path $serverData 'modpack-version.json'
$modpackZip = Join-Path $serverData 'modpack.zip'

if (-not (Test-Path -LiteralPath $versionFile)) {
    Write-Output "Mod paketi bulunamadi, sync-server-modpack.ps1 calistiriliyor..."
    & (Join-Path $PSScriptRoot 'sync-server-modpack.ps1')
}

$versionJson = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
$versionNum = $versionJson.version
$fileCount = $versionJson.fileCount

if ([string]::IsNullOrEmpty($TagName)) {
    $TagName = "v$versionNum.0.0"
}
if ([string]::IsNullOrEmpty($Title)) {
    $Title = "SkyMP TR - Mod Paketi v$versionNum ve Guncel Baslatici"
}

# 3. Launcher ve paketlerin derlenmesi
if (-not $SkipBuild) {
    Write-Output "1. Launcher ve Stock Game paketleri derleniyor..."
    & (Join-Path $PSScriptRoot 'build-launcher.ps1')
}

$launcherExe = Join-Path $projectRoot 'dist/SkyMPTR-Launcher/SkyMPTR-Launcher.exe'
$dataZip = Join-Path $projectRoot 'dist/SkyMPTR-Launcher/SkyMPTR-Data.zip'
$fullInstaller = Join-Path $projectRoot 'dist/SkyMPTR-StockGame-Installer.zip'

$assets = @($launcherExe, $dataZip, $fullInstaller, $modpackZip, $versionFile)
foreach ($a in $assets) {
    if (-not (Test-Path -LiteralPath $a)) {
        throw "Gerekli release dosyasi bulunamadi: $a"
    }
}

# 4. GitHub Release olusturma veya guncelleme
Write-Output "2. GitHub Release olusturuluyor / yukleniyor: $TagName..."

$notes = "SkyMP TR Surum $TagName`n`nBu surum en guncel SkyMP TR istemcisini, mod paketini (v$versionNum, $fileCount dosya), 3D yakinlik sesli sohbetini ve oyun ici metin sohbetini icerir.`n`nDosyalar:`n- SkyMPTR-Launcher.exe: Tek basina calisabilen hafif baslatici (55 KB). Diger tum dosyalari GitHub'dan otomatik indirir.`n- SkyMPTR-Data.zip: SKSE, SkyMP istemcisi, Engine Fixes ve ekran ayarlari veri paketi.`n- SkyMPTR-StockGame-Installer.zip: Launcher ve tum veri paketini iceren eksiksiz cevrimdisi kurulum arsivi.`n- modpack.zip: Sunucuyla esit $fileCount dosyali mod paketi."

# Release kontrolu
$repo = 'fallenhak/skymptrtest'
$existingReleases = & gh release list -R $repo | Out-String
if ($existingReleases -match [regex]::Escape($TagName)) {
    Write-Output "Surum ($TagName) zaten mevcut, dosyalar guncelleniyor (--clobber)..."
    & gh release upload $TagName @assets -R $repo --clobber
} else {
    Write-Output "Yeni release yayinlaniyor: $TagName..."
    & gh release create $TagName @assets -R $repo --title $Title --notes $notes --latest
}

Write-Output "=========================================="
Write-Output "Tebrikler! Release basariyla yayinlandi!"
Write-Output "URL: https://github.com/fallenhak/skymptrtest/releases/tag/$TagName"
Write-Output "=========================================="
