
using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

public readonly record struct PlayerId(int Value)
{
    public PlayerId Opponent() => new PlayerId(Value ^ 1);
    public override string ToString() => Value.ToString();
}

public enum TurnPhase { Start, Main, Battle, End }

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
    void Reseed(int seed);
}

public interface IGameState
{
    PlayerId ActivePlayer { get; }
    PlayerId PriorityPlayer { get; }
    TurnPhase Phase { get; }
    IReadOnlyList<PlayerState> Players { get; }
    StackState Stack { get; }
    IRandomSource Rng { get; }

    IGameState Apply(ActionIntent intent);
    IEnumerable<ActionIntent> GenerateLegalActions(PlayerId player);
}

public interface ISimulator
{
    IGameState InitialState(Deck a, Deck b, int seed);
    IGameState Step(IGameState s, ActionIntent a);
    IEnumerable<ActionIntent> Legal(IGameState s);
    bool IsTerminal(IGameState s, out GameResult result);
    ulong Hash(IGameState s);
}

public enum GameResult { InProgress, Player0Win, Player1Win, Draw }

public enum ZoneKind { Deck, Hand, Battle, Mana, Shield, Graveyard, Exile }

public enum ActionType
{
    PassPriority,
    ResolveTop,
    // Demo actions to exercise stack/priority path:
    CastDummySpell, // pushes a no-op item onto the stack owned by PriorityPlayer
    PlayMana, // payload: int handIndex
    SummonDummyFromHand, // payload: int handIndex -> moves card to Battle (creature placeholder)
    BreakOpponentShield, // payload: int shieldIndex -> goes onto stack
    DestroyOpponentCreature, // payload: int battleIndex -> goes onto stack
    BuffOwnCreature // payload: int battleIndex -> +1000 this turn
}

public readonly record struct CardId(int Value);

public sealed record CardRef(CardId Id, PlayerId Owner, ZoneKind Zone, int Index);

public sealed record ActionIntent(ActionType Type, object? Payload = null);
