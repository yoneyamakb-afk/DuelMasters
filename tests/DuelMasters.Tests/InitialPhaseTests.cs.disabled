using DMRules.Engine;
using Xunit;

public class InitialPhaseTests
{
    [Fact]
    public void PhaseIsMainAndPriorityTP()
    {
        var s = EngineAdapterCore.CreateInitial();
        Assert.Equal(Phase.Main, s.Phase);
        Assert.Equal(Priority.TurnPlayer, s.Priority);
    }
}