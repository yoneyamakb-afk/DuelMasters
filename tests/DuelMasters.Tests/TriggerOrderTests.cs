
using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class TriggerOrderTests
{
    [Fact(DisplayName = "S-Triggers first; APNAP within batch (TP then NP)")]
    public void Order_Striggers_TP_First()
    {
        IGameState s = new MinimalState();
        s = Adapter.Instance.EnqueueNewTriggers(s);
        s = Adapter.Instance.ResolveSAndOtherTriggers(s);
        var log = Adapter.Instance.Audit.Dump();
        var tp = log.ToList().FindIndex(x => x.Contains("QUEUE[S:TP]"));
        var np = log.ToList().FindIndex(x => x.Contains("QUEUE[S:NP]"));
        tp.Should().BeGreaterOrEqualTo(0);
        np.Should().BeGreaterOrEqualTo(0);
        tp.Should().BeLessThan(np);
    }
}
