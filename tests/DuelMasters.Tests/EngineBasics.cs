
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class EngineBasics
{
    [Fact]
    public void InitialState_GivesPriorityToActivePlayer_InMainPhase()
    {
        var deck = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());
        var s = new Simulator().InitialState(deck, deck, 123);
        Assert.Equal(s.ActivePlayer.Value, s.PriorityPlayer.Value);
        // Main phase by design (Start auto-advanced)
        Assert.Equal(TurnPhase.Main, s.Phase);
    }
}
