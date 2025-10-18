
using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class InitialPhaseTests
{
    [Fact(DisplayName = "Initial state: Phase Main & priority TP")]
    public void InitialPhase_Main_WithActivePriority()
    {
        IGameState s = new MinimalState();
        s.Phase.Should().Be("Main");
        s.PriorityPlayer.Should().Be("TP");
    }
}
