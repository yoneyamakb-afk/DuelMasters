using System;
using System.Collections.Generic;
using DMRules.Engine;
using Xunit;

public class ReplacementTests
{
    [Fact]
    public void ReplacementAppliedOnlyOncePerEventId()
    {
        var ms = new MinimalState(Phase.Main);
        var id = Guid.NewGuid();

        var s = ms.S;
        Assert.True(s.TryReplacementOnce(id));
        Assert.False(s.TryReplacementOnce(id));
    }
}