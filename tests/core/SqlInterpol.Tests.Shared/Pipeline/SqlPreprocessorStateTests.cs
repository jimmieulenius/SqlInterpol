using SqlInterpol.Pipeline;
using Xunit;

namespace SqlInterpol.Tests.Pipeline;

public class SqlPreprocessorStateTests
{
    [Fact]
    public void PushState_IncrementsParenDepth_AndSavesFrames()
    {
        // Arrange
        var state = new SqlPreprocessorState(null!, 10);
        state.CurrentKeyword = "SELECT";
        state.ActiveDmlKeyword = "INSERT";
        state.FromCount = 2;

        // Act
        state.PushState();

        // Assert
        Assert.Equal(1, state.ParenDepth);
        Assert.Single(state.Frames);
        Assert.Equal("SELECT", state.Frames[0].Keyword);
        Assert.Equal("INSERT", state.Frames[0].ActiveDmlKeyword);
        Assert.Equal(2, state.Frames[0].FromCount);
    }

    [Fact]
    public void PopState_DecrementsParenDepth_AndRestoresFrame()
    {
        // Arrange
        var state = new SqlPreprocessorState(null!, 10);
        state.CurrentKeyword = "OUTER";
        
        state.PushState(); // Saves "OUTER"
        
        state.CurrentKeyword = "INNER"; // We are inside parentheses now
        
        // Act
        state.PopState();  // Exits parentheses, should restore "OUTER"

        // Assert
        Assert.Equal(0, state.ParenDepth);
        Assert.Empty(state.Frames);
        Assert.Equal("OUTER", state.CurrentKeyword);
    }

    [Fact]
    public void PopState_DoesNothing_WhenParenDepthIsZero()
    {
        // Arrange
        var state = new SqlPreprocessorState(null!, 10);
        state.CurrentKeyword = "SELECT";

        // Act
        state.PopState();

        // Assert
        Assert.Equal(0, state.ParenDepth);
        Assert.Equal("SELECT", state.CurrentKeyword);
    }
}