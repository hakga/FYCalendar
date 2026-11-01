[CmdletBinding()]
param(
    [string]$RepositoryPath = 'C:\Users\hs.SONE\source\repos\hakga\FYCalendar',
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath $RepositoryPath).Path
$patchFile = Join-Path $PSScriptRoot 'FYCalendar-test-setup.patch'
if (-not (Test-Path -LiteralPath (Join-Path $repository 'App\App.csproj'))) {
    throw 'App/App.csproj was not found in the selected repository.'
}
if (-not (Test-Path -LiteralPath $patchFile)) {
    throw 'Keep FYCalendar-test-setup.patch next to this script.'
}

$actualRoot = & git -C $repository rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect the Git repository.' }
if ([System.IO.Path]::GetFullPath($actualRoot).TrimEnd('\', '/') -ne
    [System.IO.Path]::GetFullPath($repository).TrimEnd('\', '/')) {
    throw 'RepositoryPath must point to the root of FYCalendar.'
}

& git -C $repository apply --check -- $patchFile
if ($LASTEXITCODE -ne 0) {
    throw 'The patch does not match the current files. No changes were applied.'
}
if ($CheckOnly) {
    Write-Host 'Patch check passed. No changes were applied.'
    return
}

& git -C $repository apply -- $patchFile
if ($LASTEXITCODE -ne 0) { throw 'Could not apply the patch.' }

Write-Host 'Added App.Tests and App.E2ETests. See tests/README.md for setup and execution.'
Write-Host 'No packages, browsers, database migrations, commits, or pushes were run by this script.'
