using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class ReplacementTests
{
    [Fact(DisplayName = "One event receives at most one replacement")]
    public void EventGetsSingleReplacement()
    {
        var s = new MinimalState(0, "Main", "TP");
        var ev = GameEvent.Create(EventKind.Draw, new Dictionary<string, object?> { ["player"] = "TP" });
        s = Adapter.Instance.ApplyEventWithReplacement(s, ev);
        var s2 = Adapter.Instance.ApplyEventWithReplacement(s, ev);
        Adapter.Instance.Audit.Dump().Count(x => x.StartsWith("REPLACE")).Should().BeLessOrEqualTo(1);
    }
}
