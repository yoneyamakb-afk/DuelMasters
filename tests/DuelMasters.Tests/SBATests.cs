using DMRules.Engine;
using Xunit;

public class SBATests
{
    [Fact]
    public void PowerLessOrEqualZeroGetsDestroyed_Stable()
    {
        var ms = new MinimalState(2, Phase.Main, Priority.TurnPlayer);
        // 2体を0パワーで置いて安定破壊を確認
        ms.S.Battlefield.Add(new Creature("X", 0));
        ms.S.Battlefield.Add(new Creature("Y", -1000));

        Adapter.Instance.DoSBAUntilStable(ms);
        Assert.True(ms.S.Battlefield[0].Destroyed);
        Assert.True(ms.S.Battlefield[1].Destroyed);
    }
}