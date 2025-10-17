
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class SetupAndDraw
{
    [Fact]
    public void Setup_Has5ShieldsAnd5HandEach()
    {
        var deck = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var s = new Simulator().InitialState(deck, deck, 1);
        foreach (var p in s.Players)
        {
            Assert.Equal(5, p.Shield.Cards.Length);
            Assert.Equal(5, p.Hand.Cards.Length);
        }
    }

    [Fact]
    public void FirstPlayerFirstTurn_SkipsDraw()
    {
        var deck = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var s = new Simulator().InitialState(deck, deck, 1);
        // After initial AdvancePhaseIfNeeded(), we are in Main and first player has not drawn
        Assert.Equal(5, s.Players[s.ActivePlayer.Value].Hand.Cards.Length);
    }
}
