# SWAI Build Release Script
# Creates a portable release for testing on another PC

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "..\release"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SWAI Release Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Push-Location $rootDir

try {
    # Clean previous release
    $releaseDir = Join-Path $rootDir $OutputDir
    if (Test-Path $releaseDir) {
        Write-Host "`nCleaning previous release..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force $releaseDir
    }
    
    # Build
    Write-Host "`nBuilding $Configuration..." -ForegroundColor Yellow
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    
    # Publish self-contained
    Write-Host "`nPublishing self-contained app..." -ForegroundColor Yellow
    dotnet publish src/SWAI.App/SWAI.App.csproj `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -o $releaseDir
    
    if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
    
    # Copy sample config
    Write-Host "`nCopying configuration template..." -ForegroundColor Yellow
    Copy-Item "appsettings.sample.json" "$releaseDir\appsettings.sample.json"
    
    # Create quick-start script for work PC
    $quickStart = @'
@echo off
echo ========================================
echo   SWAI - SolidWorks AI Assistant
echo ========================================
echo.

if not exist appsettings.json (
    echo Creating config from template...
    copy appsettings.sample.json appsettings.json
    echo.
    echo IMPORTANT: Edit appsettings.json and add your API key!
    echo Then run this script again.
    echo.
    notepad appsettings.json
    pause
    exit /b
)

echo Starting SWAI...
echo.
echo Make sure SolidWorks is running, or set AutoConnect: true
echo.
SWAI.exe
'@
    Set-Content -Path "$releaseDir\START-SWAI.bat" -Value $quickStart
    
    # Create test commands file
    $testCommands = @'
# SWAI Test Commands
# Copy and paste these into SWAI to test

## Basic Shapes
Create a box 10 x 5 x 2 inches
Create a cylinder 3 inch diameter 6 inches tall

## Features
Add a 0.25 inch fillet to all edges
Add a 0.5 inch hole through the center

## Multi-step
Create a box 6 x 4 x 2 inches with a 1 inch hole through the center

## Check these:
- [ ] Part appears in SolidWorks
- [ ] Dimensions are correct
- [ ] Features are created properly
'@
    Set-Content -Path "$releaseDir\TEST-COMMANDS.txt" -Value $testCommands
    
    # Create README for release
    $readmeRelease = @'
# SWAI Release Package

## Quick Start

1. Copy this entire folder to your work PC
2. Run START-SWAI.bat
3. First run will open appsettings.json - add your API key
4. Run START-SWAI.bat again to start the app

## Configuration

Edit appsettings.json:
- Set your AI API key (xAI, OpenAI, or Azure)
- Set UseMock: false for real SolidWorks testing
- Set UseMock: true for testing without SolidWorks

## Testing with SolidWorks

1. Start SolidWorks first (or set AutoConnect: true)
2. Run SWAI
3. Try commands from TEST-COMMANDS.txt
4. Check if parts are created correctly

## Logs

Check the logs folder for detailed output if issues occur.

## Reporting Issues

Note down:
- What command you typed
- What happened vs what you expected
- Any error messages
- Screenshot if helpful
'@
    Set-Content -Path "$releaseDir\README.txt" -Value $readmeRelease
    
    # Show results
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  Release created successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nLocation: $releaseDir" -ForegroundColor White
    Write-Host "`nFiles:" -ForegroundColor Yellow
    Get-ChildItem $releaseDir -Name | ForEach-Object { Write-Host "  $_" }
    
    Write-Host "`nTo deploy to work PC:" -ForegroundColor Cyan
    Write-Host "  1. Copy the 'release' folder to work PC" -ForegroundColor White
    Write-Host "  2. Run START-SWAI.bat" -ForegroundColor White
    Write-Host "  3. Add your API key when prompted" -ForegroundColor White
    
} finally {
    Pop-Location
}
