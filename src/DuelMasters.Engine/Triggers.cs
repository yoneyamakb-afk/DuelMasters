
using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

public enum TriggerTiming
{
    // Examples: EnterBattlezone, LeavesBattlezone, OnAttackDeclare, OnBreakShield, etc.
    Custom
}

public sealed record Trigger(TriggerTiming Timing, string Description, PlayerId Controller);

public static class TriggerEngine
{
    /// <summary>
    /// Collect triggers that should fire between 'before' and 'after' states.
    /// This is a stub placeholder; real detection will diff zones/flags and create Trigger items.
    /// </summary>
    public static IEnumerable<Trigger> Collect(GameState before, GameState after)
    {
        yield break;
    }

    /// <summary>
    /// APNAP order: Active player first, then non-active.
    /// </summary>
    public static GameState PushAll(GameState s, IEnumerable<Trigger> triggers)
    {
        // For now, triggers are not materialized into stack items.
        return s;
    }
}
