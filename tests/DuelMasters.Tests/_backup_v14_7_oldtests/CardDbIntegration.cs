
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class CardDbIntegration
{
    [Fact]
    public void Simulator_Works_Without_DB()
    {
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToImmutableArray());
        var sim = new Simulator(null);
        var s = sim.InitialState(a, b, 5);
        Assert.NotNull(s);
    }
}
