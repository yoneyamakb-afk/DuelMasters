
using DMRules.Effects;
using Xunit;

public class TriggerVocabularyTests
{
    [Theory]
    [InlineData("破壊された時", EffectIR.Trigger.Destroyed)]
    [InlineData("このクリーチャーが破壊されるとき", EffectIR.Trigger.Destroyed)]
    [InlineData("バトルに勝った時", EffectIR.Trigger.WinsBattle)]
    [InlineData("シールドをブレイクした時", EffectIR.Trigger.ShieldBreak)]
    [InlineData("on destroyed", EffectIR.Trigger.Destroyed)]
    [InlineData("on battle win", EffectIR.Trigger.WinsBattle)]
    [InlineData("on shield break", EffectIR.Trigger.ShieldBreak)]
    public void JP_And_English_Bare_Triggers_Parse_To_NoOp(string text, EffectIR.Trigger expected)
    {
        var eff = EffectParser.Parse(text);
        Assert.NotNull(eff);
        Assert.Single(eff.Clauses);
        var clause = Assert.IsType<EffectIR.OnEvent>(eff.Clauses[0]);
        Assert.Equal(expected, clause.Trigger);
        Assert.IsType<EffectIR.NoOp>(clause.Action);
    }
}
