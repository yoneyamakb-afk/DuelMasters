
using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class ReplacementTests
{
    [Fact(DisplayName = "Replacement: at most once per event id")]
    public void EventGetsSingleReplacement()
    {
        IGameState s = new MinimalState();
        var ev = GameEvent.Create(EventKind.Draw, new Dictionary<string, object?> { ["player"] = "TP" });
        s = Adapter.Instance.ApplyEventWithReplacement(s, ev);
        s = Adapter.Instance.ApplyEventWithReplacement(s, ev);
        Adapter.Instance.Audit.Dump().Count(x => x.StartsWith("REPLACE")).Should().Be(1);
    }
}
