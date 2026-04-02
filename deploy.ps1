<#
.SYNOPSIS
    Build and deploy ImprovedGarrisons mod to Bannerlord Modules folder.
.DESCRIPTION
    Builds the project and copies all module files to the game's Modules directory.
.PARAMETER GameFolder
    Path to your Bannerlord installation. Defaults to Steam's default location.
.PARAMETER Configuration
    Build configuration. Default: Release.
#>
param(
    [string]$GameFolder = "C:\Games\steamapps\common\Mount & Blade II Bannerlord",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ModuleName = "ImprovedGarrisons"
$TargetModuleDir = Join-Path $GameFolder "Modules\$ModuleName"

Write-Host "=== Building $ModuleName ===" -ForegroundColor Cyan

# Build the project
dotnet build "src\ImprovedGarrisons\ImprovedGarrisons.csproj" `
    -c $Configuration `
    -p:GameFolder="$GameFolder"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "=== Deploying to $TargetModuleDir ===" -ForegroundColor Cyan

# Create target directories
$binDir = Join-Path $TargetModuleDir "bin\Win64_Shipping_Client"

New-Item -ItemType Directory -Path $binDir -Force | Out-Null

# Copy SubModule.xml
Copy-Item "Module\SubModule.xml" -Destination $TargetModuleDir -Force

# Copy built DLL
$builtDll = "src\ImprovedGarrisons\bin\$Configuration\net472\ImprovedGarrisons.dll"
if (Test-Path $builtDll) {
    Copy-Item $builtDll -Destination $binDir -Force
}

# Copy Harmony DLL if needed
$harmonyDll = "src\ImprovedGarrisons\bin\$Configuration\net472\0Harmony.dll"
if (Test-Path $harmonyDll) {
    Copy-Item $harmonyDll -Destination $binDir -Force
}

Write-Host "=== Done! ===" -ForegroundColor Green
Write-Host "Module deployed to: $TargetModuleDir"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Launch Bannerlord"
Write-Host "  2. Go to Mods and enable 'Improved Garrisons'"
Write-Host "  3. Start or load a campaign"
Write-Host "  4. Own a fief to see auto-recruitment, training, and guard parties in action"
