using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using Microsoft.Data.Sqlite;
using DMRules.Engine.TextParsing;

namespace DMRules.Tools
{
    public static class ScanProgram
    {
        public static int Run(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: scan --db <Duelmasters.db> [--limit 100] [--out ./artifacts]");
                return 2;
            }

            string dbPath = "";
            int limit = 100;
            string outDir = "./artifacts";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--db" && i + 1 < args.Length) { dbPath = args[++i]; }
                else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[i+1], out var lim)) { limit = lim; i++; }
                else if (args[i] == "--out" && i + 1 < args.Length) { outDir = args[++i]; }
            }

            if (string.IsNullOrWhiteSpace(dbPath) || !File.Exists(dbPath))
            {
                Console.Error.WriteLine($"DB not found: {dbPath}");
                return 3;
            }

            Directory.CreateDirectory(outDir);

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT face_id, abilitytxt FROM card_face WHERE abilitytxt IS NOT NULL AND TRIM(abilitytxt) <> ''";

            using var reader = cmd.ExecuteReader();
            int rows = 0;
            while (reader.Read())
            {
                rows++;
                string ability = reader.IsDBNull(1) ? "" : reader.GetString(1);

                // NEW: parser-oriented normalization (conservative)
                var normalized = CardTextNormalizer.NormalizeForParsing(ability);

                var result = CardTextParser.Parse(normalized);
                foreach (var phrase in result.UnresolvedPhrases)
                {
                    var norm = NormalizeForCounting(phrase);
                    if (norm.Length == 0) continue;
                    counts.TryGetValue(norm, out var c);
                    counts[norm] = c + 1;
                }
            }

            var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
            var csvPath = System.IO.Path.Combine(outDir, $"UNRESOLVED_top{limit}_{ts}.csv");
            var jsonPath = System.IO.Path.Combine(outDir, $"UNRESOLVED_top{limit}_{ts}.json");

            var ordered = counts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();

            using (var writer = new StreamWriter(csvPath, false, new System.Text.UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField("rank"); csv.WriteField("phrase"); csv.WriteField("count");
                csv.NextRecord();
                int rank = 1;
                foreach (var kv in ordered)
                {
                    csv.WriteField(rank++);
                    csv.WriteField(kv.Key);
                    csv.WriteField(kv.Value);
                    csv.NextRecord();
                }
            }

            using (var jw = new StreamWriter(jsonPath, false, new System.Text.UTF8Encoding(true)))
            {
                jw.WriteLine("[");
                for (int i = 0; i < ordered.Count; i++)
                {
                    var kv = ordered[i];
                    var line = $"  {{\"rank\":{i+1},\"phrase\":\"{EscapeJson(kv.Key)}\",\"count\":{kv.Value}}}";
                    if (i < ordered.Count - 1) line += ",";
                    jw.WriteLine(line);
                }
                jw.WriteLine("]");
            }

            Console.WriteLine($"Rows scanned: {rows}");
            Console.WriteLine($"Top{limit} written:");
            Console.WriteLine($" - {System.IO.Path.GetFullPath(csvPath)}");
            Console.WriteLine($" - {System.IO.Path.GetFullPath(jsonPath)}");
            return 0;
        }

        private static string NormalizeForCounting(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = Regex.Replace(s, @"[\r\n\t]+", " ");
            s = s.Replace("　", " ");
            s = Regex.Replace(s, @"\s{2,}", " ").Trim();
            return s;
        }

        private static string EscapeJson(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

    }
}
