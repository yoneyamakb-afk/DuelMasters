
using System.Linq;
using Xunit;
using DuelMasters.Engine;

public class EngineBasics
{
    [Fact]
    public void InitialState_IsDeterministic_WithSeed()
    {
        var deck = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());
        var s1 = new Simulator().InitialState(deck, deck, 123);
        var s2 = new Simulator().InitialState(deck, deck, 123);
        Assert.Equal(new Simulator().Hash(s1), new Simulator().Hash(s2));
    }
}
