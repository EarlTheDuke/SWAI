# SWAI - SolidWorks Artificial Intelligence

> Transform natural language descriptions into SolidWorks 3D models using AI

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![SolidWorks](https://img.shields.io/badge/SolidWorks-2025-red)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

SWAI is a conversational AI application that translates English descriptions into SolidWorks parts and assemblies. Simply describe what you want to create, and SWAI handles the CAD operations for you.

### Example

```
You: "Create a part 36 inches wide, 96 inches long, and 0.75 inches thick"

SWAI: Creating rectangular part...
      ✓ New part document created
      ✓ Base sketch: 36" x 96" rectangle
      ✓ Extruded to 0.75" thickness
      ✓ Part ready: "Part_36x96x0.75.SLDPRT"
```

## Features

- **Natural Language Input**: Describe parts in plain English
- **Unit Flexibility**: Supports inches, millimeters, fractional dimensions (e.g., "3/4 inch")
- **Incremental Building**: Add features conversationally ("now add 2-inch edges")
- **Multiple Export Formats**: SLDPRT, STEP, STL, IGES
- **Session Memory**: Persistent sessions with design state tracking and auto-snapshots
- **Command Preview**: Structured preview with risk assessment, confidence scores, and Execute/Edit/Cancel
- **Multiple AI Providers**: OpenAI, Azure OpenAI, xAI Grok, Anthropic Claude with fallback support
- **Special Commands**: `/help`, `/summarize`, `/undo`, `/history`, `/sessions` and more
- **Mock Mode**: Development without SolidWorks with recording/playback capability

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      SWAI Application                        │
├─────────────────────────────────────────────────────────────┤
│  SWAI.App          │  WPF UI with MVVM pattern              │
│  SWAI.AI           │  Semantic Kernel + LLM integration     │
│  SWAI.Core         │  Domain models, commands, interfaces   │
│  SWAI.SolidWorks   │  SolidWorks COM API abstraction        │
└─────────────────────────────────────────────────────────────┘
```

## Prerequisites

- **SolidWorks 2025** (or later) with API access
- **.NET 8.0 SDK**
- **Visual Studio 2022** (recommended)
- **OpenAI API Key** (or Azure OpenAI)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/EarlTheDuke/SWAI.git
cd SWAI
```

### 2. Configure API Keys

Copy the sample configuration and add your API key:

```bash
copy appsettings.sample.json appsettings.json
```

Edit `appsettings.json`:

```json
{
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "your-openai-api-key-here",
    "Model": "gpt-4o"
  }
}
```

### 3. Build and Run

```bash
dotnet build
dotnet run --project src/SWAI.App
```

## Configuration

### Basic Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `AI:Provider` | Primary AI provider | OpenAI |
| `AI:ApiKey` | Your API key | (required) |
| `AI:Model` | Model to use | gpt-4o |
| `AI:FallbackProvider` | Fallback if primary fails | xAI |
| `SolidWorks:AutoConnect` | Connect to SW on startup | true |
| `SolidWorks:DefaultUnits` | Default unit system | Inches |
| `Mock:Enabled` | Enable mock mode | false |

### Multi-Provider Configuration

```json
{
  "AI": {
    "Provider": "OpenAI",
    "FallbackProvider": "xAI",
    "Providers": {
      "OpenAI": { "ApiKey": "sk-...", "Model": "gpt-4o" },
      "xAI": { "ApiKey": "xai-...", "Model": "grok-4", "BaseUrl": "https://api.x.ai/v1" },
      "Anthropic": { "ApiKey": "sk-ant-...", "Model": "claude-opus-4-5-20250514" }
    }
  }
}
```

## Supported Commands

### Part Creation
- "Create a box 10x20x5 inches"
- "Make a cylinder 2 inch diameter, 6 inches tall"
- "Create a plate 500mm x 300mm x 10mm"
- "Make a part 36" wide, 96" long, 3/4" thick"

### Feature Operations
- "Add a 1-inch fillet to all edges"
- "Add a 0.5 inch chamfer"
- "Cut a 2-inch hole in the center"
- "Add a through hole 1/2 inch diameter"
- "Extrude the sketch 3 inches"

### Pattern Operations
- "Add 4 holes 2 inches apart"
- "Create a circular pattern of 6 holes"
- "Mirror about the right plane"

### Incremental Commands
- "Make it thicker"
- "Increase the width by 2 inches"
- "Double the height"
- "Add another hole"

### Special Commands (Slash Commands)
- `/help` - Show all available commands
- `/summarize` - Session summary with context
- `/list` - List parts, features, components
- `/undo` - Undo last command
- `/history` - Recent command history
- `/sessions` - List saved sessions
- `/save-session` - Save current session
- `/load-session <name>` - Load a saved session
- `/new-session [name]` - Start fresh session
- `/context` - Show current design context

### Assembly Operations
- "Create a new assembly called Cabinet"
- "Insert the component Side.sldprt"
- "Add a coincident mate between Part1-1 and Part2-1"
- "Make a concentric mate between Shaft-1 and Hole-1"
- "Fix the component Base-1"
- "Add a distance mate of 2 inches"
- "Move the component by 3 inches in X direction"
- "Save the assembly"

### File Operations
- "Save the part"
- "Save as STEP file"
- "Export to STL"
- "Save the part as Cabinet_Side.SLDPRT"

## Development

### Project Structure

```
src/
├── SWAI.App/           # WPF Application
├── SWAI.Core/          # Domain logic
├── SWAI.AI/            # AI/LLM integration
└── SWAI.SolidWorks/    # SolidWorks API

tests/
├── SWAI.Core.Tests/
├── SWAI.AI.Tests/
└── SWAI.SolidWorks.Tests/
```

### Building

```bash
dotnet build SWAI.sln
```

### Testing

```bash
dotnet test
```

### Running Without SolidWorks

The application supports enhanced Mock Mode for development and testing:

```json
{
  "SolidWorks": { "UseMock": true },
  "Mock": {
    "Enabled": true,
    "FailureRate": 0.0,
    "RecordMode": false,
    "MinDelayMs": 100,
    "MaxDelayMs": 500,
    "RealisticResponses": true
  }
}
```

**Mock Features:**
- Realistic feature names and IDs
- Configurable random failures for testing error handling
- Recording and playback of API sessions
- Simulated network delays

## Roadmap

- [x] Phase 1: Foundation & Architecture
- [x] Phase 2: AI Pipeline & Command Parsing (Structured JSON output, enhanced prompts)
- [x] Phase 3: Core CAD Operations (Fillets, chamfers, holes, patterns)
- [x] Phase 4: Conversational Intelligence (Context memory, incremental commands)
- [x] Phase 5: Assembly Operations (Components, mates, transforms)
- [ ] Phase 6: Drawing/Image Recognition

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [SolidWorks API Documentation](https://help.solidworks.com/2025/english/api/sldworksapiprogguide/Welcome.htm)
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- Inspired by [SolidWorks-Copilot](https://github.com/weianweigan/SolidWorks-Copilot)

---

**SWAI** - Making CAD accessible through conversation.
