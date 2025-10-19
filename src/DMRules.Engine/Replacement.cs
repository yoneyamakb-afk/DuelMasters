using System; using System.Linq; using System.Collections.Immutable;
namespace DMRules.Engine;
public enum GameEventKind { Destroy }
public readonly record struct DestroyEvent(PlayerId AffectedOwner, bool Flag = false);
public readonly record struct ReplacementApplyResult(GameState State, bool PreventDefault, (GameEventKind kind, object evt)? NextEvent);
public interface IReplacementEffect
{
    int Priority { get; }
    PlayerId Controller { get; }
    bool Matches(GameState s, GameEventKind kind, object evt);
    ReplacementApplyResult Apply(GameState s, GameEventKind kind, object evt);
    bool IsOneShot { get; }
}
public static class ReplacementEngine
{
    public static GameState ProcessEvent(GameState s, GameEventKind kind, object evt)
    {
        var currentKind = kind;
        var currentEvt = evt;
        var cur = s;
        bool defaultPrevented = false;

        // --- 修正版: cur に直接適用する ---
        if (cur.ReplacementEffects == null)
            cur = cur.With(replacementEffects: ImmutableList<IReplacementEffect>.Empty);

        if (cur.Chooser == null)
            cur = cur.With(chooser: new DefaultChooser());
        // ----------------------------------

        while (true)
        {
            var cands = cur.ReplacementEffects
                .Where(e => e.Matches(cur, currentKind, currentEvt))
                .ToImmutableList();
            cur = cur.AddTrace("Repl.Candidates", $"{cands.Count} match for {currentKind}");
            if (cands.IsEmpty) break;

            var ordered = cands
                .OrderByDescending(e => (currentEvt is DestroyEvent de) && e.Controller == de.AffectedOwner)
                .ThenByDescending(e => e.Priority)
                .ToList();

            int idx = ordered.Count > 1 ? cur.Chooser.Choose(ordered.Count, i => DescribeEffect(ordered[i])) : 0;
            var chosen = ordered[idx];
            cur = cur.AddTrace("Repl.Choose", DescribeEffect(chosen));

            var res = chosen.Apply(cur, currentKind, currentEvt);
            cur = res.State.AddTrace("Repl.Apply", chosen.GetType().Name);
            if (chosen.IsOneShot)
                cur = cur.With(replacementEffects: cur.ReplacementEffects.Remove(chosen));

            if (res.NextEvent.HasValue)
            {
                (currentKind, currentEvt) = res.NextEvent.Value;
                cur = cur.AddTrace("Repl.NextEvent", $"{currentKind}");
                defaultPrevented = true;
                continue;
            }

            if (res.PreventDefault)
                defaultPrevented = true;

            break;
        }

        if (!defaultPrevented && currentKind == GameEventKind.Destroy)
        {
            cur = cur.With(
                battlefieldCount: cur.BattlefieldCount - 1,
                graveyardCount: cur.GraveyardCount + 1
            ).AddTrace("Default", "Destroy -> bf-1 gy+1");
        }

        return cur;
    }

    public static GameState ApplyDestroy(GameState s, DestroyEvent evt) => ProcessEvent(s, GameEventKind.Destroy, evt);
    private static string DescribeEffect(IReplacementEffect e) => $"[{e.Controller}] prio={e.Priority} {e.GetType().Name}";
}

// Effects
public sealed class PreventNextDestroyForOwnerEffect : IReplacementEffect
{
    public int Priority { get; }
    public PlayerId Controller { get; }
    public bool IsOneShot => true;
    public PreventNextDestroyForOwnerEffect(PlayerId controller, int priority = 0) { Controller = controller; Priority = priority; }
    public bool Matches(GameState s, GameEventKind kind, object evt) => kind == GameEventKind.Destroy && evt is DestroyEvent de && de.AffectedOwner == Controller;
    public ReplacementApplyResult Apply(GameState s, GameEventKind kind, object evt)
    {
        var seq = s.NextSequence;
        var t = new SimpleTriggeredAbility(Controller, priority: 0, sequence: seq, factory: () => new DrawCardAction());
        return new ReplacementApplyResult(s.With(nextSequence: seq + 1).EnqueueTrigger(t), PreventDefault: true, NextEvent: null);
    }
}
public sealed class ReplaceDestroyWithExileEffect : IReplacementEffect
{
    public int Priority { get; }
    public PlayerId Controller { get; }
    public bool IsOneShot { get; }
    public ReplaceDestroyWithExileEffect(PlayerId controller, int priority = 0, bool oneShot = true) { Controller = controller; Priority = priority; IsOneShot = oneShot; }
    public bool Matches(GameState s, GameEventKind kind, object evt) => kind == GameEventKind.Destroy;
    public ReplacementApplyResult Apply(GameState s, GameEventKind kind, object evt)
    {
        var after = s.With(battlefieldCount: s.BattlefieldCount - 1);
        return new ReplacementApplyResult(after, PreventDefault: true, NextEvent: null);
    }
}
public sealed class ChainFlipFlagEffect : IReplacementEffect
{
    public int Priority { get; }
    public PlayerId Controller { get; }
    public bool IsOneShot => true;
    public ChainFlipFlagEffect(PlayerId controller, int priority = 0) { Controller = controller; Priority = priority; }
    public bool Matches(GameState s, GameEventKind kind, object evt) => kind == GameEventKind.Destroy && evt is DestroyEvent de && de.AffectedOwner == Controller && de.Flag == false;
    public ReplacementApplyResult Apply(GameState s, GameEventKind kind, object evt)
    {
        var de = (DestroyEvent)evt;
        var next = (GameEventKind.Destroy, (object)new DestroyEvent(de.AffectedOwner, Flag: true));
        return new ReplacementApplyResult(s, PreventDefault: true, NextEvent: next);
    }
}
