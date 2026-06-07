<#
.SYNOPSIS
  Builds a self-contained release of SyncLabs Hub and packs a Velopack installer.

.EXAMPLE
  ./publish-release.ps1 -Version 1.0.1
#>
param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root     = $PSScriptRoot
$project  = Join-Path $root "src/SyncLabsHub.App/SyncLabsHub.App.csproj"
$publish  = Join-Path $root "publish"
$releases = Join-Path $root "releases"

Write-Host "==> Publishing $Version ($Runtime, self-contained)..." -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish $project -c Release -r $Runtime --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Locate the Velopack CLI (PATH, else the global tools folder).
$vpk = "vpk"
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    $vpk = Join-Path $env:USERPROFILE ".dotnet\tools\vpk.exe"
}
if (-not (Test-Path $vpk) -and $vpk -ne "vpk") {
    throw "vpk not found. Install it: dotnet tool install -g vpk --version 0.0.1298"
}

Write-Host "==> Packing Velopack release..." -ForegroundColor Cyan
& $vpk pack `
    --packId SyncLabsHub `
    --packTitle "SyncLabs Hub" `
    --packAuthors "SyncLabs" `
    --packVersion $Version `
    --packDir $publish `
    --mainExe SyncLabsHub.exe `
    --outputDir $releases
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "==> Done. Installer + release files are in: $releases" -ForegroundColor Green
