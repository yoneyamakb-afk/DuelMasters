using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class TriggerOrderTests
{
    [Fact(DisplayName = "S-Triggers first, then other triggers: TP before non-TP")]
    public void Order_Striggers_TP_First()
    {
        var s = new MinimalState(0, "Main", "TP");
        s = Adapter.Instance.EnqueueNewTriggers(s);
        s = Adapter.Instance.ResolveSAndOtherTriggers(s);
        var log = Adapter.Instance.Audit.Dump();
        var tpIndex = log.FindIndex(x => x.StartsWith("QUEUE[S:TP]"));
        var npIndex = log.FindIndex(x => x.StartsWith("QUEUE[S:NP]"));
        tpIndex.Should().BeGreaterThanOrEqualTo(0);
        npIndex.Should().BeGreaterThanOrEqualTo(0);
        tpIndex.Should().BeLessThan(npIndex);
    }
}
