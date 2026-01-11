# SWAI Development Guide

## Architecture Overview

SWAI follows a clean architecture pattern with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      SWAI.App (WPF)                         │
│  - Views (XAML)                                             │
│  - ViewModels (MVVM with CommunityToolkit)                  │
│  - Dependency Injection setup                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      SWAI.AI                                │
│  - Semantic Kernel integration                              │
│  - Natural language parsing                                 │
│  - Intent detection                                         │
│  - Command generation                                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    SWAI.SolidWorks                          │
│  - COM API wrappers                                         │
│  - Service implementations                                  │
│  - Command executor                                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      SWAI.Core                              │
│  - Domain models (Dimension, Geometry, Features)            │
│  - Commands (CreateBox, AddFillet, etc.)                    │
│  - Interfaces (ISolidWorksService, IAIService)              │
│  - No external dependencies                                 │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
src/
├── SWAI.App/           # WPF Application entry point
│   ├── Views/          # XAML views
│   ├── ViewModels/     # MVVM view models
│   └── Resources/      # Icons, styles
│
├── SWAI.Core/          # Domain layer (no dependencies)
│   ├── Commands/       # Command definitions
│   ├── Configuration/  # Settings classes
│   ├── Interfaces/     # Service contracts
│   ├── Models/         # Domain entities
│   │   ├── Documents/  # Part, Assembly
│   │   ├── Features/   # Extrusion, Fillet, etc.
│   │   ├── Geometry/   # Point3D, Vector3D, Plane
│   │   ├── Sketch/     # SketchEntity, Profile
│   │   └── Units/      # Dimension, UnitSystem
│   └── Services/       # Core services
│
├── SWAI.AI/            # AI/LLM integration
│   ├── Parsing/        # Command parsing
│   ├── Prompts/        # LLM prompt templates
│   └── Services/       # AI service implementations
│
└── SWAI.SolidWorks/    # SolidWorks API integration
    ├── Adapters/       # COM interop adapters
    └── Services/       # SW service implementations
```

## Key Concepts

### Commands

All CAD operations are represented as commands. This enables:
- **Validation** before execution
- **Undo/Redo** support
- **Logging** and auditing
- **Testing** without SolidWorks

```csharp
public class CreateBoxCommand : SwaiCommandBase
{
    public Dimension Width { get; init; }
    public Dimension Length { get; init; }
    public Dimension Height { get; init; }
    // ...
}
```

### Dimensions

The `Dimension` type safely handles measurements with units:

```csharp
var width = Dimension.Parse("36 inches");
var length = Dimension.Inches(96);
var height = Dimension.Millimeters(19.05);

// Unit conversion
var widthInMm = width.ConvertTo(UnitSystem.Millimeters);

// Arithmetic
var total = width + length; // Units are handled automatically

// For SolidWorks API (always uses meters)
var metersValue = height.Meters;
```

### AI Pipeline

```
User Input → Intent Detection → Parameter Extraction → Command Building → Execution
```

1. **Intent Detection**: Determines what the user wants (CreateBox, AddFillet, etc.)
2. **Parameter Extraction**: Extracts dimensions, names, options
3. **Command Building**: Creates typed command objects
4. **Execution**: Runs commands against SolidWorks

### Mock Mode

When SolidWorks isn't available, the application runs in mock mode:

```json
{
  "SolidWorks": {
    "UseMock": true
  }
}
```

This allows:
- UI development and testing
- AI pipeline testing
- Demo/presentation mode

## Development Setup

### Prerequisites

- Visual Studio 2022+
- .NET 8 SDK
- SolidWorks 2025 (optional for development)

### Building

```bash
dotnet build SWAI.sln
```

### Running Tests

```bash
dotnet test
```

### Running the Application

```bash
dotnet run --project src/SWAI.App
```

## Adding New Features

### Adding a New Command

1. Define the command in `SWAI.Core/Commands/`:

```csharp
public class AddMyFeatureCommand : SwaiCommandBase
{
    public Dimension Param1 { get; init; }
    public override string CommandType => "MyFeature";
    public override string Description => $"Add my feature: {Param1}";
}
```

2. Add parsing logic in `SWAI.AI/Parsing/CommandParser.cs`

3. Add execution logic in `SWAI.SolidWorks/Services/CommandExecutor.cs`

4. Update intent detection in `SWAI.AI/Services/AIService.cs`

### Adding SolidWorks API Calls

1. Find the API method in SolidWorks API Help
2. Add to the appropriate service in `SWAI.SolidWorks/Services/`
3. Always wrap in `Task.Run()` for async operation
4. Check `_config.UseMock` to support mock mode

```csharp
public async Task<MyFeature> CreateMyFeatureAsync(params)
{
    if (!_config.UseMock)
    {
        await Task.Run(() =>
        {
            var swApp = _swService.GetApplication();
            // ... SolidWorks API calls
        });
    }
    return new MyFeature(...);
}
```

## Testing

### Unit Tests

Use xUnit with FluentAssertions:

```csharp
[Fact]
public void Parse_SimpleInput_ShouldSucceed()
{
    var result = _parser.ParseCreateBox("box 10x20x5 inches");
    result.Should().NotBeNull();
    result.Width.Value.Should().Be(10);
}
```

### Integration Tests

For SolidWorks integration tests, use `[Trait("Category", "Integration")]`:

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task CreatePart_WithSolidWorks_ShouldSucceed()
{
    // Requires running SolidWorks instance
}
```

## Configuration

Settings are in `appsettings.json`:

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "your-key",
    "Model": "gpt-4o"
  },
  "SolidWorks": {
    "UseMock": false,
    "AutoConnect": true
  }
}
```

## Logging

Uses Serilog with file and console sinks:

```csharp
_logger.LogInformation("Creating part: {Name}", partName);
_logger.LogError(ex, "Failed to execute command");
```

Logs are stored in: `%LOCALAPPDATA%\SWAI\logs\`

## Future Roadmap

- [ ] Assembly operations
- [ ] Drawing recognition (Vision API)
- [ ] Template library
- [ ] Batch processing
- [ ] Plugin system
