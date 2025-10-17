
using System.Linq;
using FsCheck.Xunit;
using DuelMasters.Engine;

public class Invariants
{
    [Property(MaxTest = 50)]
    public void Legal_Always_Contains_PassPriority(int seed)
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
