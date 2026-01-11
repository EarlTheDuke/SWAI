# SWAI - SolidWorks Artificial Intelligence

> Transform natural language descriptions into SolidWorks 3D models using AI

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![SolidWorks](https://img.shields.io/badge/SolidWorks-2020--2025-red)
![License](https://img.shields.io/badge/License-MIT-green)
![Status](https://img.shields.io/badge/Status-Beta-orange)

## Overview

SWAI is a conversational AI application that translates English descriptions into SolidWorks parts and assemblies. Simply describe what you want to create, and SWAI handles the CAD operations for you.

### Demo

```
You: "Create a box 10 x 5 x 2 inches"

SWAI: ✓ Created box: 10" x 5" x 2"

📋 SolidWorks API Preview:
   // Step 1: Create new part document
   swModel = swApp.NewDocument(...)
   
   // Step 2: Create new sketch on Top Plane
   swModel.Extension.SelectByID2("Top Plane", "PLANE", ...)
   swModel.SketchManager.InsertSketch(true)
   
   // Step 3: Draw rectangle (-0.127, -0.0635) to (0.127, 0.0635) meters
   swModel.SketchManager.CreateCornerRectangle(...)
   
   // Step 4: Extrude sketch 0.0508 m (2 inches)
   swModel.FeatureManager.FeatureExtrusion3(...)
```

## Current Status (Beta)

| Feature | Status | Notes |
|---------|--------|-------|
| Natural Language Parsing | ✅ Working | xAI Grok, OpenAI GPT-4, Claude |
| API Code Preview | ✅ Working | See exact SolidWorks API calls |
| Mock Mode Testing | ✅ Working | Test without SolidWorks |
| Real SolidWorks Integration | ⚠️ Ready to Test | Needs testing with licensed SW |
| Basic Shapes (box, cylinder) | ✅ Implemented | |
| Features (holes, fillets, chamfers) | ✅ Implemented | |
| Assemblies & Mates | ✅ Implemented | |
| Complex Patterns | ⚠️ Basic | May need refinement |

## Quick Start for R&D Team

### Prerequisites

- **Windows 10/11**
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SolidWorks 2020-2025** with API access (for real testing)
- **AI API Key** (xAI, OpenAI, or Azure OpenAI)

### Step 1: Clone and Build

```powershell
git clone https://github.com/EarlTheDuke/SWAI.git
cd SWAI
dotnet build
```

### Step 2: Configure API Key

```powershell
copy appsettings.sample.json appsettings.json
```

Edit `appsettings.json` and add your API key:

```json
{
  "AI": {
    "Provider": "xAI",
    "ApiKey": "xai-YOUR_ACTUAL_KEY_HERE",
    "Model": "grok-4-1-fast-reasoning"
  }
}
```

**Supported AI Providers:**
- **xAI Grok** (recommended) - Fast reasoning, good for CAD commands
- **OpenAI GPT-4o** - Excellent understanding
- **Azure OpenAI** - Enterprise option
- **Anthropic Claude** - Alternative option

### Step 3: Choose Mode

#### Option A: Mock Mode (No SolidWorks Required)
Test the AI parsing without SolidWorks installed:

```json
{
  "SolidWorks": {
    "UseMock": true
  }
}
```

#### Option B: Real SolidWorks Mode
Test with actual SolidWorks:

```json
{
  "SolidWorks": {
    "UseMock": false,
    "AutoConnect": true,
    "StartVisible": true
  }
}
```

### Step 4: Run

```powershell
# From project root
dotnet run --project src/SWAI.App

# Or run the built executable
.\src\SWAI.App\bin\Debug\net8.0-windows\SWAI.exe
```

## How It Works

### Architecture

```
┌────────────────────────────────────────────────────────────────────┐
│                         User Input                                  │
│              "Create a box 10 x 5 x 2 inches"                      │
└─────────────────────────────┬──────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                    AI Service (xAI/OpenAI)                         │
│  - Parses natural language                                         │
│  - Returns structured JSON with intent + parameters                │
└─────────────────────────────┬──────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                    Command Executor                                 │
│  - Converts JSON to ISwaiCommand objects                           │
│  - Generates API preview (in mock mode)                            │
│  - Executes against SolidWorks COM API                             │
└─────────────────────────────┬──────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│                    SolidWorks API                                   │
│  - CreateCornerRectangle, CreateCircleByRadius                     │
│  - FeatureExtrusion3, FeatureFillet3, HoleWizard5                  │
│  - All coordinates in METERS (converted from user units)           │
└────────────────────────────────────────────────────────────────────┘
```

### SolidWorks API Integration

The app uses **COM automation** to control SolidWorks:

```csharp
// Connect to running SolidWorks or start new instance
var swType = Type.GetTypeFromProgID("SldWorks.Application");
_swApp = Activator.CreateInstance(swType);
_swApp.Visible = true;

// Create a sketch on the Top plane
swModel.Extension.SelectByID2("Top Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
swModel.SketchManager.InsertSketch(true);

// Draw a rectangle (coordinates in METERS)
swModel.SketchManager.CreateCornerRectangle(0, 0, 0, 0.254, 0.127, 0);

// Extrude the sketch
swModel.FeatureManager.FeatureExtrusion3(...);
```

**Important:** SolidWorks API uses meters internally. SWAI automatically converts from user units (inches, mm) to meters.

## Test Commands

Try these commands to test the system:

### Basic Shapes
```
Create a box 10 x 5 x 2 inches
Create a cylinder 3 inch diameter, 6 inches tall
Make a plate 200mm x 100mm x 5mm
```

### Features
```
Add a 0.5 inch fillet to all edges
Add a 0.25 inch chamfer
Add a 1 inch diameter hole through the center
```

### Multi-Step
```
Create a box 6 x 4 x 2 inches with a 1 inch hole through the center
```

### Patterns
```
Add 4 holes in a row, 2 inches apart
Create a circular pattern of 8 holes
```

## Project Structure

```
SWAI/
├── src/
│   ├── SWAI.App/           # WPF Application (UI)
│   │   ├── Views/          # XAML windows
│   │   └── ViewModels/     # MVVM view models
│   ├── SWAI.Core/          # Domain models & interfaces
│   │   ├── Commands/       # ISwaiCommand implementations
│   │   ├── Models/         # Geometry, Units, Features
│   │   └── Interfaces/     # Service contracts
│   ├── SWAI.AI/            # AI/LLM integration
│   │   ├── Services/       # StructuredAIService
│   │   └── Models/         # CommandSchema (JSON parsing)
│   └── SWAI.SolidWorks/    # SolidWorks COM API
│       └── Services/       # PartService, SketchService, etc.
├── tests/
│   ├── SWAI.Core.Tests/
│   └── SWAI.AI.Tests/
├── appsettings.sample.json # Template config (copy to appsettings.json)
└── README.md
```

## Known Issues & Limitations

1. **Complex Patterns**: Grid patterns and mirror operations may need refinement
2. **Edge Selection**: Fillets/chamfers apply to "all edges" - specific edge selection coming
3. **Assemblies**: Basic mate support; complex mates need testing
4. **Error Recovery**: Some SolidWorks errors may need manual intervention

## Testing Checklist for R&D

- [ ] Build succeeds with `dotnet build`
- [ ] App launches in mock mode
- [ ] AI connection works (green indicator in top-right)
- [ ] Basic commands parse correctly
- [ ] API preview shows reasonable SolidWorks code
- [ ] (With SW) App connects to SolidWorks
- [ ] (With SW) Box creation works
- [ ] (With SW) Cylinder creation works
- [ ] (With SW) Hole creation works
- [ ] (With SW) Fillet creation works

## Troubleshooting

### "AI unavailable - using offline parsing"
- Check your API key in appsettings.json
- Verify network connectivity
- Check API provider status

### App won't connect to SolidWorks
- Ensure SolidWorks is running OR set `AutoConnect: true`
- Check that SolidWorks API is enabled (Tools > Add-Ins > SolidWorks API SDK)
- Run SWAI as Administrator if COM registration issues

### Build errors
```powershell
# Clean and rebuild
dotnet clean
dotnet build
```

### Multiple windows opening
- This was fixed - ensure you have the latest code

## API Reference

Key SolidWorks API methods used:

| SWAI Operation | SolidWorks API | Documentation |
|----------------|----------------|---------------|
| Select plane | `Extension.SelectByID2` | [Link](https://help.solidworks.com/2025/english/api/sldworksapi/) |
| Create sketch | `SketchManager.InsertSketch` | |
| Draw rectangle | `SketchManager.CreateCornerRectangle` | |
| Draw circle | `SketchManager.CreateCircleByRadius` | |
| Extrude | `FeatureManager.FeatureExtrusion3` | |
| Fillet | `FeatureManager.FeatureFillet3` | |
| Hole | `FeatureManager.HoleWizard5` | |

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes
4. Run tests: `dotnet test`
5. Submit a PR

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contact

Questions? Issues? Open a GitHub issue or contact the development team.

---

**SWAI** - Making CAD accessible through conversation.
