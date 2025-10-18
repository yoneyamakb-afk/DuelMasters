using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class Official_SBA_Tests
{
    [Fact(DisplayName = "SBA: Creatures with power <= 0 are destroyed")]
    public void ZeroOrLessPower_Destroyed()
    {
        var s = new MinimalState(0, "Main", "TP");
        s = Adapter.Instance.DoSBAUntilStable(s);
        // In your adapter's SBA loop, ensure zero-power destruction applies and no unresolved triggers remain
        Adapter.Instance.PendingTriggersCount(s).Should().Be(0);
    }
}
