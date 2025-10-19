using System;

namespace DMRules.Engine;

public interface IGameAction
{
    GameState Apply(GameState s);
}

public sealed class DestroyOneAction : IGameAction
{
    private readonly PlayerId _owner;
    public DestroyOneAction(PlayerId owner) => _owner = owner;

    public GameState Apply(GameState s)
    {
        if (s.BattlefieldCount <= 0) return s;
        var after = s.With(
            battlefieldCount: s.BattlefieldCount - 1,
            graveyardCount: s.GraveyardCount + 1
        );
        // enqueue a death trigger owned by _owner
        return after.EnqueueTrigger(new SimpleTriggeredAbility(_owner, () => new DrawCardAction()));
    }
}

public sealed class DrawCardAction : IGameAction
{
    public GameState Apply(GameState s)
    {
        // Dummy: mark legal; no zone count changes
        return s.With(isLegal: true);
    }
}

public static class GameStateExtensions
{
    public static GameState Push(this GameState s, IGameAction a)
        => s.With(stack: s.Stack.Push(a));

    public static GameState Pop(this GameState s, out IGameAction top)
    {
        if (s.Stack.IsEmpty) throw new InvalidOperationException("Empty stack");
        top = s.Stack.Peek();
        return s.With(stack: s.Stack.Pop());
    }

    public static GameState EnqueueTrigger(this GameState s, ITriggeredAbility t)
        => t.Owner == s.ActivePlayer
            ? s.With(triggersAP: s.TriggersAP.Enqueue(t))
            : s.With(triggersNAP: s.TriggersNAP.Enqueue(t));

    public static bool HasStack(this GameState s) => !s.Stack.IsEmpty;
    public static bool HasAnyTriggers(this GameState s) => !s.TriggersAP.IsEmpty || !s.TriggersNAP.IsEmpty;
}
