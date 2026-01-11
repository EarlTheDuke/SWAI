using FluentAssertions;
using SWAI.Core.Commands;
using SWAI.Core.Models.Features;
using SWAI.Core.Models.Units;
using SWAI.Core.Services;
using Xunit;

namespace SWAI.Core.Tests;

public class ConversationContextTests
{
    [Fact]
    public void PushDimension_ShouldAddToHistory()
    {
        // Arrange
        var context = new ConversationContext();
        var dim = Dimension.Inches(10);

        // Act
        context.PushDimension(dim);

        // Assert
        context.RecentDimensions.Should().HaveCount(1);
        context.RecentDimensions.Peek().Should().Be(dim);
    }

    [Fact]
    public void PushDimension_ShouldLimitTo10()
    {
        // Arrange
        var context = new ConversationContext();

        // Act
        for (int i = 0; i < 15; i++)
        {
            context.PushDimension(Dimension.Inches(i));
        }

        // Assert
        context.RecentDimensions.Should().HaveCount(10);
    }

    [Fact]
    public void SetReference_ShouldStoreReference()
    {
        // Arrange
        var context = new ConversationContext();
        var feature = new ExtrusionFeature("TestFeature", null!, Dimension.Inches(5));

        // Act
        context.SetReference("first hole", feature);

        // Assert
        var retrieved = context.GetReference<ExtrusionFeature>("first hole");
        retrieved.Should().Be(feature);
    }

    [Fact]
    public void SetReference_ShouldBeCaseInsensitive()
    {
        // Arrange
        var context = new ConversationContext();
        var feature = new ExtrusionFeature("TestFeature", null!, Dimension.Inches(5));

        // Act
        context.SetReference("First Hole", feature);

        // Assert
        var retrieved = context.GetReference<ExtrusionFeature>("first hole");
        retrieved.Should().Be(feature);
    }

    [Fact]
    public void OnCommandExecuted_ShouldUpdateContext()
    {
        // Arrange
        var context = new ConversationContext();
        var command = new CreateBoxCommand("Box", Dimension.Inches(10), Dimension.Inches(20), Dimension.Inches(5));

        // Act
        context.OnCommandExecuted(command, null);

        // Assert
        context.LastCommand.Should().Be(command);
        context.RecentDimensions.Should().Contain(d => d.Value == 10);
        context.RecentDimensions.Should().Contain(d => d.Value == 20);
        context.RecentDimensions.Should().Contain(d => d.Value == 5);
    }

    [Fact]
    public void Clear_ShouldResetEverything()
    {
        // Arrange
        var context = new ConversationContext();
        context.PushDimension(Dimension.Inches(10));
        context.SetReference("test", new object());
        context.LastCommand = new SavePartCommand();

        // Act
        context.Clear();

        // Assert
        context.RecentDimensions.Should().BeEmpty();
        context.NamedReferences.Should().BeEmpty();
        context.LastCommand.Should().BeNull();
        context.CurrentPart.Should().BeNull();
    }
}
