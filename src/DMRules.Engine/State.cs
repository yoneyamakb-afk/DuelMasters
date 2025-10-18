
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMRules.Engine
{
    public enum Phase { Main }
    public enum Priority { TurnPlayer, NonTurnPlayer }

    public sealed class PlayerId : IEquatable<PlayerId>
    {
        public int Id { get; }
        public PlayerId(int id) => Id = id;
        public static implicit operator PlayerId(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new PlayerId(1);
            var t = s.Trim().ToUpperInvariant();
            if (t.StartsWith("P") && int.TryParse(t.Substring(1), out var pn)) return new PlayerId(pn);
            if (int.TryParse(t, out var n)) return new PlayerId(n);
            return new PlayerId(1);
        }
        public bool Equals(PlayerId? other) => other is not null && Id == other.Id;
        public override bool Equals(object? obj) => Equals(obj as PlayerId);
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => $"P{Id}";
    }

    public sealed class Creature
    {
        public string Name { get; }
        public int Power { get; private set; }
        public bool Destroyed { get; private set; }
        public Creature(string name, int power) { Name = name; Power = power; }
        public void ModifyPower(int delta) => Power += delta;
        public void MarkDestroyed() => Destroyed = true;
        public override string ToString() => $"{Name}({Power}){(Destroyed ? "✖" : "")}";
    }

    public sealed class StackItem
    {
        public string Label { get; }
        public PlayerId Controller { get; }
        public StackItem(string label, PlayerId controller) { Label = label; Controller = controller; }
    }

    public sealed class ReplacementRegistry
    {
        private readonly HashSet<Guid> _applied = new();
        public bool TryApply(Guid eventId)
        {
            if (_applied.Contains(eventId)) return false;
            _applied.Add(eventId);
            return true;
        }
        public bool HasApplied(Guid eventId) => _applied.Contains(eventId);
    }

    public sealed class State
    {
        public void SetPhase(Phase phase) => Phase = phase;
        public Phase Phase { get; private set; }
        public Priority Priority { get; internal set; }
        public PlayerId TurnPlayer { get; }
        public PlayerId NonTurnPlayer { get; }
        public List<Creature> Battlefield { get; }
        public List<StackItem> Stack { get; }
        public Dictionary<PlayerId, Queue<string>> Decks { get; }
        public PlayerId? LosingPlayer { get; private set; }
        public ReplacementRegistry Replacement { get; } = new();

        public State(PlayerId tp, PlayerId np)
        {
            TurnPlayer = tp;
            NonTurnPlayer = np;
            Phase = Phase.Main;
            Priority = Priority.TurnPlayer;
            Battlefield = new List<Creature>();
            Stack = new List<StackItem>();
            Decks = new Dictionary<PlayerId, Queue<string>>
            {
                [tp] = new Queue<string>(Enumerable.Range(0, 10).Select(i => $"TP-{i}")),
                [np] = new Queue<string>(Enumerable.Range(0, 10).Select(i => $"NP-{i}")),
            };
        }

        public void SetPhaseByString(string? phase)
        {
            if (string.IsNullOrWhiteSpace(phase)) return;
            var p = phase.Trim().ToLowerInvariant();
            if (p == "main" || p == "start" || p == "mainphase") Phase = Phase.Main;
        }

        public void Draw(PlayerId p)
        {
            if (!Decks.TryGetValue(p, out var deck) || deck.Count == 0)
            {
                LosingPlayer = p;
                return;
            }
            deck.Dequeue();
        }

        public void EvaluateSBA()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var c in Battlefield.Where(c => !c.Destroyed && c.Power <= 0).ToList())
                {
                    c.MarkDestroyed();
                    changed = true;
                }
                // ★ ここで Battlefield.RemoveAll(...) はしない
            } while (changed);
        }

        public IReadOnlyList<StackItem> ResolveBatchAPNAP(IEnumerable<StackItem> items)
        {
            var list = new List<StackItem>(items);
            var tp = list.Where(x => x.Controller.Equals(TurnPlayer));
            var np = list.Where(x => x.Controller.Equals(NonTurnPlayer));
            return tp.Concat(np).ToList();
        }

        public bool TryReplacementOnce(Guid eventId) => Replacement.TryApply(eventId);
    }

    public enum EventKind { Unknown = 0, Draw = 1, PowerChange = 2, Custom = 255 }

    public readonly struct GameEvent
    {
        public Guid Id { get; }
        public EventKind Kind { get; }
        public int Value { get; }
        public GameEvent(Guid id, EventKind kind, int value = 0) { Id = id; Kind = kind; Value = value; }
        public static GameEvent Create(EventKind kind, int value = 0) => new GameEvent(Guid.NewGuid(), kind, value);
    }

    public interface IGameState
    {
        Phase Phase { get; }
        PlayerId PriorityPlayer { get; }
        IReadOnlyList<Creature> BattleZone { get; }
        IReadOnlyList<PlayerId> Losers { get; }
    }

    public sealed class MinimalState : IGameState
    {
        public State S { get; }

        // 既定コンストラクタ
        public MinimalState()
        {
            S = new State(new PlayerId(1), new PlayerId(2));
            S.SetPhase(Phase.Main);
            S.Priority = Priority.TurnPlayer;
        }

        // フェーズだけ指定
        public MinimalState(Phase phase)
        {
            S = new State(new PlayerId(1), new PlayerId(2));
            S.SetPhase(phase);
            S.Priority = Priority.TurnPlayer;
        }

        // バトルゾーン + フェーズ + 優先権
        public MinimalState(int bz, Phase phase, Priority priority)
        {
            S = new State(new PlayerId(1), new PlayerId(2));
            S.SetPhase(phase);
            S.Priority = priority;
            for (int i = 0; i < bz; i++)
                S.Battlefield.Add(new Creature($"C{i}", 0));
        }

        public MinimalState(int libraryCountTP, bool attemptDrawTP)
        {
            S = new State(new PlayerId(1), new PlayerId(2));

            // フェーズ/優先権の初期状態
            S.SetPhase(Phase.Main);                // ← SetPhaseが無ければ S.SetPhaseByString("Main");
            S.Priority = Priority.TurnPlayer;

            // TPの山札枚数を libraryCountTP に設定
            if (!S.Decks.TryGetValue(S.TurnPlayer, out var q))
                S.Decks[S.TurnPlayer] = q = new Queue<string>();

            q.Clear();
            for (int i = 0; i < Math.Max(0, libraryCountTP); i++)
                q.Enqueue($"TP-Card{i + 1}");

            // そのまま1ドローを試みる（残枚数0なら敗北フラグが立つ）
            if (attemptDrawTP)
                S.Draw(S.TurnPlayer);
        }

        public Phase Phase => S.Phase;
        public PlayerId PriorityPlayer => S.Priority == Priority.TurnPlayer ? S.TurnPlayer : S.NonTurnPlayer;
        public IReadOnlyList<Creature> BattleZone => S.Battlefield;
        public IReadOnlyList<PlayerId> Losers => S.LosingPlayer is null ? Array.Empty<PlayerId>() : new[] { S.LosingPlayer };

        private void SetDeckSize(PlayerId p, int size)
        {
            if (!S.Decks.TryGetValue(p, out var q)) return;
            q.Clear();
            for (int i = 0; i < Math.Max(0, size); i++) q.Enqueue($"{p}-Card{i + 1}");
        }

        private void ApplyArgs(Dictionary<string, object?>? args)
        {
            if (args is null) return;
            if (args.TryGetValue("phase", out var p) && p is string ps) S.SetPhaseByString(ps);
            if (args.TryGetValue("libraryCountTP", out var ltp)) SetDeckSize(S.TurnPlayer, ToInt(ltp, 10));
            if (args.TryGetValue("libraryCountNP", out var lnp)) SetDeckSize(S.NonTurnPlayer, ToInt(lnp, 10));
            if (args.TryGetValue("bz", out var bzv))
            {
                if (bzv is int bn) { for (int i = 0; i < Math.Max(0, bn); i++) S.Battlefield.Add(new Creature($"C{i + 1}", 0)); }
                else if (bzv is string bs) { InitBattleZoneFromDescriptor(bs); }
            }
            if (args.TryGetValue("attemptDrawTP", out var ad) && ToBool(ad)) S.Draw(S.TurnPlayer);
        }

        private static int ToInt(object? v, int def)
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
            if (v is string s && int.TryParse(s, out var p)) return p;
            return def;
        }

        private static bool ToBool(object? v)
        {
            if (v is bool b) return b;
            if (v is string s && bool.TryParse(s, out var pb)) return pb;
            if (v is int i) return i != 0;
            return false;
        }

        private void InitBattleZoneFromDescriptor(string bz)
        {
            if (string.IsNullOrWhiteSpace(bz)) return;
            var t = bz.Trim();
            if (int.TryParse(t, out var n))
            {
                for (int i = 0; i < Math.Max(0, n); i++) S.Battlefield.Add(new Creature($"C{i + 1}", 0));
                return;
            }
            var parts = t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int idx = 1;
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var pow))
                    S.Battlefield.Add(new Creature($"C{idx++}", pow));
            }
        }
    }

    public interface IEngineAdapter
    {
        State CreateInitial();
        IGameState Audit { get; }
    }

    public static class EngineAdapterCore
    {
        public static State CreateInitial() => new State(new PlayerId(1), new PlayerId(2));
    }

    public sealed class EngineAdapter : IEngineAdapter
    {
        public State CreateInitial() => EngineAdapterCore.CreateInitial();
        public IGameState Audit => new MinimalState();
    }

    public static class Adapter
    {
        public static IEngineAdapter Instance { get; } = new EngineAdapter();
    }

    public static class AdapterExtensions
    {
        public static IGameState ApplyEventWithReplacement(this IEngineAdapter adapter, IGameState gs, GameEvent ev)
        {
            var s = Extract(gs);
            if (ev.Kind == EventKind.Draw)
                s.Draw(s.TurnPlayer);
            s.TryReplacementOnce(ev.Id);
            return gs;
        }

        public static IGameState EnqueueNewTriggers(this IEngineAdapter adapter, IGameState gs, params string[] labels)
        {
            var s = Extract(gs);
            foreach (var label in labels ?? Array.Empty<string>())
                s.Stack.Add(new StackItem(label, s.TurnPlayer));
            return gs;
        }

        public static IGameState ResolveSAndOtherTriggers(this IEngineAdapter adapter, IGameState gs)
        {
            var s = Extract(gs);
            var _ = s.ResolveBatchAPNAP(s.Stack);
            s.Stack.Clear();
            return gs;
        }

        public static IGameState DoSBAUntilStable(this IEngineAdapter adapter, IGameState gs)
        {
            var s = Extract(gs);
            s.EvaluateSBA();
            return gs;
        }

        public static int PendingTriggersCount(this IEngineAdapter adapter, IGameState gs)
        {
            var s = Extract(gs);
            return s.Stack.Count;
        }

        public static IGameState Audit(this IEngineAdapter adapter) => adapter.Audit;
        public static IGameState Audit(this IEngineAdapter adapter, IGameState gs) => gs;

        private static State Extract(IGameState gs)
        {
            if (gs is MinimalState ms) return ms.S;
            throw new InvalidOperationException("Unsupported IGameState implementation.");
        }
    }

    public static class GameStateDebugExtensions
    {
        public static string Dump(this IGameState gs)
        {
            if (gs is not MinimalState ms) return "<unknown>";
            var s = ms.S;
            var sb = new StringBuilder();
            sb.AppendLine($"Phase: {s.Phase}");
            sb.AppendLine($"Priority: {(s.Priority == Priority.TurnPlayer ? "TP" : "NP")}");
            sb.AppendLine($"TP: {s.TurnPlayer}, NP: {s.NonTurnPlayer}");
            if (s.LosingPlayer is not null) sb.AppendLine($"Loser: {s.LosingPlayer}");
            sb.AppendLine("BZ:");
            foreach (var c in s.Battlefield) sb.AppendLine($" - {c}");
            sb.AppendLine($"Stack: {s.Stack.Count} item(s)");
            return sb.ToString();
        }
    }
}
