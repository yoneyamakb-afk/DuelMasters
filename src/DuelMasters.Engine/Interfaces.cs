
using System;
using System.Collections.Generic;

namespace DuelMasters.Engine;

public readonly record struct PlayerId(int Value)
{
    public override string ToString() => Value.ToString();
}

public interface IRandomSource
{
    int Next(int minInclusive, int maxExclusive);
    void Reseed(int seed);
}

public interface IGameState
{
    PlayerId ActivePlayer { get; }
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

public enum ActionType { PassPriority, PlayMana, Summon, CastSpell, Attack, Block, ActivateAbility, ResolveTop }

public readonly record struct CardId(int Value);

public sealed record CardRef(CardId Id, PlayerId Owner, ZoneKind Zone, int Index);

public sealed record ActionIntent(ActionType Type, object? Payload = null);
