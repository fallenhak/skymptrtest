param([ValidateRange(1, 2147483647)][int]$ProfileId = 1)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$lock = Get-Content -LiteralPath (Join-Path $root 'sources.lock.json') -Raw | ConvertFrom-Json
$server = Join-Path $root $lock.serverArtifact.runtimeDirectory
$lab = Join-Path $root "$($lock.gameLab.directory)/lab-player-$ProfileId"
$game = Join-Path $lab 'game'
$processes = Get-CimInstance Win32_Process
if ($processes | Where-Object { ($_.Name -eq 'node.exe' -and $_.CommandLine -match 'skymp5-server\.js') -or ($_.ExecutablePath -and $_.ExecutablePath.StartsWith($lab + '\', [StringComparison]::OrdinalIgnoreCase)) }) {
    throw 'Close the test server and lab Skyrim/MO2 before deploying. Other game installations are preserved.'
}
$files = @(
    @('build/dist/server/dist_back/skymp5-server.js', (Join-Path $server 'dist_back/skymp5-server.js')),
    @('build/dist/server/dist_back/skymp5-server.js.map', (Join-Path $server 'dist_back/skymp5-server.js.map')),
    @('scripts/gamemode.js', (Join-Path $server 'gamemode.js')),
    @('build/dist/client/Data/Platform/Plugins/skymp5-client.js', (Join-Path $game 'Data/Platform/Plugins/skymp5-client.js')),
    @('build/dist/client/Data/Platform/Plugins/skymp5-client.js', (Join-Path $lab 'mod-organizer/mods/05_SkyMP_Client/Platform/Plugins/skymp5-client.js'))
)
foreach ($pair in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $pair[0]) -PathType Leaf)) { throw "Build missing: $($pair[0])" }
    if (-not (Test-Path -LiteralPath (Split-Path -Parent $pair[1]) -PathType Container)) { throw "Unprepared destination: $($pair[1])" }
}
$backup = Join-Path $root ('.local/code-backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
$manifest = @()
$index = 0
foreach ($pair in $files) {
    $source = Join-Path $root $pair[0]
    $destination = $pair[1]
    if (Test-Path -LiteralPath $destination) { Copy-Item -LiteralPath $destination -Destination (Join-Path $backup "$index.bak") }
    Copy-Item -LiteralPath $source -Destination $destination -Force
    $hash = (Get-FileHash -LiteralPath $source).Hash
    if ((Get-FileHash -LiteralPath $destination).Hash -ne $hash) { throw "Copy verification failed: $destination" }
    $manifest += @{ backup = "$index.bak"; destination = $destination; sha256 = $hash }
    $index++
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
Write-Output "Deployed server/client code. Settings, native binaries and saves preserved. Backup: $backup"
