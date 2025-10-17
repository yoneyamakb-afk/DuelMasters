
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using DuelMasters.Engine;

namespace DuelMasters.CLI;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("Duel Masters CLI - Priority/Stack demo");
        // Robust DB path resolution
string? envPath = Environment.GetEnvironmentVariable("DM_DB_PATH");
string[] candidates = new string[] {
    // 1) Environment override
    envPath ?? "",
    // 2) Solution root (bin/Debug/net8.0 -> up 5)
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..","..","..","..","..","Duelmasters.db")),
    // 3) src root (up 4) - legacy fallback
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..","..","..","..","Duelmasters.db")),
    // 4) current working directory
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Duelmasters.db")),
    // 5) alongside executable
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Duelmasters.db")),
};
string? dbPath = null;
foreach (var c in candidates)
{
    if (!string.IsNullOrWhiteSpace(c) && File.Exists(c)) { dbPath = c; break; }
}
if (dbPath == null) dbPath = candidates[0]; // report env path or first candidate even if missing to guide the user
        if (dbPath == null || !File.Exists(dbPath))
        {
            Console.WriteLine($"[!] DB not found. Looked at: {dbPath}\nSet DM_DB_PATH or place Duelmasters.db at the solution root next to DuelMasters.sln.");
            Console.WriteLine("Create or copy your Duelmasters.db next to the solution root.");
        }
        else
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' LIMIT 3;";
            using var r = cmd.ExecuteReader();
            Console.WriteLine("DB tables (peek):");
            while (r.Read())
                Console.WriteLine(" - " + r.GetString(0));
        }

        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());

        var sim = new Simulator();
        var s = sim.InitialState(a, b, seed: 42);

        int steps = 0;
        while (!sim.IsTerminal(s, out var result) && steps < 80)
        {
            var legal = sim.Legal(s).ToList();

            // Very small policy:
            // - 30%: Cast dummy spell if legal and stack empty (to exercise stack)
            // - else: Pass priority
            ActionIntent pick;

var canCast = legal.Exists(x => x.Type == ActionType.CastDummySpell);
var canPlayMana = legal.FindAll(x => x.Type == ActionType.PlayMana).ToList();

if (canPlayMana.Count > 0 && s.Phase == TurnPhase.Main && s.Rng.Next(0,10) < 4)
    pick = canPlayMana[s.Rng.Next(0, canPlayMana.Count)];
else if (canCast && s.Stack.IsEmpty && s.Rng.Next(0,10) < 2)
    pick = new ActionIntent(ActionType.CastDummySpell);
else if (legal.Exists(x => x.Type == ActionType.ResolveTop) && s.Rng.Next(0,10) < 5)
    pick = new ActionIntent(ActionType.ResolveTop);
else
    pick = new ActionIntent(ActionType.PassPriority);

            Console.WriteLine($"P{s.PriorityPlayer.Value} {s.Phase} -> {pick.Type}" + (pick.Type==ActionType.CastDummySpell? " (pushed)": (pick.Type==ActionType.PlayMana||pick.Type==ActionType.SummonDummyFromHand||pick.Type==ActionType.BreakOpponentShield||pick.Type==ActionType.DestroyOpponentCreature? $" (i={(int)(pick.Payload??0)})" : "")));
            s = sim.Step(s, pick);

            // Log when resolution happens
            if (pick.Type == ActionType.ResolveTop)
                Console.WriteLine($"   [Resolved top] Remaining stack: {s.Stack.Items.Count}");

            // When pass advances phase/turn or resolves automatically, you'll see priority switch in next loop
            steps++;
        }

        Console.WriteLine("Demo finished.");
        return 0;
    }
}
