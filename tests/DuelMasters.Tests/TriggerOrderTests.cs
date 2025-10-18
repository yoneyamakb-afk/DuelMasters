using System.Linq;
using DMRules.Engine;
using Xunit;

public class TriggerOrderTests
{
    [Fact]
    public void APNAP_TPThenNPWithinBatch()
    {
        var s = EngineAdapterCore.CreateInitial();
        var a = new StackItem("A", s.TurnPlayer);
        var b = new StackItem("B", s.NonTurnPlayer);
        var c = new StackItem("C", s.TurnPlayer);
        var d = new StackItem("D", s.NonTurnPlayer);

        var ordered = s.ResolveBatchAPNAP(new[] { b, a, d, c }).ToList();
        Assert.Collection(ordered,
            x => Assert.Equal("A", x.Label),
            x => Assert.Equal("C", x.Label),
            x => Assert.Equal("B", x.Label),
            x => Assert.Equal("D", x.Label)
        );
    }
}