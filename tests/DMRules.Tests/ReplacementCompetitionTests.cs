using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class ReplacementCompetitionTests
{
    [Fact]
    public void Affected_Owner_Controls_Choice_Over_Priority()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .AddReplacement(new PreventNextDestroyForOwnerEffect(PlayerId.P0, priority: 0))
            .AddReplacement(new ReplaceDestroyWithExileEffect(PlayerId.P1, priority: 99, oneShot: true))
            .Push(new DestroyOneAction(PlayerId.P0));
        var after = Executor.RunAll(s);
        Assert.Equal(1, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);
        Assert.True(after.IsLegal);
    }
    [Fact]
    public void If_Same_Controller_Then_Higher_Priority_Wins()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .AddReplacement(new ReplaceDestroyWithExileEffect(PlayerId.P0, priority: 5))
            .AddReplacement(new PreventNextDestroyForOwnerEffect(PlayerId.P0, priority: 1))
            .Push(new DestroyOneAction(PlayerId.P0));
        var after = Executor.RunAll(s);
        Assert.Equal(0, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);
    }
}
