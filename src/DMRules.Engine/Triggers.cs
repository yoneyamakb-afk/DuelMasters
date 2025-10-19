using System;
using System.Collections.Immutable;

namespace DMRules.Engine;

public interface ITriggeredAbility
{
    PlayerId Owner { get; }
    IGameAction Resolve();
}

public sealed class SimpleTriggeredAbility : ITriggeredAbility
{
    public PlayerId Owner { get; }
    private readonly Func<IGameAction> _factory;
    public SimpleTriggeredAbility(PlayerId owner, Func<IGameAction> factory)
    {
        Owner = owner;
        _factory = factory;
    }
    public IGameAction Resolve() => _factory();
}

public static class TriggerProcessor
{
    // APNAP: drain from Active player's queue first if both non-empty; otherwise drain whichever has items.
    public static GameState DrainOneAPNAP(GameState s)
    {
        if (!s.TriggersAP.IsEmpty)
        {
            s.TriggersAP.Dequeue(out var trig, out var rest);
            return s.With(triggersAP: rest).Push(trig.Resolve());
        }
        if (!s.TriggersNAP.IsEmpty)
        {
            s.TriggersNAP.Dequeue(out var trig, out var rest);
            return s.With(triggersNAP: rest).Push(trig.Resolve());
        }
        return s;
    }

    public static void Dequeue<T>(this ImmutableQueue<T> q, out T item, out ImmutableQueue<T> rest)
    {
        q = q.Dequeue(out item);
        rest = q;
    }
}
