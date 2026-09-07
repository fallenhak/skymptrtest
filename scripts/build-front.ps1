$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$previousOutput = $env:SKYMP_FRONT_OUTPUT
try {
    $env:SKYMP_FRONT_OUTPUT = Join-Path $projectRoot 'build/dist/client/Data/Platform/UI'
    Push-Location (Join-Path $projectRoot 'skymp5-front')
    try {
        & npm.cmd exec --yes --package=yarn@1.22.22 -- yarn install --frozen-lockfile
        if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency installation failed.' }
        & npm.cmd exec --yes --package=yarn@1.22.22 -- yarn build
        if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    } finally { Pop-Location }
} finally { $env:SKYMP_FRONT_OUTPUT = $previousOutput }
Write-Output 'Frontend built from the tracked source into build/dist/client/Data/Platform/UI.'
