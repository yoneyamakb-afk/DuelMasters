
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using DuelMasters.Engine;

namespace DuelMasters.CLI;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("Duel Masters CLI - Random vs Random (skeleton)");
        var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Duelmasters.db"));
        if (!File.Exists(dbPath))
        {
            Console.WriteLine($"[!] DB not found at {dbPath}");
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

        // Prepare trivial decks of 40 identical sample cards (IDs are sequential for hashing determinism)
        var a = new Deck(Enumerable.Range(0, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());
        var b = new Deck(Enumerable.Range(1000, 40).Select(i => new CardId(i)).ToArray().ToImmutableArray());

        var sim = new Simulator();
        var state = sim.InitialState(a, b, seed: 42);

        int steps = 0;
        while (!sim.IsTerminal(state, out var result) && steps < 50)
        {
            var legal = sim.Legal(state).ToList();
            var pick = legal[state.Rng.Next(0, legal.Count)];
            state = sim.Step(state, pick);
            steps++;
        }

        Console.WriteLine("Finished. (stubbed terminal check) Steps=" + steps);
        return 0;
    }
}
