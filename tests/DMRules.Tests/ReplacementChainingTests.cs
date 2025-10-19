using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class ReplacementChainingTests
{
    [Fact]
    public void Destroy_Then_FlipFlag_Then_Prevent()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .AddReplacement(new ChainFlipFlagEffect(PlayerId.P0, priority: 0))   // produces Destroy(flag=true)
            .AddReplacement(new PreventNextDestroyForOwnerEffect(PlayerId.P0, priority: 0)) // consumes second-stage destroy
            .Push(new DestroyOneAction(PlayerId.P0));

        var after = Executor.RunAll(s);
        Assert.Equal(1, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);
        Assert.True(after.IsLegal);
    }

    [Fact]
    public void Destroy_Then_FlipFlag_Then_Exile()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .AddReplacement(new ChainFlipFlagEffect(PlayerId.P0, priority: 0))     // produces Destroy(flag=true)
            .AddReplacement(new ReplaceDestroyWithExileEffect(PlayerId.P0, priority: 5, oneShot: true)) // chosen after chaining
            .Push(new DestroyOneAction(PlayerId.P0));

        var after = Executor.RunAll(s);
        Assert.Equal(0, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);
        Assert.True(after.IsLegal);
    }
}
