
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class SBAZeroPower
{
    [Fact]
    public void ZeroOrLessPowerCreature_IsDestroyedBySBA()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(a, b, 7);

        // Summon a dummy for P0
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        var gs = (GameState)s;
        var inst = gs.Players[0].BattleIds[0];

        // Inject a negative buff to drop power to <=0 this turn
        gs = gs with { ContinuousEffects = gs.ContinuousEffects.Add(new PowerBuff(new PlayerId(0), inst, -2000, gs.TurnNumber)) };
        // Any state change triggers SBA loop eventually; do a no-op pass-pass to advance priority and trigger checks
        s = gs;
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        gs = (GameState)s;
        Assert.Empty(gs.Players[0].Battle.Cards);
        Assert.Single(gs.Players[0].Graveyard.Cards);
    }
}
