using FluentAssertions;
using SWAI.Core.Models.Preview;
using Xunit;

namespace SWAI.Core.Tests;

public class CommandPreviewTests
{
    [Fact]
    public void CommandPreviewResult_NewPreview_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            OriginalInput = "Create a box"
        };

        // Assert
        preview.Id.Should().NotBeEmpty();
        preview.OriginalInput.Should().Be("Create a box");
        preview.RiskLevel.Should().Be(RiskLevel.Low);
        preview.Actions.Should().BeEmpty();
        preview.Warnings.Should().BeEmpty();
        preview.IsExecuted.Should().BeFalse();
        preview.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void CommandPreviewResult_WithHighConfidenceAndLowRisk_CanAutoExecute()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            OriginalInput = "Create a box",
            Confidence = 0.95,
            RiskLevel = RiskLevel.Low,
            Actions = new List<PreviewAction>
            {
                new PreviewAction { Sequence = 1, Type = ActionType.Create }
            }
        };

        // Assert
        preview.CanAutoExecute.Should().BeTrue();
    }

    [Fact]
    public void CommandPreviewResult_WithWarnings_CannotAutoExecute()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            OriginalInput = "Delete part",
            Confidence = 0.95,
            RiskLevel = RiskLevel.Low,
            Warnings = new List<PreviewWarning>
            {
                new PreviewWarning { Message = "This action cannot be undone" }
            }
        };

        // Assert
        preview.CanAutoExecute.Should().BeFalse();
    }

    [Fact]
    public void CommandPreviewResult_WithHighRisk_CannotAutoExecute()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            OriginalInput = "Delete all features",
            Confidence = 0.95,
            RiskLevel = RiskLevel.High
        };

        // Assert
        preview.CanAutoExecute.Should().BeFalse();
    }

    [Fact]
    public void CommandPreviewResult_Summary_WithNoActions_ShowsCorrectMessage()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult();

        // Assert
        preview.Summary.Should().Be("No actions planned");
    }

    [Fact]
    public void CommandPreviewResult_Summary_WithOneAction_ShowsDescription()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            Actions = new List<PreviewAction>
            {
                new PreviewAction { Description = "Create box 10x20x5" }
            }
        };

        // Assert
        preview.Summary.Should().Be("Create box 10x20x5");
    }

    [Fact]
    public void CommandPreviewResult_Summary_WithMultipleActions_ShowsCount()
    {
        // Arrange & Act
        var preview = new CommandPreviewResult
        {
            Actions = new List<PreviewAction>
            {
                new PreviewAction { Description = "Create sketch" },
                new PreviewAction { Description = "Add rectangle" },
                new PreviewAction { Description = "Extrude" }
            }
        };

        // Assert
        preview.Summary.Should().Contain("3 actions");
        preview.Summary.Should().Contain("Create sketch");
    }

    [Fact]
    public void PreviewAction_Create_ShouldHaveCorrectType()
    {
        // Arrange & Act
        var action = new PreviewAction
        {
            Type = ActionType.Create,
            Description = "Create new part"
        };

        // Assert
        action.Type.Should().Be(ActionType.Create);
    }

    [Fact]
    public void PreviewWarning_ShouldContainMessage()
    {
        // Arrange & Act
        var warning = new PreviewWarning
        {
            Severity = WarningSeverity.Warning,
            Message = "This file already exists",
            Resolution = "Choose a different name"
        };

        // Assert
        warning.Severity.Should().Be(WarningSeverity.Warning);
        warning.Message.Should().Be("This file already exists");
        warning.Resolution.Should().Be("Choose a different name");
    }

    [Fact]
    public void RiskLevel_ShouldHaveCorrectValues()
    {
        // Assert
        RiskLevel.Low.Should().BeDefined();
        RiskLevel.Medium.Should().BeDefined();
        RiskLevel.High.Should().BeDefined();
        RiskLevel.Critical.Should().BeDefined();
    }
}
