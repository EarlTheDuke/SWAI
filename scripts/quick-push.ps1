# SWAI Quick Push Script (for Home PC)
# Commits and pushes changes to GitHub

param(
    [string]$Message = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SWAI Quick Push" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Push-Location $rootDir

try {
    # Show status
    Write-Host "`nChanged files:" -ForegroundColor Yellow
    git status --short
    
    $changes = git status --porcelain
    if (-not $changes) {
        Write-Host "`nNo changes to commit." -ForegroundColor Green
        exit 0
    }
    
    # Build to verify
    Write-Host "`nBuilding to verify..." -ForegroundColor Yellow
    dotnet build -c Debug --verbosity quiet
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "Build failed! Fix errors before pushing." -ForegroundColor Red
        exit 1
    }
    Write-Host "Build OK" -ForegroundColor Green
    
    # Get commit message
    if (-not $Message) {
        Write-Host ""
        $Message = Read-Host "Commit message"
    }
    
    if (-not $Message) {
        Write-Host "No message provided. Aborting." -ForegroundColor Red
        exit 1
    }
    
    # Stage all changes (except appsettings.json)
    Write-Host "`nStaging changes..." -ForegroundColor Yellow
    git add -A
    
    # Make sure appsettings.json is not staged
    git reset HEAD appsettings.json 2>$null
    
    # Commit
    Write-Host "Committing..." -ForegroundColor Yellow
    git commit -m $Message
    if ($LASTEXITCODE -ne 0) { throw "Commit failed" }
    
    # Push
    Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
    git push origin master
    if ($LASTEXITCODE -ne 0) { throw "Push failed" }
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  Pushed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nOn work PC, run:" -ForegroundColor Cyan
    Write-Host "  .\scripts\sync-and-run.ps1" -ForegroundColor White
    
} finally {
    Pop-Location
}
