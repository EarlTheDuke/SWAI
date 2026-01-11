using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Assembly;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Executes assembly-specific commands
/// </summary>
public class AssemblyCommandExecutor
{
    private readonly ILogger<AssemblyCommandExecutor> _logger;
    private readonly IAssemblyService _assemblyService;
    private readonly IMateService _mateService;
    private readonly SolidWorksConfiguration _config;

    public AssemblyCommandExecutor(
        IAssemblyService assemblyService,
        IMateService mateService,
        SolidWorksConfiguration config,
        ILogger<AssemblyCommandExecutor> logger)
    {
        _assemblyService = assemblyService;
        _mateService = mateService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Execute an assembly command
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(ISwaiCommand command)
    {
        _logger.LogInformation("Executing assembly command: {Type}", command.CommandType);

        return command switch
        {
            CreateAssemblyCommand cmd => await ExecuteCreateAssemblyAsync(cmd),
            InsertComponentCommand cmd => await ExecuteInsertComponentAsync(cmd),
            AddMateCommand cmd => await ExecuteAddMateAsync(cmd),
            AddCoincidentMateCommand cmd => await ExecuteCoincidentMateAsync(cmd),
            AddConcentricMateCommand cmd => await ExecuteConcentricMateAsync(cmd),
            AddDistanceMateCommand cmd => await ExecuteDistanceMateAsync(cmd),
            MoveComponentCommand cmd => await ExecuteMoveComponentAsync(cmd),
            RotateComponentCommand cmd => await ExecuteRotateComponentAsync(cmd),
            FixComponentCommand cmd => await ExecuteFixComponentAsync(cmd),
            SuppressComponentCommand cmd => await ExecuteSuppressComponentAsync(cmd),
            AssemblyPatternCommand cmd => await ExecuteAssemblyPatternAsync(cmd),
            SaveAssemblyCommand cmd => await ExecuteSaveAssemblyAsync(cmd),
            ShowAssemblyInfoCommand cmd => await ExecuteShowInfoAsync(cmd),
            _ => CommandResult.Failed($"Unknown assembly command: {command.CommandType}")
        };
    }

    private async Task<CommandResult> ExecuteCreateAssemblyAsync(CreateAssemblyCommand cmd)
    {
        var assembly = await _assemblyService.CreateAssemblyAsync(cmd.AssemblyName, cmd.Units);
        return CommandResult.Succeeded($"Created assembly: {assembly.Name}", assembly);
    }

    private async Task<CommandResult> ExecuteInsertComponentAsync(InsertComponentCommand cmd)
    {
        var component = await _assemblyService.InsertComponentAsync(
            cmd.ComponentPath,
            cmd.Position,
            cmd.Fixed,
            cmd.Configuration
        );

        if (component == null)
        {
            return CommandResult.Failed($"Failed to insert component: {cmd.ComponentPath}");
        }

        return CommandResult.Succeeded($"Inserted component: {component.InstanceName}", component);
    }

    private async Task<CommandResult> ExecuteAddMateAsync(AddMateCommand cmd)
    {
        var mate = await _mateService.AddMateAsync(
            cmd.MateType,
            cmd.Entity1,
            cmd.Entity2,
            cmd.Alignment,
            cmd.Distance,
            cmd.Angle
        );

        if (mate == null)
        {
            return CommandResult.Failed($"Failed to create {cmd.MateType} mate");
        }

        return CommandResult.Succeeded($"Created {cmd.MateType} mate: {mate.Name}", mate);
    }

    private async Task<CommandResult> ExecuteCoincidentMateAsync(AddCoincidentMateCommand cmd)
    {
        var mate = await _mateService.AddCoincidentMateAsync(
            cmd.Component1, cmd.Face1,
            cmd.Component2, cmd.Face2,
            cmd.Alignment
        );

        if (mate == null)
        {
            return CommandResult.Failed("Failed to create coincident mate");
        }

        return CommandResult.Succeeded($"Created coincident mate between {cmd.Component1} and {cmd.Component2}", mate);
    }

    private async Task<CommandResult> ExecuteConcentricMateAsync(AddConcentricMateCommand cmd)
    {
        var mate = await _mateService.AddConcentricMateAsync(
            cmd.Component1, cmd.Cylinder1,
            cmd.Component2, cmd.Cylinder2
        );

        if (mate == null)
        {
            return CommandResult.Failed("Failed to create concentric mate");
        }

        return CommandResult.Succeeded($"Created concentric mate between {cmd.Component1} and {cmd.Component2}", mate);
    }

    private async Task<CommandResult> ExecuteDistanceMateAsync(AddDistanceMateCommand cmd)
    {
        var mate = await _mateService.AddDistanceMateAsync(
            cmd.Component1, cmd.Face1,
            cmd.Component2, cmd.Face2,
            cmd.Distance
        );

        if (mate == null)
        {
            return CommandResult.Failed("Failed to create distance mate");
        }

        return CommandResult.Succeeded($"Created distance mate: {cmd.Distance}", mate);
    }

    private async Task<CommandResult> ExecuteMoveComponentAsync(MoveComponentCommand cmd)
    {
        bool success;
        
        if (cmd.NewPosition.HasValue)
        {
            success = await _assemblyService.MoveComponentAsync(cmd.ComponentName, cmd.NewPosition.Value);
        }
        else if (cmd.Offset.HasValue)
        {
            success = await _assemblyService.MoveComponentByAsync(cmd.ComponentName, cmd.Offset.Value);
        }
        else
        {
            return CommandResult.Failed("No position or offset specified");
        }

        return success
            ? CommandResult.Succeeded($"Moved component: {cmd.ComponentName}")
            : CommandResult.Failed($"Failed to move component: {cmd.ComponentName}");
    }

    private async Task<CommandResult> ExecuteRotateComponentAsync(RotateComponentCommand cmd)
    {
        var success = await _assemblyService.RotateComponentAsync(
            cmd.ComponentName,
            cmd.AngleX,
            cmd.AngleY,
            cmd.AngleZ
        );

        return success
            ? CommandResult.Succeeded($"Rotated component: {cmd.ComponentName}")
            : CommandResult.Failed($"Failed to rotate component: {cmd.ComponentName}");
    }

    private async Task<CommandResult> ExecuteFixComponentAsync(FixComponentCommand cmd)
    {
        var success = await _assemblyService.FixComponentAsync(cmd.ComponentName, cmd.Fix);

        return success
            ? CommandResult.Succeeded(cmd.Fix 
                ? $"Fixed component: {cmd.ComponentName}"
                : $"Floated component: {cmd.ComponentName}")
            : CommandResult.Failed($"Failed to {(cmd.Fix ? "fix" : "float")} component");
    }

    private async Task<CommandResult> ExecuteSuppressComponentAsync(SuppressComponentCommand cmd)
    {
        var success = await _assemblyService.SuppressComponentAsync(cmd.ComponentName, cmd.Suppress);

        return success
            ? CommandResult.Succeeded(cmd.Suppress
                ? $"Suppressed component: {cmd.ComponentName}"
                : $"Unsuppressed component: {cmd.ComponentName}")
            : CommandResult.Failed($"Failed to {(cmd.Suppress ? "suppress" : "unsuppress")} component");
    }

    private async Task<CommandResult> ExecuteAssemblyPatternAsync(AssemblyPatternCommand cmd)
    {
        if (cmd.PatternType == PatternType.Linear && cmd.Spacing != null)
        {
            var components = await _assemblyService.InsertComponentsAsync(
                cmd.ComponentName, // Assuming this is the part path
                cmd.Count,
                cmd.Spacing.Value
            );

            return components.Count > 0
                ? CommandResult.Succeeded($"Created pattern with {components.Count} components", components)
                : CommandResult.Failed("Failed to create component pattern");
        }

        return CommandResult.Failed("Pattern type not yet implemented");
    }

    private async Task<CommandResult> ExecuteSaveAssemblyAsync(SaveAssemblyCommand cmd)
    {
        var success = await _assemblyService.SaveAssemblyAsync(cmd.FilePath, cmd.SaveComponents);

        return success
            ? CommandResult.Succeeded("Assembly saved successfully")
            : CommandResult.Failed("Failed to save assembly");
    }

    private async Task<CommandResult> ExecuteShowInfoAsync(ShowAssemblyInfoCommand cmd)
    {
        var assembly = _assemblyService.ActiveAssembly;
        if (assembly == null)
        {
            return CommandResult.Succeeded("No active assembly. Create or open an assembly first.");
        }

        var components = await _assemblyService.GetComponentsAsync();
        var mates = await _mateService.GetMatesAsync();

        var info = cmd.InfoType switch
        {
            AssemblyInfoType.Components => GetComponentsInfo(components),
            AssemblyInfoType.Mates => GetMatesInfo(mates),
            AssemblyInfoType.BillOfMaterials => GetBOMInfo(components),
            _ => GetAllInfo(assembly, components, mates)
        };

        return CommandResult.Succeeded(info, assembly);
    }

    private string GetComponentsInfo(List<Core.Models.Documents.AssemblyComponent> components)
    {
        if (components.Count == 0)
            return "No components in assembly.";

        var lines = components.Select(c => 
            $"  • {c.InstanceName}" +
            (c.IsFixed ? " [Fixed]" : "") +
            (c.IsSuppressed ? " [Suppressed]" : "")
        );
        
        return $"Components ({components.Count}):\n{string.Join("\n", lines)}";
    }

    private string GetMatesInfo(List<AssemblyMate> mates)
    {
        if (mates.Count == 0)
            return "No mates in assembly.";

        var lines = mates.Select(m =>
            $"  • {m.Name}: {m.Type}" +
            (m.Distance != null ? $" ({m.Distance})" : "") +
            (m.Angle != null ? $" ({m.Angle}°)" : "") +
            (m.IsSuppressed ? " [Suppressed]" : "")
        );

        return $"Mates ({mates.Count}):\n{string.Join("\n", lines)}";
    }

    private string GetBOMInfo(List<Core.Models.Documents.AssemblyComponent> components)
    {
        var bom = components
            .Where(c => !c.IsSuppressed)
            .GroupBy(c => c.Name)
            .Select(g => $"  • {g.Key}: {g.Count()} pcs");

        return $"Bill of Materials:\n{string.Join("\n", bom)}";
    }

    private string GetAllInfo(
        Core.Models.Documents.AssemblyDocument assembly,
        List<Core.Models.Documents.AssemblyComponent> components,
        List<AssemblyMate> mates)
    {
        return $"Assembly: {assembly.Name}\n" +
               $"Components: {components.Count}\n" +
               $"Mates: {mates.Count}\n" +
               $"Modified: {(assembly.IsDirty ? "Yes" : "No")}";
    }
}
