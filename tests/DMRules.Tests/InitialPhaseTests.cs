using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class InitialPhaseTests
{
    [Fact(DisplayName = "Initial state: Phase should be Main and priority to Active Player")]
    public void InitialPhase_Main_WithActivePriority()
    {
        var s = new MinimalState(0, "Main", "TP");
        s.Phase.Should().Be("Main");
        s.PriorityPlayer.Should().Be("TP");
    }
}
