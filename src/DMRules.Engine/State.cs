
namespace DMRules.Engine;

public enum EventKind { Draw, Destroy, ZoneMove, Battle, BreakShield, CastSpell, ZoneEnter, Custom }

public sealed record GameEvent(Guid Id, EventKind Kind, IReadOnlyDictionary<string, object?> Payload)
{
    public static GameEvent Create(EventKind kind, IDictionary<string, object?> payload)
        => new(Guid.NewGuid(), kind, new Dictionary<string, object?>(payload));
}

public interface IGameState
{
    int StackSize { get; }
    string Phase { get; }
    string PriorityPlayer { get; }
    string TurnPlayer { get; }

    IReadOnlyList<Creature> BattleZone { get; }
    IReadOnlyList<Creature> Graveyard { get; }
    int LibraryCountTP { get; }
    bool AttemptDrawTP { get; }
    IReadOnlyCollection<string> Losers { get; }

    IGameState With(
        int? StackSize = null,
        string? Phase = null,
        string? PriorityPlayer = null,
        string? TurnPlayer = null,
        List<Creature>? BattleZone = null,
        List<Creature>? Graveyard = null,
        int? LibraryCountTP = null,
        bool? AttemptDrawTP = null,
        HashSet<string>? Losers = null);
}

public readonly record struct Creature(int Id, int Power);

public interface IAuditLog
{
    void Note(string message);
    void EventApplied(GameEvent ev, string detail);
    void TriggerQueued(string phase, string owner, string detail);
    void ReplacementApplied(Guid eventId, string ruleName);
    IReadOnlyList<string> Dump();
    void Clear();
}

public interface IEngineAdapter
{
    IGameState RunTurnBasedActions(IGameState s, string step);
    IGameState DoSBAUntilStable(IGameState s);
    IGameState EnqueueNewTriggers(IGameState s);
    IGameState ResolveSAndOtherTriggers(IGameState s);
    IGameState ApplyEventWithReplacement(IGameState s, GameEvent ev);
    IGameState OnZoneEnterApplyContinuousEffects(IGameState s);
    int PendingTriggersCount(IGameState s);
    IAuditLog Audit { get; }
}

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly List<string> _lines = new();
    public void Note(string message) => _lines.Add($"NOTE: {message}");
    public void EventApplied(GameEvent ev, string detail) => _lines.Add($"APPLY: {ev.Kind} {ev.Id} :: {detail}");
    public void TriggerQueued(string phase, string owner, string detail) => _lines.Add($"QUEUE[{phase}:{owner}]: {detail}");
    public void ReplacementApplied(Guid eventId, string ruleName) => _lines.Add($"REPLACE: {eventId} via {ruleName}");
    public IReadOnlyList<string> Dump() => _lines.ToList();
    public void Clear() => _lines.Clear();
}

public sealed class MinimalState : IGameState
{
    public int StackSize { get; init; }
    public string Phase { get; init; }
    public string PriorityPlayer { get; init; }
    public string TurnPlayer { get; init; }
    public List<Creature> BattleZoneInternal { get; init; } = new();
    public List<Creature> GraveyardInternal { get; init; } = new();
    public int LibraryCountTP { get; init; }
    public bool AttemptDrawTP { get; init; }
    public HashSet<string> LosersInternal { get; init; } = new();
    public IReadOnlyList<Creature> BattleZone => BattleZoneInternal;
    public IReadOnlyList<Creature> Graveyard => GraveyardInternal;
    public IReadOnlyCollection<string> Losers => LosersInternal;

    public MinimalState(
        int stackSize = 0,
        string phase = "Main",
        string priorityPlayer = "TP",
        string turnPlayer = "TP",
        IEnumerable<Creature>? bz = null,
        IEnumerable<Creature>? gy = null,
        int libraryCountTP = 0,
        bool attemptDrawTP = false)
    {
        StackSize = stackSize;
        Phase = phase;
        PriorityPlayer = priorityPlayer;
        TurnPlayer = turnPlayer;
        if (bz != null) BattleZoneInternal = bz.ToList();
        if (gy != null) GraveyardInternal = gy.ToList();
        LibraryCountTP = libraryCountTP;
        AttemptDrawTP = attemptDrawTP;
    }

    public IGameState With(
        int? StackSize = null,
        string? Phase = null,
        string? PriorityPlayer = null,
        string? TurnPlayer = null,
        List<Creature>? BattleZone = null,
        List<Creature>? Graveyard = null,
        int? LibraryCountTP = null,
        bool? AttemptDrawTP = null,
        HashSet<string>? Losers = null)
        => new MinimalState(
            stackSize: StackSize ?? this.StackSize,
            phase: Phase ?? this.Phase,
            priorityPlayer: PriorityPlayer ?? this.PriorityPlayer,
            turnPlayer: TurnPlayer ?? this.TurnPlayer,
            bz: BattleZone ?? this.BattleZoneInternal,
            gy: Graveyard ?? this.GraveyardInternal,
            libraryCountTP: LibraryCountTP ?? this.LibraryCountTP,
            attemptDrawTP: AttemptDrawTP ?? this.AttemptDrawTP
        ){ LosersInternal = Losers ?? new HashSet<string>(this.LosersInternal) };
}
