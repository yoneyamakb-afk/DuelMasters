using System.Linq;
using System.Collections.Immutable;
using DuelMasters.Engine.Integration.M11;

namespace DuelMasters.Engine;

public partial record GameState
{
    private GameState ResolveTopInternal()
    {
        EngineHooks.OnEvent?.Invoke(new EngineEvent { Kind = EngineEventKind.CardEnteredZone });

        var pop = Stack.Pop();
        if (pop is null)
            return this;

        var (item, rest) = pop.Value;
        var after = this with { Stack = rest };

        switch (item.Kind)
        {
            case StackItemKind.BuffPower:
                break;
            case StackItemKind.TriggerDemo:
                break;
        }

        after = after.RunStateBasedActions();
        after = after.ProcessTriggers();
        return after;
    }

    public GameState RunStateBasedActions()
    {
        EngineHooks.OnEvent?.Invoke(new EngineEvent { Kind = EngineEventKind.PhaseBegin, PhaseName = Phase.ToString() });
        EngineHooks.ApplyCardStaticFlags(this);

        var after = StateBasedActions.Fix(this);
        if (!after.PendingTriggers.IsDefaultOrEmpty && after.PendingTriggers.Length > 0)
            after = after.ProcessTriggers();

        return after;
    }

    private GameState ProcessTriggers()
    {
        if (PendingTriggers.IsDefaultOrEmpty || PendingTriggers.Length == 0)
            return this;

        var ap = this.ActivePlayer;
        var nap = ap.Opponent();

        var first  = PendingTriggers.Where(t => t.Controller.Value == ap.Value);
        var second = PendingTriggers.Where(t => t.Controller.Value == nap.Value);

        var s = this with { PendingTriggers = ImmutableArray<TriggeredAbility>.Empty };

        foreach (var t in first)
            s = s with { Stack = s.Stack.Push(new StackItem(StackItemKind.TriggerDemo, t.Controller, TargetSpec.None, t.Info)) };
        foreach (var t in second)
            s = s with { Stack = s.Stack.Push(new StackItem(StackItemKind.TriggerDemo, t.Controller, TargetSpec.None, t.Info)) };

        return s with { PriorityPlayer = ap, ConsecutivePasses = 0 };
    }
}
