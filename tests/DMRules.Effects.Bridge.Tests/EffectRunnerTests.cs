
using System;
using DMRules.Effects.Bridge;
using Xunit;

public class EffectRunnerTests
{
    [Fact]
    public void RunOnEnterBattlezone_UsesDelegates()
    {
        int drew = 0, mana = 0;
        EffectRunner.RunOnEnterBattlezone("on summon: draw 2",
            (pid, n) => drew += n,
            (pid, n) => mana += n,
            controllerId: 0);

        Assert.Equal(2, drew);
        Assert.Equal(0, mana);
    }

    [Fact]
    public void RunOnAttackDeclared_UsesDelegates()
    {
        int drew = 0, mana = 0;
        EffectRunner.RunOnAttackDeclared("on attack: draw 1",
            (pid, n) => drew += n,
            (pid, n) => mana += n,
            controllerId: 0);

        Assert.Equal(1, drew);
        Assert.Equal(0, mana);
    }

    [Fact]
    public void UnknownText_IsNoOp()
    {
        int drew = 0, mana = 0;
        EffectRunner.RunOnEnterBattlezone("gain super powers",
            (pid, n) => drew += n,
            (pid, n) => mana += n,
            controllerId: 0);

        Assert.Equal(0, drew);
        Assert.Equal(0, mana);
    }
}
