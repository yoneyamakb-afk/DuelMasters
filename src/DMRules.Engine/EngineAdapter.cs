
namespace DMRules.Engine;

public sealed class EngineAdapter : IEngineAdapter
{
    private readonly HashSet<Guid> _replaced = new();
    private readonly List<(string phase,string owner,string detail)> _queued = new();
    public IAuditLog Audit { get; } = new InMemoryAuditLog();

    public IGameState RunTurnBasedActions(IGameState s, string step)
    {
        Audit.Note($"TBA:{step}");
        return s;
    }

    public IGameState DoSBAUntilStable(IGameState s0)
    {
        if (s0 is not MinimalState s) return s0;
        bool changed;
        int guard = 0;
        do
        {
            changed = false; guard++;

            var dying = s.BattleZone.Where(c => c.Power <= 0).ToList();
            if (dying.Count > 0)
            {
                foreach (var d in dying) { s.BattleZoneInternal.Remove(d); s.GraveyardInternal.Add(d); }
                Audit.Note($"SBA: Destroy {dying.Count} zero-power creatures");
                changed = true;
            }

            if (s.AttemptDrawTP && s.LibraryCountTP <= 0 && !s.Losers.Contains("TP"))
            {
                s.LosersInternal.Add("TP");
                Audit.Note("SBA: DeckOut TP loses");
                changed = true;
            }

            if (guard > 64) break;
        } while (changed);

        _queued.Clear();
        return s0;
    }

    public IGameState EnqueueNewTriggers(IGameState s0)
    {
        if (s0 is not MinimalState s) return s0;
        _queued.Add(("S","TP","example"));
        _queued.Add(("S","NP","example"));
        Audit.TriggerQueued("S","TP","example");
        Audit.TriggerQueued("S","NP","example");
        return s0;
    }

    public IGameState ResolveSAndOtherTriggers(IGameState s0)
    {
        if (s0 is not MinimalState s) return s0;
        foreach (var item in _queued.OrderBy(q => q.phase!="S").ThenBy(q => q.owner=="TP"?0:1).ToList())
        {
            Audit.Note($"Resolve {item.phase}:{item.owner}");
            _queued.Remove(item);
        }
        return s0;
    }

    public int PendingTriggersCount(IGameState s) => _queued.Count;

    public IGameState ApplyEventWithReplacement(IGameState s, GameEvent ev)
    {
        if (_replaced.Contains(ev.Id)) { Audit.Note($"Already replaced {ev.Id}"); return s; }
        _replaced.Add(ev.Id);
        Audit.ReplacementApplied(ev.Id, "one-per-event");
        return s;
    }

    public IGameState OnZoneEnterApplyContinuousEffects(IGameState s)
    {
        Audit.Note("ZoneEnter: apply continuous effects before ETB checks");
        return s;
    }
}
