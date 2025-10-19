using System; using System.Linq; using System.Collections.Immutable;
namespace DMRules.Engine;
public interface ITriggeredAbility { PlayerId Owner { get; } int Priority { get; } long Sequence { get; } IGameAction Resolve(); }
public sealed class SimpleTriggeredAbility : ITriggeredAbility
{
    public PlayerId Owner { get; } public int Priority { get; } public long Sequence { get; }
    private readonly Func<IGameAction> _factory;
    public SimpleTriggeredAbility(PlayerId owner, int priority, long sequence, Func<IGameAction> factory)
    { Owner = owner; Priority = priority; Sequence = sequence; _factory = factory; }
    public IGameAction Resolve() => _factory();
}
public static class TriggerProcessor
{
    public static GameState DrainOneAPNAPPriority(GameState s)
    {
        var fromAP = !s.TriggersAP.IsEmpty; var fromNAP = !s.TriggersNAP.IsEmpty;
        if (!fromAP && !fromNAP) return s;
        var useAP = fromAP || !fromNAP;
        var list = useAP ? s.TriggersAP : s.TriggersNAP;
        var chosen = list.OrderByDescending(t => t.Priority).ThenBy(t => t.Sequence).First();
        var idx = list.IndexOf(chosen); var newList = list.RemoveAt(idx);
        var after = s.With(stack: s.Stack.Push(chosen.Resolve()),
                      triggersAP: useAP ? newList : s.TriggersAP,
                      triggersNAP: useAP ? s.TriggersNAP : newList);
        return after.AddTrace("TriggerDrain", useAP ? $"AP priority={chosen.Priority} seq={chosen.Sequence}" : $"NAP priority={chosen.Priority} seq={chosen.Sequence}");
    }
}
