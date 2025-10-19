using DMRules.Engine;
using Xunit;

namespace DMRules.Tests;

public class TurnPhaseTests
{
    [Fact]
    public void Phase_Cycles_And_Active_Player_Swaps_At_End()
    {
        var s = TestUtils.NewState(Phase.Setup, PlayerId.P0);
        s = TurnSystem.AdvancePhase(s); // Setup -> StartOfTurn
        Assert.Equal(Phase.StartOfTurn, s.Phase);
        Assert.Equal(PlayerId.P0, s.ActivePlayer);

        s = TurnSystem.AdvancePhase(s); // -> Main
        Assert.Equal(Phase.Main, s.Phase);
        s = TurnSystem.AdvancePhase(s); // -> Attack
        Assert.Equal(Phase.Attack, s.Phase);
        s = TurnSystem.AdvancePhase(s); // -> End
        Assert.Equal(Phase.End, s.Phase);
        s = TurnSystem.AdvancePhase(s); // -> StartOfTurn & swap AP
        Assert.Equal(Phase.StartOfTurn, s.Phase);
        Assert.Equal(PlayerId.P1, s.ActivePlayer);
    }
}
