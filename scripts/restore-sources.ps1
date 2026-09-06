$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceLock = Get-Content -LiteralPath (Join-Path $projectRoot 'sources.lock.json') -Raw | ConvertFrom-Json
$gitCommand = Get-Command git.exe -ErrorAction Stop

foreach ($source in @($sourceLock.skymp, $sourceLock.customSkills)) {
    $checkout = Join-Path $projectRoot $source.localCheckout
    if (Test-Path -LiteralPath $checkout) {
        if (-not (Test-Path -LiteralPath (Join-Path $checkout '.git'))) {
            throw "Existing directory is not a source checkout: $checkout"
        }
        $actualCommit = & $gitCommand.Source -C $checkout rev-parse HEAD
        if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $source.commit) {
            throw "Existing checkout differs from the source lock; it was preserved: $checkout"
        }
        $localChanges = & $gitCommand.Source -C $checkout status --porcelain
        if ($LASTEXITCODE -ne 0) { throw "Could not inspect source checkout: $checkout" }
        if ($localChanges) { Write-Warning "Local changes were preserved in $checkout" }
        Write-Output "Source already at pinned commit: $($source.repository) $actualCommit"
        continue
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $checkout) -Force | Out-Null
    & $gitCommand.Source init --quiet $checkout
    if ($LASTEXITCODE -ne 0) { throw "Could not create source checkout: $checkout" }
    & $gitCommand.Source -C $checkout remote add origin $source.url
    if ($LASTEXITCODE -ne 0) { throw "Could not set source remote: $checkout" }
    & $gitCommand.Source -C $checkout fetch --depth 1 origin $source.commit
    if ($LASTEXITCODE -ne 0) { throw "Could not fetch pinned source: $($source.repository)" }
    & $gitCommand.Source -C $checkout -c advice.detachedHead=false checkout --detach FETCH_HEAD
    if ($LASTEXITCODE -ne 0) { throw "Could not check out pinned source: $($source.repository)" }
    $actualCommit = & $gitCommand.Source -C $checkout rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $actualCommit -ne $source.commit) {
        throw "Source verification failed: $($source.repository)"
    }
    Write-Output "Source restored: $($source.repository) $actualCommit"
}

Write-Output 'Research sources are ready. Submodules and build dependencies have not been installed.'
