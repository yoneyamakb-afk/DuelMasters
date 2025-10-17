
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DuelMasters.Engine;

public sealed class GameState : IGameState
{
    public PlayerId ActivePlayer { get; init; }
    public ImmutableArray<PlayerState> Players { get; init; }
    public StackState Stack { get; init; }
    public IRandomSource Rng { get; init; }

    IReadOnlyList<PlayerState> IGameState.Players => Players;

    private GameState(PlayerId active, ImmutableArray<PlayerState> players, StackState stack, IRandomSource rng)
    {
        ActivePlayer = active;
        Players = players;
        Stack = stack;
        Rng = rng;
    }

    public static GameState Create(Deck a, Deck b, int seed)
    {
        var rng = new SeededRandom(seed);
        var p0 = PlayerState.Create(new PlayerId(0), a, rng);
        var p1 = PlayerState.Create(new PlayerId(1), b, rng);
        return new GameState(new PlayerId(0), ImmutableArray.Create(p0, p1), StackState.Empty, rng);
    }

    public IGameState Apply(ActionIntent intent)
    {
        // Minimal stub: only PassPriority and ResolveTop wired for now
        return intent.Type switch
        {
            ActionType.PassPriority => this, // priority model to be added
            ActionType.ResolveTop => this with { Stack = Stack.Pop() },
            _ => this
        };
    }

    public IEnumerable<ActionIntent> GenerateLegalActions(PlayerId player)
    {
        // Minimal: can always pass; resolve if stack not empty
        yield return new ActionIntent(ActionType.PassPriority);
        if (!Stack.IsEmpty)
            yield return new ActionIntent(ActionType.ResolveTop);
    }

    public GameState With(PlayerId? active = null, ImmutableArray<PlayerState>? players = null, StackState? stack = null, IRandomSource? rng = null)
        => new GameState(active ?? ActivePlayer, players ?? Players, stack ?? Stack, rng ?? Rng);
}

public sealed record PlayerState(PlayerId Id,
    Zone Deck,
    Zone Hand,
    Zone Battle,
    Zone Mana,
    Zone Shield,
    Zone Graveyard)
{
    public static PlayerState Create(PlayerId id, Deck deck, IRandomSource rng)
    {
        // Shuffle deck deterministically
        var shuffled = deck.Cards.ToList();
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        var zDeck = new Zone(ZoneKind.Deck, shuffled.ToImmutableArray());
        return new PlayerState(id,
            zDeck,
            new Zone(ZoneKind.Hand, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Battle, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Mana, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Shield, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Graveyard, ImmutableArray<CardId>.Empty));
    }
}

public readonly record struct Deck(ImmutableArray<CardId> Cards);

public sealed record Zone(ZoneKind Kind, ImmutableArray<CardId> Cards);

public sealed record StackItem(string Description);

public sealed class StackState
{
    private readonly ImmutableArray<StackItem> _items;
    public bool IsEmpty => _items.IsDefaultOrEmpty;
    private StackState(ImmutableArray<StackItem> items) => _items = items;
    public static StackState Empty => new StackState(ImmutableArray<StackItem>.Empty);
    public StackState Push(StackItem item) => new StackState(_items.Add(item));
    public StackState Pop() => new StackState(_items.IsEmpty ? _items : _items.RemoveAt(_items.Length - 1));
    public IReadOnlyList<StackItem> Items => _items;
}
