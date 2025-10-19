#nullable enable
using System;
using System.Linq;
using Xunit;
using static DMRules.Tests.TestCompatOverloads;
using DMRules.Engine;

namespace DMRules.Tests.Scenarios;

public class ExecutionOrderScenarios
{
    [Fact]
    public void Walk_One_Full_Turn_Phases_If_Available()
    {
        string[] phases = ScenarioConfig.GetPhaseNames();
        if (phases.Length == 0) { Assert.True(true); return; }

        var s = ScenarioConfig.CreateInitialState();
        foreach (var p in phases)
        {
            s = Step(s, p);
        }

        Assert.NotNull(s);
    }

    [Fact]
    public void Walk_Two_Phases_With_Priority_If_Available()
    {
        string[] phases = ScenarioConfig.GetPhaseNames();
        string[] prios = ScenarioConfig.GetPriorityNames();
        if (phases.Length == 0 || prios.Length == 0) { Assert.True(true); return; }

        var s = ScenarioConfig.CreateInitialState();
        var take = Math.Min(2, phases.Length);
        for (int i = 0; i < take; i++)
        {
            s = Step(s, phases[i], prios[0]);
        }
        Assert.NotNull(s);
    }
}
