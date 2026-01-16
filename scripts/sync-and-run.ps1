# SWAI Sync and Run Script (for Work PC)
# Pulls latest changes from GitHub and runs the app

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SWAI Sync & Run" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Push-Location $rootDir

try {
    # Check for uncommitted changes
    $status = git status --porcelain
    if ($status) {
        Write-Host "`nYou have local changes:" -ForegroundColor Yellow
        git status --short
        Write-Host ""
        $response = Read-Host "Stash changes and continue? (y/n)"
        if ($response -eq 'y') {
            git stash
            Write-Host "Changes stashed." -ForegroundColor Green
        } else {
            Write-Host "Aborting." -ForegroundColor Red
            exit 1
        }
    }
    
    # Pull latest
    Write-Host "`nPulling latest from GitHub..." -ForegroundColor Yellow
    git pull origin master
    if ($LASTEXITCODE -ne 0) { throw "Git pull failed" }
    
    # Show recent commits
    Write-Host "`nRecent changes:" -ForegroundColor Yellow
    git log --oneline -5
    
    # Build
    Write-Host "`nBuilding..." -ForegroundColor Yellow
    dotnet build -c Debug
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    
    # Check for appsettings.json
    $appSettings = "src\SWAI.App\bin\Debug\net8.0-windows\appsettings.json"
    if (-not (Test-Path $appSettings)) {
        Write-Host "`nCreating appsettings.json from template..." -ForegroundColor Yellow
        Copy-Item "appsettings.sample.json" $appSettings
        Write-Host "IMPORTANT: Edit $appSettings and add your API key!" -ForegroundColor Red
        notepad $appSettings
        Read-Host "Press Enter after saving your API key"
    }
    
    # Also copy to root bin if needed
    $rootBin = "src\SWAI.App\bin\Debug\net8.0-windows\"
    if (Test-Path "appsettings.json") {
        Copy-Item "appsettings.json" $rootBin -Force
    }
    
    # Run
    Write-Host "`nStarting SWAI..." -ForegroundColor Green
    Write-Host "Make sure SolidWorks is running!" -ForegroundColor Yellow
    Write-Host ""
    
    & "src\SWAI.App\bin\Debug\net8.0-windows\SWAI.exe"
    
} finally {
    Pop-Location
}
