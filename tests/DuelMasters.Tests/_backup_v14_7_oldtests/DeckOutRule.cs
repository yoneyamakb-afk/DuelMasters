
using System.Linq;
using System.Collections.Immutable;
using Xunit;
using DuelMasters.Engine;

public class DeckOutRule
{
    [Fact]
    public void PlayerWhoCannotDraw_Loses()
    {
        // Make tiny decks so draws exhaust quickly
        var a = new Deck(Enumerable.Range(0, 10).Select(i => new CardId(i)).ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 10).Select(i => new CardId(i)).ToImmutableArray());

        var sim = new Simulator();
        var s = sim.InitialState(a, b, 1);

        // Advance turns until someone decks out. Start phase auto-draw (except first turn P0).
        int guard = 200;
        while (!sim.IsTerminal(s, out var result) && guard-- > 0)
        {
            var legal = sim.Legal(s).ToList();
            // Pass priority until phases advance; no stack interactions needed.
            var pass = legal.First(x => x.Type == ActionType.PassPriority);
            s = sim.Step(s, pass);
        }
        Assert.True(sim.IsTerminal(s, out var final));
        Assert.NotEqual(GameResult.InProgress, final);
    }
}
