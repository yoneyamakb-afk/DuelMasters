using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class TriggerApnap
{
    [Fact]
    public void Simultaneous_Destroy_Triggers_Are_Stacked_APNAP()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(a, b, 101);

        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));

        var gs = s;
        var inst0 = gs.Players[0].BattleIds[0];
        var inst1 = gs.Players[1].BattleIds[0];

        gs = gs with { ContinuousEffects = gs.ContinuousEffects
            .Add(new PowerBuff(new PlayerId(0), inst0, -2000, gs.TurnNumber))
            .Add(new PowerBuff(new PlayerId(1), inst1, -2000, gs.TurnNumber)) };
        s = gs;

        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        gs = s;
        var items = gs.Stack.Items.ToList();

        Assert.True(items.Count >= 2);
        Assert.Contains(items, it => it.Kind == StackItemKind.TriggerDemo && it.Controller.Value == gs.ActivePlayer.Value);
        Assert.Contains(items, it => it.Kind == StackItemKind.TriggerDemo && it.Controller.Value == gs.ActivePlayer.Opponent().Value);
    }
}
