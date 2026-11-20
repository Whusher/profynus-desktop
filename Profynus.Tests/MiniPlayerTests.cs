using Profynus.Desktop.Services;
using Xunit;
namespace Profynus.Tests;
public sealed class MiniPlayerTests
{
    [Fact]
    public void ControlsHideThreeSecondsAfterPointerLeaves()
    {
        var state = new MiniPlayerVisibility();
        state.Enter(TimeSpan.Zero);
        Assert.True(state.ControlsVisible(TimeSpan.FromSeconds(10), false));
        state.Leave(TimeSpan.FromSeconds(10));
        Assert.True(state.ControlsVisible(TimeSpan.FromSeconds(12.9), false));
        Assert.False(state.ControlsVisible(TimeSpan.FromSeconds(13), false));
        state.Enter(TimeSpan.FromSeconds(14));
        Assert.True(state.ControlsVisible(TimeSpan.FromSeconds(14), false));
    }
    [Fact]
    public void KeyboardUsersCanKeepControlsVisible()
    {
        var state = new MiniPlayerVisibility();
        Assert.True(state.ControlsVisible(TimeSpan.FromSeconds(30), true));
        Assert.False(state.ControlsVisible(TimeSpan.FromSeconds(30), false));
    }
}
