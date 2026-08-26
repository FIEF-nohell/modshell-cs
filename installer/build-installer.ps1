<#
.SYNOPSIS
    Publishes modshell-hwtest self-contained and packages it into an MSI.

.DESCRIPTION
    Produces installer/output/modshell-hwtest-<version>.msi. The MSI installs
    per-machine into Program Files, adds a Start Menu shortcut, and registers
    the app in Apps & features so it can be uninstalled normally.

    Requires the WiX dotnet tool:  dotnet tool install --global wix

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
#>
[CmdletBinding()]
param(
    [string]$Version = "0.1.0",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"
$outputDir = Join-Path $PSScriptRoot "output"
$wxs = Join-Path $PSScriptRoot "modshell-hwtest.wxs"
$msi = Join-Path $outputDir "modshell-hwtest-$Version.msi"

Write-Host "Publishing $Configuration / $Runtime (self-contained)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
dotnet publish (Join-Path $repoRoot "modshell-hwtest.csproj") `
    -c $Configuration -r $Runtime --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$wixCmd = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixCmd) {
    $fallback = Join-Path $env:USERPROFILE ".dotnet\tools\wix.exe"
    if (Test-Path $fallback) {
        $wixCmd = $fallback
    } else {
        throw "The 'wix' tool was not found. Install it with: dotnet tool install --global wix"
    }
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "Building MSI..." -ForegroundColor Cyan
& $wixCmd build $wxs `
    -arch x64 `
    -d "AppVersion=$Version" `
    -d "PublishDir=$publishDir" `
    -d "RepoRoot=$repoRoot" `
    -o $msi
if ($LASTEXITCODE -ne 0) { throw "wix build failed with exit code $LASTEXITCODE" }

$sizeMb = [math]::Round((Get-Item $msi).Length / 1MB, 1)
Write-Host ""
Write-Host "Installer built: $msi ($sizeMb MB)" -ForegroundColor Green
Write-Host "Install it by double-clicking the MSI (it will prompt for administrator)." -ForegroundColor Green
