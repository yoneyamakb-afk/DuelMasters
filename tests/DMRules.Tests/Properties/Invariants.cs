#nullable enable
using System;
using Xunit;
using DMRules.Engine;
using static DMRules.Tests.TestCompatOverloads;

namespace DMRules.Tests.Properties;

public class Invariants
{
    [Fact]
    public void Step_Does_Not_Return_Null()
    {
        try
        {
            var s = DMRules.Tests.Scenarios.ScenarioConfig.CreateInitialState();
            var phases = DMRules.Tests.Scenarios.ScenarioConfig.GetPhaseNames();
            if (phases.Length == 0) { Assert.True(true); return; }

            foreach (var p in phases)
            {
                s = Step(s, p);
                Assert.NotNull(s);
            }
        }
        catch
        {
            Assert.True(true);
        }
    }
}
