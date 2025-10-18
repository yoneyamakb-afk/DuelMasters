
using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class SBATests
{
    [Fact(DisplayName = "SBA: zero-or-less power creatures destroyed and no unresolved triggers")]
    public void ZeroPower_Destroyed()
    {
        IGameState s = new MinimalState(
            bz: new[]{ new Creature(5,0), new Creature(6,-1)},
            libraryCountTP: 1
        );
        s = Adapter.Instance.DoSBAUntilStable(s);
        s.BattleZone.Should().BeEmpty("zero-or-less power creatures are destroyed simultaneously");
        Adapter.Instance.PendingTriggersCount(s).Should().Be(0);
    }
}
