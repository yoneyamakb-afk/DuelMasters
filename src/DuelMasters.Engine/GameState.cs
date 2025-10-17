
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace DuelMasters.Engine;

public sealed record GameState : IGameState
{
    public PlayerId ActivePlayer { get; init; }
    public PlayerId PriorityPlayer { get; init; }
    public TurnPhase Phase { get; init; }
    public ImmutableArray<PlayerState> Players { get; init; }
    public StackState Stack { get; init; }
    public IRandomSource Rng { get; init; }
    public int ConsecutivePasses { get; init; }
    public int TurnNumber { get; init; } // Increments when cycle returns to P0
    public GameResult? GameOverResult { get; init; }
    public ImmutableArray<PowerBuff> ContinuousEffects { get; init; } = ImmutableArray<PowerBuff>.Empty;
    public ICardDatabase? CardDb { get; init; }
    public int NextInstanceId { get; init; }

    IReadOnlyList<PlayerState> IGameState.Players => Players;

    private GameState(PlayerId active, PlayerId priority, TurnPhase phase,
        ImmutableArray<PlayerState> players, StackState stack, IRandomSource rng, int passes, int turnNumber, int nextInstanceId,
            GameResult? gameOver = null, ImmutableArray<PowerBuff>? effects = null, ICardDatabase? cardDb = null)
    {
        ActivePlayer = active;
        PriorityPlayer = priority;
        Phase = phase;
        Players = players;
        Stack = stack;
        Rng = rng;
        ConsecutivePasses = passes;
        TurnNumber = turnNumber;
        NextInstanceId = nextInstanceId;
        GameOverResult = gameOver;
        ContinuousEffects = effects ?? ContinuousEffects;
        
        CardDb = cardDb ?? CardDb;
CardDb = cardDb ?? CardDb;
    }

    public static GameState Create(Deck a, Deck b, int seed)
    {
        var rng = new SeededRandom(seed);
        var p0 = PlayerState.Create(new PlayerId(0), a, rng);
        var p1 = PlayerState.Create(new PlayerId(1), b, rng);

        // Setup: 5 shields (top of deck), then draw 5 cards for each
        p0 = p0.DealShields(5).Draw(5);
        p1 = p1.DealShields(5).Draw(5);

        var gs = new GameState(new PlayerId(0), new PlayerId(0), TurnPhase.Start,
            ImmutableArray.Create(p0, p1), StackState.Empty, rng, passes: 0, turnNumber: 0, nextInstanceId: 1);
        return gs.AdvancePhaseIfNeeded(); // process Start (draw/skip) -> Main
    }

    public IGameState Apply(ActionIntent intent)
    {
        return intent.Type switch
        {
            ActionType.PassPriority => Pass(),
            ActionType.ResolveTop => ResolveTop(),
            ActionType.CastDummySpell => PushDummy(),
            ActionType.PlayMana => PlayMana((int)(intent.Payload ?? 0)),
            ActionType.SummonDummyFromHand => SummonDummy((int)(intent.Payload ?? 0)),
            ActionType.BreakOpponentShield => PushBreakOpponentShield((int)(intent.Payload ?? 0)),
            ActionType.DestroyOpponentCreature => PushDestroyOpponentCreature((int)(intent.Payload ?? 0)),
            ActionType.BuffOwnCreature => PushBuffOwnCreature((int)(intent.Payload ?? 0)),
            _ => this
        };
    }

    public IEnumerable<ActionIntent> GenerateLegalActions(PlayerId player)
    {
        if (player.Value != PriorityPlayer.Value) yield break;

        if (Phase == TurnPhase.Main)
        {
            // Demo no-op
            yield return new ActionIntent(ActionType.CastDummySpell);

            // PlayMana from any hand card
            var hand = Players[player.Value].Hand;
            for (int i = 0; i < hand.Cards.Length; i++)
                yield return new ActionIntent(ActionType.PlayMana, i);

            // Summon dummy creature
            for (int i = 0; i < hand.Cards.Length; i++)
                yield return new ActionIntent(ActionType.SummonDummyFromHand, i);

            // Break opponent shields
            var opp = player.Opponent();
            var oppShields = Players[opp.Value].Shield.Cards;
            for (int si = 0; si < oppShields.Length; si++)
                yield return new ActionIntent(ActionType.BreakOpponentShield, si);

            // Destroy opponent creature
            var oppBattle = Players[opp.Value].Battle.Cards;
            for (int bi = 0; bi < oppBattle.Length; bi++)
                yield return new ActionIntent(ActionType.DestroyOpponentCreature, bi);

            // Buff own creature +1000 this turn
            var myBz = Players[player.Value].Battle.Cards;
            for (int bi = 0; bi < myBz.Length; bi++)
                yield return new ActionIntent(ActionType.BuffOwnCreature, bi);
        }

        // Always can pass
        yield return new ActionIntent(ActionType.PassPriority);

        // Optional resolve surface
        if (!Stack.IsEmpty && ConsecutivePasses >= 1)
            yield return new ActionIntent(ActionType.ResolveTop);
    }

    // ----- Action implementations

    private GameState PushDummy()
    {
        var desc = $"DummySpell by P{PriorityPlayer.Value}";
        return this with { Stack = Stack.Push(new StackItem(StackItemKind.Dummy, PriorityPlayer, TargetSpec.None, desc)), ConsecutivePasses = 0 };
    }

    private GameState PlayMana(int handIndex)
    {
        var p = Players[PriorityPlayer.Value];
        if (handIndex < 0 || handIndex >= p.Hand.Cards.Length) return this;

        var card = p.Hand.Cards[handIndex];
        var newHand = new Zone(ZoneKind.Hand, p.Hand.Cards.RemoveAt(handIndex));
        var newMana = new Zone(ZoneKind.Mana, p.Mana.Cards.Add(card));
        var newPlayer = p with { Hand = newHand, Mana = newMana };

        return this.With(players: Players.SetItem(PriorityPlayer.Value, newPlayer));
    }

    private GameState SummonDummy(int handIndex)
    {
        var p = Players[PriorityPlayer.Value];
        if (handIndex < 0 || handIndex >= p.Hand.Cards.Length) return this;
        var card = p.Hand.Cards[handIndex];
        var newHand = new Zone(ZoneKind.Hand, p.Hand.Cards.RemoveAt(handIndex));
        var newBattle = new Zone(ZoneKind.Battle, p.Battle.Cards.Add(card));
        var newIds = p.BattleIds.Add(this.NextInstanceId);
        var newPlayer = p with { Hand = newHand, Battle = newBattle, BattleIds = newIds };
        return this.With(players: Players.SetItem(PriorityPlayer.Value, newPlayer), nextInstanceId: this.NextInstanceId + 1);
    }

    private GameState PushBreakOpponentShield(int shieldIndex)
    {
        var spec = new TargetSpec(TargetKind.OpponentShield, shieldIndex);
        if (!Targeting.IsLegal(this, PriorityPlayer, spec)) return this;
        return this with { Stack = Stack.Push(new StackItem(StackItemKind.BreakShield, PriorityPlayer, spec)), ConsecutivePasses = 0 };
    }

    private GameState PushDestroyOpponentCreature(int battleIndex)
    {
        var spec = new TargetSpec(TargetKind.OpponentCreature, battleIndex);
        if (!Targeting.IsLegal(this, PriorityPlayer, spec)) return this;
        return this with { Stack = Stack.Push(new StackItem(StackItemKind.DestroyCreature, PriorityPlayer, spec)), ConsecutivePasses = 0 };
    }

    private GameState PushBuffOwnCreature(int battleIndex)
    {
        var spec = new TargetSpec(TargetKind.OwnCreature, battleIndex);
        if (!Targeting.IsLegal(this, PriorityPlayer, spec)) return this;
        return this with { Stack = Stack.Push(new StackItem(StackItemKind.BuffPower, PriorityPlayer, spec, "+1000 this turn")), ConsecutivePasses = 0 };
    }

    private GameState Pass()
    {
        var next = this with { ConsecutivePasses = this.ConsecutivePasses + 1, PriorityPlayer = this.PriorityPlayer.Opponent() };

        // Two consecutive passes with empty stack -> advance phase/turn
        if (next.Stack.IsEmpty && next.ConsecutivePasses >= 2)
        {
            next = next with { ConsecutivePasses = 0 };
            next = next.AdvancePhaseOrTurn();
            return next;
        }

        // Two consecutive passes with non-empty stack -> resolve top
        if (!next.Stack.IsEmpty && next.ConsecutivePasses >= 2)
        {
            next = next.ResolveTopInternal();
            next = next with { PriorityPlayer = next.ActivePlayer, ConsecutivePasses = 0 };
            return next;
        }

        return next;
    }

    private GameState ResolveTop()
    {
        if (Stack.IsEmpty) return this;
        var next = ResolveTopInternal();
        return next with { PriorityPlayer = next.ActivePlayer, ConsecutivePasses = 0 };
    }

    private GameState ResolveTopInternal()
    {
        var item = Stack.Peek();
        var after = this with { Stack = Stack.Pop() };

        switch (item.Kind)
        {
            case StackItemKind.Dummy:
                // no-op
                break;

            case StackItemKind.BreakShield:
            {
                var controller = item.Controller;
                var opp = controller.Opponent();
                if (item.Target.Index is int si)
                {
                    var shields = after.Players[opp.Value].Shield.Cards;
                    if (si >= 0 && si < shields.Length)
                    {
                        var cid = shields[si];
                        var newShields = new Zone(ZoneKind.Shield, shields.RemoveAt(si));
                        var oppP = after.Players[opp.Value];
                        var newHand = new Zone(ZoneKind.Hand, oppP.Hand.Cards.Add(cid));
                        var newOpp = oppP with { Shield = newShields, Hand = newHand };
                        after = after.With(players: after.Players.SetItem(opp.Value, newOpp));
                    }
                }
                break;
            }

            case StackItemKind.DestroyCreature:
            {
                var controller = item.Controller;
                var opp = controller.Opponent();
                if (item.Target.Index is int bi)
                {
                    var bz = after.Players[opp.Value].Battle.Cards;
                    if (bi >= 0 && bi < bz.Length)
                    {
                        var cid = bz[bi];
                        var newBattle = new Zone(ZoneKind.Battle, bz.RemoveAt(bi));
                        var oppP = after.Players[opp.Value];
                        var newIds = oppP.BattleIds.RemoveAt(bi);
                        var newGy = new Zone(ZoneKind.Graveyard, oppP.Graveyard.Cards.Add(cid));
                        var newOpp = oppP with { Battle = newBattle, Graveyard = newGy, BattleIds = newIds };
                        after = after.With(players: after.Players.SetItem(opp.Value, newOpp));
                    }
                }
                break;
            }

            case StackItemKind.BuffPower:
            {
                var controller = item.Controller;
                if (item.Target.Index is int bi)
                {
                    var myBz = after.Players[controller.Value].Battle.Cards;
                    if (bi >= 0 && bi < myBz.Length)
                    {
                        var inst = after.Players[controller.Value].BattleIds[bi];
                        var eff = new PowerBuff(controller, inst, 1000, after.TurnNumber);
                        after = after with { ContinuousEffects = after.ContinuousEffects.Add(eff) };
                    }
                }
                break;
            }
        }

        return after.RunStateBasedActions();
    }

    private GameState RunStateBasedActions()
    {
        return StateBasedActions.Fix(this);
    }

    private GameState DrawCard(PlayerId player)
    {
        var p = Players[player.Value];
        if (p.Deck.Cards.Length == 0)
        {
            // Deck-out: player who fails to draw loses
            var result = player.Value == 0 ? GameResult.Player1Win : GameResult.Player0Win;
            return this with { GameOverResult = result };
        }
        var top = p.Deck.Cards[^1];
        var newDeck = new Zone(ZoneKind.Deck, p.Deck.Cards.RemoveAt(p.Deck.Cards.Length - 1));
        var newHand = new Zone(ZoneKind.Hand, p.Hand.Cards.Add(top));
        var np = p with { Deck = newDeck, Hand = newHand };
        return this.With(players: Players.SetItem(player.Value, np));
    }

    private GameState AdvancePhaseOrTurn()
    {
        var phase = Phase;
        phase = phase switch
        {
            TurnPhase.Start => TurnPhase.Main,
            TurnPhase.Main => TurnPhase.Battle,
            TurnPhase.Battle => TurnPhase.End,
            TurnPhase.End => TurnPhase.Start,
            _ => TurnPhase.Main
        };

        var next = this with { Phase = phase, PriorityPlayer = this.ActivePlayer, ConsecutivePasses = 0 };
        if (phase == TurnPhase.Start)
        {
            var newActive = this.ActivePlayer.Opponent();
            var tn = this.TurnNumber + (newActive.Value == 0 ? 1 : 0);
            var cleared = this.ContinuousEffects.RemoveExpired(tn);
            next = next with { ActivePlayer = newActive, TurnNumber = tn, ContinuousEffects = cleared };
            next = next.AdvancePhaseIfNeeded();
        }
        return next;
    }

    private GameState AdvancePhaseIfNeeded()
    {
        if (this.Phase == TurnPhase.Start)
        {
            var next = this;
            bool skipDraw = (next.TurnNumber == 0 && next.ActivePlayer.Value == 0);
            if (!skipDraw)
            {
                next = next.DrawCard(next.ActivePlayer);
            }
            return next with { Phase = TurnPhase.Main, PriorityPlayer = next.ActivePlayer, ConsecutivePasses = 0 };
        }
        return this;
    }

    public GameState With(PlayerId? active = null, PlayerId? priority = null, TurnPhase? phase = null,
        ImmutableArray<PlayerState>? players = null, StackState? stack = null, IRandomSource? rng = null, int? passes = null, int? turnNumber = null,
        GameResult? gameOver = null, ImmutableArray<PowerBuff>? effects = null, int? nextInstanceId = null, ICardDatabase? cardDb = null)
        => new GameState(active ?? ActivePlayer, priority ?? PriorityPlayer, phase ?? Phase,
            players ?? Players, stack ?? Stack, rng ?? Rng, passes ?? ConsecutivePasses, turnNumber ?? TurnNumber, nextInstanceId ?? NextInstanceId,
            gameOver ?? GameOverResult, effects ?? ContinuousEffects, cardDb ?? CardDb);
}

// ----- Player & zone records -----

public sealed record PlayerState(PlayerId Id,
    Zone Deck,
    Zone Hand,
    Zone Battle,
    ImmutableArray<int> BattleIds,
    Zone Mana,
    Zone Shield,
    Zone Graveyard)
{
    public static PlayerState Create(PlayerId id, Deck deck, IRandomSource rng)
    {
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
            ImmutableArray<int>.Empty,
            new Zone(ZoneKind.Mana, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Shield, ImmutableArray<CardId>.Empty),
            new Zone(ZoneKind.Graveyard, ImmutableArray<CardId>.Empty));
    }

    public PlayerState DealShields(int n)
    {
        var deck = Deck.Cards;
        var take = Math.Min(n, deck.Length);
        var shields = deck.TakeLast(take).ToImmutableArray();
        var newDeck = deck.RemoveRange(deck.Length - take, take);
        return this with { Deck = new Zone(ZoneKind.Deck, newDeck), Shield = new Zone(ZoneKind.Shield, shields) };
    }

    public PlayerState Draw(int n)
    {
        var p = this;
        for (int i = 0; i < n; i++)
        {
            if (p.Deck.Cards.Length == 0) break;
            var top = p.Deck.Cards[^1];
            p = p with
            {
                Deck = new Zone(ZoneKind.Deck, p.Deck.Cards.RemoveAt(p.Deck.Cards.Length - 1)),
                Hand = new Zone(ZoneKind.Hand, p.Hand.Cards.Add(top))
            };
        }
        return p;
    }
}

public readonly record struct Deck(ImmutableArray<CardId> Cards);

public sealed record Zone(ZoneKind Kind, ImmutableArray<CardId> Cards);

// ----- Stack definitions -----

public enum StackItemKind { Dummy, BreakShield, DestroyCreature, BuffPower }

public sealed record StackItem(StackItemKind Kind, PlayerId Controller, TargetSpec Target, string? Info = null);

public sealed class StackState
{
    private readonly ImmutableArray<StackItem> _items;
    public bool IsEmpty => _items.IsDefaultOrEmpty;
    private StackState(ImmutableArray<StackItem> items) => _items = items;
    public static StackState Empty => new StackState(ImmutableArray<StackItem>.Empty);
    public StackState Push(StackItem item) => new StackState(_items.Add(item));
    public StackItem Peek() => _items[_items.Length - 1];
    public StackState Pop() => new StackState(_items.IsEmpty ? _items : _items.RemoveAt(_items.Length - 1));
    public IReadOnlyList<StackItem> Items => _items;
}
