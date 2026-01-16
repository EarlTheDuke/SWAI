# SWAI Development Workflow

## Overview

This workflow allows you to develop on your home PC and test with real SolidWorks on your work PC.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        DEVELOPMENT CYCLE                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   HOME PC                          WORK PC                          │
│   ─────────                        ────────                         │
│                                                                     │
│   1. Edit code                                                      │
│   2. Test in Mock Mode                                              │
│   3. Review API Preview                                             │
│   4. Run: .\scripts\quick-push.ps1  ───────►  5. Run: .\scripts\sync-and-run.ps1
│                                               6. Test with SolidWorks
│                                               7. Run: .\scripts\capture-test-results.ps1
│   9. Review test results  ◄───────────────   8. Push results
│   10. Fix issues, goto 1                                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Initial Setup

### On Home PC (Development)

Already done! Just keep developing here.

### On Work PC (Testing)

**One-time setup:**

```powershell
# 1. Clone the repository
git clone https://github.com/EarlTheDuke/SWAI.git
cd SWAI

# 2. Create your config
copy appsettings.sample.json appsettings.json
notepad appsettings.json
# Add your API key, set UseMock: false

# 3. Build once to verify
dotnet build
```

## Daily Workflow

### Home PC: Make Changes

```powershell
# 1. Make your code changes

# 2. Test in mock mode
dotnet run --project src/SWAI.App

# 3. When ready, push to GitHub
.\scripts\quick-push.ps1
# Enter a commit message when prompted
```

### Work PC: Test with SolidWorks

```powershell
# 1. Pull latest and run
.\scripts\sync-and-run.ps1

# 2. Test with SolidWorks
#    - Try commands
#    - Check if parts are created correctly
#    - Note any issues

# 3. Capture test results
.\scripts\capture-test-results.ps1

# 4. (Optional) Push results back
git add test-results
git commit -m "Test results: [describe what you tested]"
git push
```

### Home PC: Review and Fix

```powershell
# 1. Pull test results
git pull

# 2. Review test-results folder
# 3. Fix any issues
# 4. Repeat cycle
```

## Alternative: Portable Release

If you don't want to install .NET SDK on your work PC:

```powershell
# On Home PC: Build a self-contained release
.\scripts\build-release.ps1

# Copy the 'release' folder to work PC via USB/network
# On Work PC: Just run START-SWAI.bat
```

## Scripts Reference

| Script | Where | Purpose |
|--------|-------|---------|
| `quick-push.ps1` | Home PC | Build, commit, push changes |
| `sync-and-run.ps1` | Work PC | Pull, build, run app |
| `capture-test-results.ps1` | Work PC | Save logs, notes, create report |
| `build-release.ps1` | Home PC | Create portable self-contained release |

## Tips

### Quick Iteration
- Keep both terminals open (Home PC and Work PC via remote if possible)
- Use short commit messages: "fix hole diameter" 
- Test one thing at a time

### Debugging Issues
- Check logs in `src\SWAI.App\bin\Debug\net8.0-windows\logs\`
- Use `capture-test-results.ps1` to collect everything
- Screenshot SolidWorks if parts look wrong

### Common Issues

**"Cannot connect to SolidWorks"**
- Start SolidWorks before running SWAI
- Or set `AutoConnect: true` in appsettings.json

**"Build failed"**
- Make sure .NET 8 SDK is installed
- Run `dotnet --version` to check

**"API key invalid"**
- Check appsettings.json has correct key
- Key should not have quotes inside the value

## SolidWorks Testing Checklist

For each test session, verify:

- [ ] SWAI connects to SolidWorks (top-right indicator)
- [ ] "Create a box 2x2x2 inches" creates correct box
- [ ] Dimensions match (measure in SW)
- [ ] "Create a cylinder 2 inch diameter 3 inches tall" works
- [ ] "Add a 0.25 inch fillet to all edges" works
- [ ] "Add a 0.5 inch hole through the center" works
- [ ] Part appears in SolidWorks Feature Tree
- [ ] Can save the part (.sldprt)
