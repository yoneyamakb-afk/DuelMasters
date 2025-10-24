using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using DuelMasters.Engine;
using DuelMasters.Engine.Integration.M11;

namespace DuelMasters.CLI;

internal static class Program
{
    private static bool _traceConsole = false;
    private static string? _traceJsonPath = null;
    private static string _traceLevel = "basic"; // basic|detail|debug

    // ---- Event tracing (wrap EngineHooks.OnEvent) ----
    private static void InstallTracing()
    {
        var prev = EngineHooks.OnEvent;
        EngineHooks.OnEvent = ev =>
        {
            if (_traceConsole)
            {
                Console.WriteLine($"[M11] {ev.TimestampUtc:O} {ev.Kind} phase={ev.PhaseName ?? "-"} ap={ev.ActivePlayerId?.ToString() ?? "-"}");
            }

            if (!string.IsNullOrEmpty(_traceJsonPath))
            {
                try
                {
                    var line = JsonSerializer.Serialize(ev, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
                    File.AppendAllText(_traceJsonPath!, line + Environment.NewLine);
                }
                catch { /* ignore logging errors */ }
            }

            prev?.Invoke(ev);
        };
    }

    // ---- CLI args ----
    private static void ParseArgs(string[] args)
    {
        foreach (var a in args)
        {
            if (a.Equals("--trace", StringComparison.OrdinalIgnoreCase)) _traceConsole = true;
            else if (a.StartsWith("--trace-json=", StringComparison.OrdinalIgnoreCase))
            {
                _traceJsonPath = a.Substring("--trace-json=".Length).Trim('"');
                if (string.IsNullOrWhiteSpace(_traceJsonPath)) _traceJsonPath = "trace.jsonl";
                try { File.WriteAllText(_traceJsonPath, string.Empty); } catch { }
            }
            else if (a.StartsWith("--trace-level=", StringComparison.OrdinalIgnoreCase))
            {
                _traceLevel = a.Substring("--trace-level=".Length).Trim().ToLowerInvariant();
                if (_traceLevel != "basic" && _traceLevel != "detail" && _traceLevel != "debug") _traceLevel = "basic";
            }
        }
    }

    // ---- Snapshot / helpers ----
    private sealed class Snapshot
    {
        public int Turn { get; init; }
        public string Phase { get; init; } = "";
        public int ActivePlayer { get; init; }
        public int PriorityPlayer { get; init; }
        public int ConsecutivePasses { get; init; }
        public int StackCount { get; init; }
        public int PendingTriggers { get; init; }
    }

    private static Snapshot Take(GameState g)
    {
        // StackState は IEnumerable を実装していないため、Count取得に失敗したら 0 にフォールバックする
        int stackCount;
        try
        {
            stackCount = g.Stack.Count;
        }
        catch
        {
            stackCount = 0; // 列挙不能な型。トレース用途なので安全に 0 固定
        }

        int triggers = 0;
        try { triggers = g.PendingTriggers.Length; } catch { triggers = 0; }

        return new Snapshot
        {
            Turn = g.TurnNumber,
            Phase = g.Phase.ToString(),
            ActivePlayer = g.ActivePlayer.Value,
            PriorityPlayer = g.PriorityPlayer.Value,
            ConsecutivePasses = g.ConsecutivePasses,
            StackCount = stackCount,
            PendingTriggers = triggers
        };
    }

    private static void EmitStepTrace(string title, Snapshot before, Snapshot after)
    {
        void diffLine(string key, object a, object b)
        {
            if (!Equals(a, b) && _traceConsole && (_traceLevel == "detail" || _traceLevel == "debug"))
                Console.WriteLine($"  - {key}: {a} -> {b}");
        }

        if (_traceConsole)
            Console.WriteLine($"[STEP] {title}");

        diffLine("Turn", before.Turn, after.Turn);
        diffLine("Phase", before.Phase, after.Phase);
        diffLine("ActivePlayer", before.ActivePlayer, after.ActivePlayer);
        diffLine("PriorityPlayer", before.PriorityPlayer, after.PriorityPlayer);
        diffLine("ConsecutivePasses", before.ConsecutivePasses, after.ConsecutivePasses);
        diffLine("StackCount", before.StackCount, after.StackCount);
        diffLine("PendingTriggers", before.PendingTriggers, after.PendingTriggers);

        if (!string.IsNullOrEmpty(_traceJsonPath))
        {
            try
            {
                var obj = new { kind = "step", title, before, after };
                File.AppendAllText(_traceJsonPath!, JsonSerializer.Serialize(obj) + Environment.NewLine);
            }
            catch { }
        }

        if (_traceConsole && _traceLevel == "debug")
        {
            Console.WriteLine($"  before: Turn={before.Turn}, Phase={before.Phase}, AP={before.ActivePlayer}, PP={before.PriorityPlayer}, Passes={before.ConsecutivePasses}, Stack={before.StackCount}, Trig={before.PendingTriggers}");
            Console.WriteLine($"  after : Turn={after.Turn}, Phase={after.Phase}, AP={after.ActivePlayer}, PP={after.PriorityPlayer}, Passes={after.ConsecutivePasses}, Stack={after.StackCount}, Trig={after.PendingTriggers}");
        }
    }

    private static Deck MakeDeck(params int[] ids)
    {
        return new Deck(ImmutableArray.Create<CardId>(ids.Select(i => new CardId(i)).ToArray()));
    }

    private static void Main(string[] args)
    {
        ParseArgs(args);
        InstallTracing();

        Console.WriteLine("=== DuelMasters Simulator CLI (M11.6 Trace) ===");

        var deckA = MakeDeck(1, 2, 3, 4, 5);
        var deckB = MakeDeck(6, 7, 8, 9, 10);

        var game = GameState.Create(deckA, deckB, seed: 1234);

        Console.WriteLine($"Game initialized: Turn {game.TurnNumber}, ActivePlayer={game.ActivePlayer.Value}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"-- Turn {game.TurnNumber} -- Active={game.ActivePlayer.Value}");

            var actions = game.GenerateLegalActions(game.PriorityPlayer).ToList();
            Console.WriteLine($"Available actions: {actions.Count}");
            if (actions.Count == 0) break;

            var before = Take(game);
            var first = actions.First();
            game = game.Apply(first);
            var after = Take(game);
            EmitStepTrace($"Apply({first.Type})", before, after);
        }

        Console.WriteLine("=== Simulation End ===");
        if (!string.IsNullOrEmpty(_traceJsonPath))
        {
            Console.WriteLine($"Trace written to {_traceJsonPath}");
        }
    }
}
