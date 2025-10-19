using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class TurnPhaseTests
{
    [Fact]
    public void Phase_Cycles_And_Active_Player_Swaps_At_End()
    {
        var s = TestUtils.NewState(Phase.Setup, PlayerId.P0);
        s = TurnSystem.AdvancePhase(s);
        s = TurnSystem.AdvancePhase(s);
        s = TurnSystem.AdvancePhase(s);
        s = TurnSystem.AdvancePhase(s);
        s = TurnSystem.AdvancePhase(s);
        Assert.Equal(Phase.StartOfTurn, s.Phase);
        Assert.Equal(PlayerId.P1, s.ActivePlayer);
    }
}
