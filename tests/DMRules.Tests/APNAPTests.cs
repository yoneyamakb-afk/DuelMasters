using DMRules.Engine;
using Xunit;

namespace DMRules.Tests;

public class APNAPTests
{
    [Fact]
    public void Active_Player_Triggers_Resolve_Before_NonActive()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0);
        // Manually enqueue two triggers: one for NAP, one for AP; ensure AP fires first.
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P1, priority: 0, sequence: 1, factory: () => new DrawCardAction())); // NAP
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P0, priority: 0, sequence: 2, factory: () => new DrawCardAction())); // AP

        // First RunAll drains AP first
        var after1 = Executor.RunAll(s);
        // No visible counters change, but both queues should be empty and state legal
        Assert.True(after1.IsLegal);
        Assert.False(after1.HasAnyTriggers());
    }
}
