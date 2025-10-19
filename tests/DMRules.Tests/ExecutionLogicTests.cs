using DMRules.Engine; using Xunit;
namespace DMRules.Tests;
public class ExecutionLogicTests
{
    [Fact] public void Basic_RunAll_Works()
    {
        var s0 = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 2, gy: 0).Push(new DestroyOneAction(PlayerId.P0));
        var s1 = Executor.RunAll(s0);
        Assert.True(s1.IsLegal);
    }
}
