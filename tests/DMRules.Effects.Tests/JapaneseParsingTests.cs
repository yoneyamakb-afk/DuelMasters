
using DMRules.Effects;
using Xunit;

public class JapaneseParsingTests
{
    [Theory]
    [InlineData("召喚時：カードを2枚引く", EffectIR.Trigger.EnterBattlezone, 2)]
    [InlineData("バトルゾーンに出た時、カードを１枚引く。", EffectIR.Trigger.EnterBattlezone, 1)]
    [InlineData("攻撃する時：カードを二枚引く", EffectIR.Trigger.AttackDeclared, 2)]
    [InlineData("攻撃時、1枚ドロー。", EffectIR.Trigger.AttackDeclared, 1)]
    [InlineData("攻撃宣言時：２ドロー", EffectIR.Trigger.AttackDeclared, 2)]
    public void JP_Text_Is_Normalized_And_Parsed(string jp, EffectIR.Trigger expectedTrigger, int expectedDraw)
    {
        var eff = EffectParser.Parse(jp);
        Assert.NotNull(eff);
        Assert.Single(eff.Clauses);
        var clause = Assert.IsType<EffectIR.OnEvent>(eff.Clauses[0]);
        Assert.Equal(expectedTrigger, clause.Trigger);
        var draw = Assert.IsType<EffectIR.Draw>(clause.Action);
        Assert.Equal(expectedDraw, draw.Cards);
    }
}
