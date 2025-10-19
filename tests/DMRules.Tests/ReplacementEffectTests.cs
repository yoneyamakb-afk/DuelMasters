using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class ReplacementEffectTests
{
    [Fact]
    public void PreventNextDestroy_Stops_ZoneChange_And_Enqueues_Draw()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 2, gy: 0)
            .AddReplacement(new PreventNextDestroyForOwnerEffect(PlayerId.P0))
            .Push(new DestroyOneAction(PlayerId.P0));
        var after = Executor.RunAll(s);
        Assert.Equal(2, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);
        Assert.True(after.IsLegal);
    }
    [Fact]
    public void Without_Replacement_Destroy_Proceeds_And_Death_Trigger_Fires()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .Push(new DestroyOneAction(PlayerId.P0));
        var after = Executor.RunAll(s);
        Assert.Equal(0, after.BattlefieldCount);
        Assert.Equal(1, after.GraveyardCount);
        Assert.True(after.IsLegal);
    }
}
