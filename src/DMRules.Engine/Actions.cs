using System;
namespace DMRules.Engine;
public interface IGameAction { GameState Apply(GameState s); }
public sealed class DestroyOneAction : IGameAction
{
    private readonly PlayerId _owner;
    public DestroyOneAction(PlayerId owner) => _owner = owner;
    public GameState Apply(GameState s)
    {
        if (s.BattlefieldCount <= 0) return s;
        var after = ReplacementEngine.ApplyDestroy(s.AddTrace("Action", "DestroyOne"), new DestroyEvent(_owner));
        if (after.GraveyardCount == s.GraveyardCount + 1)
        {
            var seq = after.NextSequence;
            var trig = new SimpleTriggeredAbility(_owner, priority: 0, sequence: seq, factory: () => new DrawCardAction());
            after = after.With(nextSequence: seq + 1).EnqueueTrigger(trig).AddTrace("DeathTrigger", "DrawCard");
        }
        return after;
    }
}
public sealed class DrawCardAction : IGameAction
{
    public GameState Apply(GameState s) => s.AddTrace("Action", "DrawCard").With(isLegal: true);
}
public static class GameStateExtensions
{
    public static GameState Push(this GameState s, IGameAction a) => s.With(stack: s.Stack.Push(a)).AddTrace("Stack", $"Push {a.GetType().Name}");
    public static GameState Pop(this GameState s, out IGameAction top)
    {
        if (s.Stack.IsEmpty) throw new InvalidOperationException("Empty stack");
        top = s.Stack.Peek();
        return s.With(stack: s.Stack.Pop()).AddTrace("Stack", $"Pop {top.GetType().Name}");
    }
    public static GameState EnqueueTrigger(this GameState s, ITriggeredAbility t)
    {
        var after = (t.Owner == s.ActivePlayer) ? s.With(triggersAP: s.TriggersAP.Add(t)) : s.With(triggersNAP: s.TriggersNAP.Add(t));
        return after.AddTrace("TriggerEnqueue", $"{(t.Owner == s.ActivePlayer ? "AP" : "NAP")} prio={t.Priority} seq={t.Sequence}");
    }
    public static GameState AddReplacement(this GameState s, IReplacementEffect eff)
        => s.With(replacementEffects: s.ReplacementEffects.Add(eff)).AddTrace("Repl.Add", eff.GetType().Name);
    public static bool HasStack(this GameState s) => !s.Stack.IsEmpty;
    public static bool HasAnyTriggers(this GameState s) => !s.TriggersAP.IsEmpty || !s.TriggersNAP.IsEmpty;
}
