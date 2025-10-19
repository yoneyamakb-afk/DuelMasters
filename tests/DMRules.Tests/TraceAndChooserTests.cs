using System.Linq;
using DMRules.Engine;
using Xunit;

namespace DMRules.Tests;

file sealed class PickLastChooser : IChooser
{
    public int Choose(int count, System.Func<int, string> describeIndex) => count - 1;
}

public class TraceAndChooserTests
{
    [Fact]
    public void Custom_Chooser_Picks_Last_Candidate_And_Trace_Records_It()
    {
        // Two matching replacements: both same controller/priority -> chooser decides.
        var s = new GameState(Phase.Main, activePlayer: PlayerId.P0, battlefieldCount: 1, chooser: new PickLastChooser())
            .AddReplacement(new PreventNextDestroyForOwnerEffect(PlayerId.P0, priority: 0))   // would prevent
            .AddReplacement(new ReplaceDestroyWithExileEffect(PlayerId.P0, priority: 0, oneShot: true)) // exile
            .Push(new DestroyOneAction(PlayerId.P0));

        var after = Executor.RunAll(s);
        // Chooser selects last -> exile path
        Assert.Equal(0, after.BattlefieldCount);
        Assert.Equal(0, after.GraveyardCount);

        // Trace contains the choice and apply logs
        Assert.Contains(after.Trace, t => t.Kind == "Repl.Choose" && t.Detail.Contains("ReplaceDestroyWithExileEffect"));
        Assert.True(after.Trace.Count > 0);
    }

    [Fact]
    public void Trace_Is_Appended_On_Key_Steps()
    {
        var s = TestUtils.NewState(Phase.Main, PlayerId.P0, bf: 1, gy: 0)
            .Push(new DestroyOneAction(PlayerId.P0));

        var after = Executor.RunAll(s);
        // Expect at least: Stack Push, Pop, Action DestroyOne, Default or Replacement, SBA etc.
        var kinds = after.Trace.Select(t => t.Kind).ToList();
        Assert.Contains("Stack", kinds);
        Assert.Contains("Action", kinds);
        Assert.Contains("SBA", kinds);
    }
}
