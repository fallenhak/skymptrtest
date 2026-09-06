$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceLock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$serverRoot = Join-Path $projectRoot $sourceLock.serverArtifact.runtimeDirectory
$nodeCommand = Get-Command node.exe -ErrorAction Stop

if (-not (Test-Path -LiteralPath (Join-Path $serverRoot 'server-settings.json'))) {
    throw 'Local server files have not been prepared. See docs/LOCAL_SERVER_TR.md.'
}

Push-Location -LiteralPath $serverRoot
try {
    & $nodeCommand.Source 'dist_back/skymp5-server.js'
    $serverExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $serverExitCode
