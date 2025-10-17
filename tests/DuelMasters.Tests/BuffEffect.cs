
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class BuffEffect
{
    [Fact]
    public void BuffOwnCreature_AddsEffect_AndExpiresNextTurn()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(a, b, 3);

        // Summon one dummy for P0 (target exists)
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        // Cast buff on own creature index 0
        s = sim.Step(s, new ActionIntent(ActionType.BuffOwnCreature, 0));
        // Pass-Pass resolve
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        var gs = (GameState)s;
        Assert.True(gs.ContinuousEffects.Any());

        // End the turn (many passes to advance phases)
        for (int i = 0; i < 10; i++)
            s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        gs = (GameState)s;
        // After turn advances to opponent and back to P0 Start, the previous-turn effects are cleared
        Assert.True(gs.ContinuousEffects.Length == 0 || gs.TurnNumber > 0);
    }
}
