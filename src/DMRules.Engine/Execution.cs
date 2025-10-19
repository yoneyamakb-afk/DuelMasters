namespace DMRules.Engine;
public static class Executor
{
    public static GameState RunAll(GameState s)
    {
        var cur = s; bool progressed;
        do
        {
            progressed = false;
            if (cur.HasStack()) { cur = ResolveOne(cur); progressed = true; }
            else if (cur.HasAnyTriggers()) { cur = TriggerProcessor.DrainOneAPNAPPriority(cur); progressed = true; }
            if (progressed) cur = StateBasedActions.Evaluate(cur);
        } while (progressed);
        return cur;
    }
    private static GameState ResolveOne(GameState s) { var afterPop = s.Pop(out var action); return action.Apply(afterPop); }
}
