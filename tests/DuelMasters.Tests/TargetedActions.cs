
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class TargetedActions
{
    [Fact]
    public void BreakOpponentShield_MovesShieldToOppHand_OnResolution()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(a, b, 1);

        // Ensure it's P0 priority in Main (already true)
        // P0 casts BreakOpponentShield on index 0
        s = sim.Step(s, new ActionIntent(ActionType.BreakOpponentShield, 0));
        // both players pass -> resolve
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        // Check P1 shields decreased by 1 and hand increased by 1
        var p1 = s.Players[1];
        Assert.Equal(4, p1.Shield.Cards.Length);
        Assert.Equal(6, p1.Hand.Cards.Length);
    }

    [Fact]
    public void DestroyOpponentCreature_SendsToGraveyard_OnResolution()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(a, b, 2);

        // P0 summons a dummy creature from hand index 0 to create a target for P1 later
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        // Pass priority to hand over to P1 (two passes -> phase advance, but priority alternates)
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        // Now P1 has priority; summon a dummy so both sides can have targets
        s = sim.Step(s, new ActionIntent(ActionType.SummonDummyFromHand, 0));
        // P1 attempts to destroy P0 creature at index 0
        s = sim.Step(s, new ActionIntent(ActionType.DestroyOpponentCreature, 0));
        // Pass-Pass to resolve
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));
        s = sim.Step(s, new ActionIntent(ActionType.PassPriority));

        var p0 = s.Players[0];
        var p1 = s.Players[1];
        Assert.Equal(0, p0.Battle.Cards.Length); // P0 creature destroyed
        Assert.Equal(1, p1.Battle.Cards.Length); // P1 creature remains
        Assert.Equal(1, p0.Graveyard.Cards.Length); // moved to graveyard
    }
}
