using DMRules.Engine;
using Xunit;

namespace DMRules.Tests;

public class ContinuousEffectOrderTests
{
    [Fact]
    public void Uses_Phase_Type_Not_String()
    {
        var s0 = TestUtils.NewState(Phase.Main);
        var s1 = Executor.RunAll(s0);
        Assert.Equal(Phase.Main, s1.Phase);
    }
}
