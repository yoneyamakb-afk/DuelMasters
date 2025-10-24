
using DMRules.Effects;
using Xunit;

public class EffectParserTests
{
    [Theory]
    [InlineData("on summon: draw 2", EffectIR.Trigger.EnterBattlezone, 2)]
    [InlineData("on enter: draw 1", EffectIR.Trigger.EnterBattlezone, 1)]
    [InlineData("on attack: draw 3", EffectIR.Trigger.AttackDeclared, 3)]
    public void Parse_DrawX_YieldsExpectedIR(string text, EffectIR.Trigger expectedTrigger, int expectedDraw)
    {
        var eff = EffectParser.Parse(text);
        Assert.NotNull(eff);
        Assert.Single(eff.Clauses);
        var clause = Assert.IsType<EffectIR.OnEvent>(eff.Clauses[0]);
        Assert.Equal(expectedTrigger, clause.Trigger);
        var draw = Assert.IsType<EffectIR.Draw>(clause.Action);
        Assert.Equal(expectedDraw, draw.Cards);
    }

    private sealed class DummyHost : IEffectHost
    {
        public int Drawn { get; private set; }
        public int Mana { get; private set; }
        public void DrawCards(int playerId, int count) => Drawn += count;
        public void AddMana(int playerId, int count) => Mana += count;
    }

    [Fact]
    public void PlannedEffect_ExecutesOnTrigger()
    {
        var eff = EffectParser.Parse("on summon: draw 2");
        var plan = EffectPlanner.Plan(eff);
        var host = new DummyHost();
        plan.OnEnterBattlezone(host, controllerId: 0);
        Assert.Equal(2, host.Drawn);
        Assert.Equal(0, host.Mana);
    }
}
