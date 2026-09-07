param(
    [switch]$ForceRebuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json

$labRoot = Join-Path $projectRoot "$($lock.gameLab.directory)/lab-player-1"
$serverData = Join-Path $projectRoot "$($lock.serverArtifact.runtimeDirectory)/data"
$serverDataFull = [IO.Path]::GetFullPath($serverData)

if (-not (Test-Path -LiteralPath $labRoot)) {
    throw "lab-player-1 bulunamadi: $labRoot"
}
if (-not (Test-Path -LiteralPath $serverDataFull)) {
    New-Item -ItemType Directory -Path $serverDataFull -Force | Out-Null
}

$stagingDir = Join-Path $projectRoot '.local/modpack-staging'
if (Test-Path -LiteralPath $stagingDir) {
    Remove-Item -LiteralPath $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

Write-Output "1. MO2 modlari ve istemci bilesenleri toparlaniyor..."

# Kok calistiricilar
$gameRoot = Join-Path $labRoot 'game'
foreach ($f in @('skse64_loader.exe', 'skse64_1_6_1170.dll', 'd3dx9_42.dll')) {
    $src = Join-Path $gameRoot $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $stagingDir -Force
    }
}

# Modlar
$stageData = Join-Path $stagingDir 'Data'
New-Item -ItemType Directory -Path $stageData -Force | Out-Null

$modsRoot = Join-Path $labRoot 'mod-organizer/mods'
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

# Host kendi oyununu da esitle
Write-Output "2. Host lab-player-1/game/Data esitleniyor..."
$hostGameData = Join-Path $gameRoot 'Data'
Get-ChildItem -Path $stageData -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($stageData.Length + 1)
    $dest = Join-Path $hostGameData $rel
    $parent = Split-Path -Parent $dest
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    if (-not (Test-Path -LiteralPath $dest) -or (Get-Item -LiteralPath $dest).LastWriteTimeUtc -lt $_.LastWriteTimeUtc) {
        Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
    }
}

# Surum hesabi
$versionFile = Join-Path $serverDataFull 'modpack-version.json'
$currentVersion = 1
if (Test-Path -LiteralPath $versionFile) {
    try {
        $vJson = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
        $currentVersion = [int]$vJson.version + 1
    } catch {
        $currentVersion = 1
    }
}

# Modpack.zip olustur
Write-Output "3. Sunucu HTTP data klasorune modpack.zip paketleniyor..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
$targetZip = Join-Path $serverDataFull 'modpack.zip'
$tempZip = Join-Path $serverDataFull 'modpack.tmp.zip'
if (Test-Path -LiteralPath $tempZip) { Remove-Item -LiteralPath $tempZip -Force }

$fileCount = (Get-ChildItem -Path $stagingDir -Recurse -File).Count
[System.IO.Compression.ZipFile]::CreateFromDirectory($stagingDir, $tempZip, [System.IO.Compression.CompressionLevel]::Fastest, $false)
$newHash = (Get-FileHash -LiteralPath $tempZip -Algorithm SHA256).Hash
Remove-Item -LiteralPath $stagingDir -Recurse -Force

# Onceki surumle karsilastir
$oldHash = ""
$previousVersion = 0
if (Test-Path -LiteralPath $versionFile) {
    try {
        $vJson = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
        $previousVersion = [int]$vJson.version
        $oldHash = [string]$vJson.hash
    } catch {
        $previousVersion = 0
    }
}

if (-not $ForceRebuild -and $oldHash -eq $newHash -and (Test-Path -LiteralPath $targetZip)) {
    Remove-Item -LiteralPath $tempZip -Force
    Write-Output "Modlarda degisiklik tespit edilmedi. Mevcut surum v$previousVersion gecerli."
    return
}

$currentVersion = $previousVersion + 1
if (Test-Path -LiteralPath $targetZip) { Remove-Item -LiteralPath $targetZip -Force }
Move-Item -LiteralPath $tempZip -Destination $targetZip -Force

# modpack-version.json yaz
$versionInfo = [ordered]@{
    version = $currentVersion
    hash = $newHash
    archiveName = 'modpack.zip'
    sizeBytes = (Get-Item -LiteralPath $targetZip).Length
    fileCount = $fileCount
    updatedAtUtc = [DateTime]::UtcNow.ToString('o')
    description = "SkyMP TR Otomatik Mod Paketi v$currentVersion"
}
$utf8 = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($versionFile, ($versionInfo | ConvertTo-Json -Depth 4), $utf8)

$zipMb = [math]::Round(((Get-Item -LiteralPath $targetZip).Length / 1MB), 2)
Write-Output "=========================================================="
Write-Output "Mod paketi basariyla sunucuya esitlendi!"
Write-Output "Surum: v$currentVersion | Boyut: $zipMb MB | Dosya: $fileCount"
Write-Output "Sunucu endpoint: http://<sunucu-ip>:3000/modpack-version.json"
Write-Output "Oyuncular baslaticiyi actiginda otomatik guncelleme bildirimi alacak."
Write-Output "=========================================================="
