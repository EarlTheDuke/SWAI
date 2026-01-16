# SWAI Test Results Capture (for Work PC)
# Captures test results, logs, and screenshots for review

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SWAI Test Results Capture" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Push-Location $rootDir

try {
    # Create test-results folder
    $timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
    $resultsDir = "test-results\$timestamp"
    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
    
    Write-Host "`nCapturing test results to: $resultsDir" -ForegroundColor Yellow
    
    # Copy logs
    $logsDir = "src\SWAI.App\bin\Debug\net8.0-windows\logs"
    if (Test-Path $logsDir) {
        Write-Host "Copying logs..." -ForegroundColor Yellow
        Copy-Item -Path "$logsDir\*" -Destination $resultsDir -Recurse -Force
    }
    
    # Get git info
    Write-Host "Recording git state..." -ForegroundColor Yellow
    git log -1 --format="Commit: %H%nDate: %ai%nMessage: %s" > "$resultsDir\git-info.txt"
    
    # Prompt for test notes
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  Enter Test Notes" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Enter your test observations (press Enter twice to finish):" -ForegroundColor Yellow
    Write-Host ""
    
    $notes = @()
    $emptyLines = 0
    while ($emptyLines -lt 1) {
        $line = Read-Host
        if ($line -eq "") {
            $emptyLines++
        } else {
            $emptyLines = 0
            $notes += $line
        }
    }
    
    # Create test report
    $report = @"
# SWAI Test Report
Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Tester: $env:USERNAME
Computer: $env:COMPUTERNAME

## Git State
$(git log -1 --format="Commit: %h - %s")

## Test Notes
$($notes -join "`n")

## Checklist
- [ ] App connected to SolidWorks
- [ ] Box creation works
- [ ] Cylinder creation works
- [ ] Hole creation works
- [ ] Fillet creation works
- [ ] Dimensions correct
- [ ] No crashes

## Issues Found


## Screenshots
(Add screenshot files to this folder)
"@
    
    Set-Content -Path "$resultsDir\TEST-REPORT.md" -Value $report
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  Test results captured!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nResults saved to: $resultsDir" -ForegroundColor White
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "  1. Add any screenshots to $resultsDir" -ForegroundColor White
    Write-Host "  2. Edit TEST-REPORT.md with more details" -ForegroundColor White
    Write-Host "  3. Commit and push:" -ForegroundColor White
    Write-Host "     git add test-results" -ForegroundColor Gray
    Write-Host "     git commit -m 'Test results from work PC'" -ForegroundColor Gray
    Write-Host "     git push" -ForegroundColor Gray
    
    # Open folder
    explorer $resultsDir
    
} finally {
    Pop-Location
}
