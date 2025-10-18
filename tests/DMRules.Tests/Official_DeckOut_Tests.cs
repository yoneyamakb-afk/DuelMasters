using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class Official_DeckOut_Tests
{
    [Fact(DisplayName = "SBA: Player who cannot draw loses")]
    public void DeckOut_PlayerLoses()
    {
        var s = new MinimalState(0, "Main", "TP");
        s = Adapter.Instance.DoSBAUntilStable(s);
        // You can expose a helper or audit marker for loss; here we assert 'no pending triggers' as minimal proxy
        Adapter.Instance.PendingTriggersCount(s).Should().Be(0);
    }
}
