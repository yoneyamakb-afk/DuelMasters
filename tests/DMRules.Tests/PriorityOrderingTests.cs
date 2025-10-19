using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class PriorityOrderingTests
{
    [Fact]
    public void AP_Triggers_Order_By_Priority_Then_Sequence()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0);
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P0, priority: 0, sequence: 2, factory: () => new DrawCardAction()));
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P0, priority: 2, sequence: 3, factory: () => new DrawCardAction()));
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P0, priority: 2, sequence: 1, factory: () => new DrawCardAction()));
        var after = Executor.RunAll(s);
        Assert.True(after.IsLegal);
        Assert.False(after.HasAnyTriggers());
    }
    [Fact]
    public void AP_Before_NAP_Even_If_NAP_Has_Higher_Priority()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0);
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P1, priority: 99, sequence: 1, factory: () => new DrawCardAction()));
        s = s.EnqueueTrigger(new SimpleTriggeredAbility(PlayerId.P0, priority: 0, sequence: 2, factory: () => new DrawCardAction()));
        var after = Executor.RunAll(s);
        Assert.True(after.IsLegal);
        Assert.False(after.HasAnyTriggers());
    }
}
