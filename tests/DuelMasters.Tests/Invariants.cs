
using System.Linq;
using System.Collections.Immutable;
using FsCheck.Xunit;
using DuelMasters.Engine;

public class Invariants
{
    [Property(MaxTest = 20)]
    public void LegalContainsPassPriority(int seed)
    {
        var deck = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());
        var sim = new Simulator();
        var s = sim.InitialState(deck, deck, seed);

        var legal = sim.Legal(s);
        bool hasPass = false;
        foreach (var a in legal)
            if (a.Type == ActionType.PassPriority) { hasPass = true; break; }
        FsCheck.Prop.Assert(hasPass);
    }
}
