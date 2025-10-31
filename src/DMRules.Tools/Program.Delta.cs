﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using System.Text.Json;

namespace DMRules.Tools
{
    public static class DeltaProgram
    {
        private record Item(int rank, string phrase, int count);

        public static int Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: dotnet run --project src/DMRules.Tools -- delta <old.json> <new.json> [--out ./artifacts]");
                return 2;
            }
            string oldPath = args[0];
            string newPath = args[1];
            string outDir = "./artifacts";

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--out" && i + 1 < args.Length) { outDir = args[i+1]; i++; }
            }

            Directory.CreateDirectory(outDir);

            var oldList = Read(oldPath);
            var newList = Read(newPath);

            var oldMap = oldList.ToDictionary(x => x.phrase, x => x, StringComparer.OrdinalIgnoreCase);
            var newMap = newList.ToDictionary(x => x.phrase, x => x, StringComparer.OrdinalIgnoreCase);

            // Categories
            var resolved = new List<(string phrase, int oldCount)>();                  // in old, not in new
            var decreased = new List<(string phrase, int oldCount, int newCount)>();   // count down
            var increased = new List<(string phrase, int oldCount, int newCount)>();   // count up (should be rare)
            var emerged = new List<(string phrase, int newCount)>();                   // in new only

            foreach (var kv in oldMap)
            {
                if (!newMap.ContainsKey(kv.Key))
                {
                    resolved.Add((kv.Key, kv.Value.count));
                }
                else
                {
                    var o = kv.Value;
                    var n = newMap[kv.Key];
                    if (n.count < o.count) decreased.Add((kv.Key, o.count, n.count));
                    else if (n.count > o.count) increased.Add((kv.Key, o.count, n.count));
                }
            }
            foreach (var kv in newMap)
            {
                if (!oldMap.ContainsKey(kv.Key))
                    emerged.Add((kv.Key, kv.Value.count));
            }

            var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
            var csv1 = Path.Combine(outDir, $"DELTA_resolved_{ts}.csv");
            var csv2 = Path.Combine(outDir, $"DELTA_decreased_{ts}.csv");
            var csv3 = Path.Combine(outDir, $"DELTA_increased_{ts}.csv");
            var csv4 = Path.Combine(outDir, $"DELTA_emerged_{ts}.csv");
            var md   = Path.Combine(outDir, $"DELTA_summary_{ts}.md");

            WriteCsv(csv1, new []{"phrase","oldCount"}, resolved.Select(x => new []{x.phrase, x.oldCount.ToString()}));
            WriteCsv(csv2, new []{"phrase","oldCount","newCount","delta"}, decreased.Select(x => new []{x.phrase, x.oldCount.ToString(), x.newCount.ToString(), (x.newCount-x.oldCount).ToString()}));
            WriteCsv(csv3, new []{"phrase","oldCount","newCount","delta"}, increased.Select(x => new []{x.phrase, x.oldCount.ToString(), x.newCount.ToString(), (x.newCount-x.oldCount).ToString()}));
            WriteCsv(csv4, new []{"phrase","newCount"}, emerged.Select(x => new []{x.phrase, x.newCount.ToString()}));

            using (var w = new StreamWriter(md, false, new UTF8Encoding(true)))
            {
                w.WriteLine("# Unresolved Delta Summary");
                w.WriteLine();
                w.WriteLine($"- Old JSON: `{Path.GetFileName(oldPath)}`");
                w.WriteLine($"- New JSON: `{Path.GetFileName(newPath)}`");
                w.WriteLine($"- Resolved   : {resolved.Count}");
                w.WriteLine($"- Decreased  : {decreased.Count}");
                w.WriteLine($"- Increased  : {increased.Count}");
                w.WriteLine($"- Newly Emerged: {emerged.Count}");
                w.WriteLine();
                w.WriteLine("## Top Resolved (max 20)");
                foreach (var p in resolved.Take(20))
                    w.WriteLine($"- {p.phrase} (old {p.oldCount})");
                w.WriteLine();
                w.WriteLine("## Top Decreased (max 20)");
                foreach (var p in decreased.OrderBy(x => x.newCount - x.oldCount).Take(20))
                    w.WriteLine($"- {p.phrase} ({p.oldCount} -> {p.newCount})");
                w.WriteLine();
                w.WriteLine("## Newly Emerged (max 20)");
                foreach (var p in emerged.OrderByDescending(x => x.newCount).Take(20))
                    w.WriteLine($"- {p.phrase} (new {p.newCount})");
            }

            Console.WriteLine("Delta written:");
            Console.WriteLine(" - " + csv1);
            Console.WriteLine(" - " + csv2);
            Console.WriteLine(" - " + csv3);
            Console.WriteLine(" - " + csv4);
            Console.WriteLine(" - " + md);
            return 0;
        }

        private static List<Item> Read(string path)
        {
            using var stream = File.OpenRead(path);
            var items = JsonSerializer.Deserialize<List<Item>>(stream);
            return items ?? new List<Item>();
        }

        private static void WriteCsv(string path, IEnumerable<string> headers, IEnumerable<string[]> rows)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            foreach (var h in headers) csv.WriteField(h);
            csv.NextRecord();
            foreach (var r in rows) { foreach (var c in r) csv.WriteField(c); csv.NextRecord(); }
        }
    }
}
