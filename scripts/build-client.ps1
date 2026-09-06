$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$clientRoot = Join-Path $projectRoot 'skymp5-client'
$npmCommand = Get-Command npm.cmd -ErrorAction Stop
$previousDeploy = $env:DEPLOY_PLUGIN
$previousZip = $env:ZIP_PLUGIN

Push-Location -LiteralPath $clientRoot
try {
    # Build into the ignored build directory without touching a game installation.
    $env:DEPLOY_PLUGIN = 'false'
    $env:ZIP_PLUGIN = 'false'
    & $npmCommand.Source exec --yes --package=yarn@1.22.22 -- yarn install --frozen-lockfile
    if ($LASTEXITCODE -ne 0) { throw 'Client dependency installation failed.' }
    & $npmCommand.Source exec --yes --package=yarn@1.22.22 -- yarn build
    if ($LASTEXITCODE -ne 0) { throw 'Client TypeScript/webpack build failed.' }
}
finally {
    $env:DEPLOY_PLUGIN = $previousDeploy
    $env:ZIP_PLUGIN = $previousZip
    Pop-Location
}
