using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DbToAst
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var o = Cli.Parse(args);
                if (o.ShowHelp)
                {
                    Console.WriteLine(Cli.Help);
                    return 0;
                }

                Directory.CreateDirectory(o.OutDir);

                if (!string.IsNullOrWhiteSpace(o.DbPath) && File.Exists(o.DbPath))
                {
                    Console.WriteLine($"[DbToAst] using DB: {o.DbPath}");
                    var n = FromDatabase(o.DbPath, o.OutDir, o.SkipTwinImpact, o.SkipHyperMode, o.Limit);
                    Console.WriteLine($"[DbToAst] done (DB): {n} files");
                    return 0;
                }
                else
                {
                    Console.WriteLine("[DbToAst] no DB provided or not found. Nothing to do.");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[DbToAst] Fatal: " + ex);
                return 1;
            }
        }

        static int FromDatabase(string dbPath, string outDir, bool skipTwin, bool skipHyper, int? limit)
        {
            int count = 0;
            using var con = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            con.Open();

            var tables = ListTables(con);
            // Prefer card_face when available
            string table = tables.Contains("card_face") ? "card_face" :
                           tables.Contains("cards") ? "cards" :
                           throw new InvalidOperationException("No known card table found (expected 'card_face' or 'cards').");

            var cols = ListColumns(con, table);
            Console.WriteLine($"[DbToAst] table={table}, columns=[{string.Join(",", cols)}]");

            // Build SELECT dynamically: only reference columns that actually exist.
            string idExpr          = FirstExistingExpr(cols, new[]{"face_id","id","card_id"}, "'?'") + " AS id";
            string nameExpr        = FirstExistingExpr(cols, new[]{"cardname","name","card_name"}, "''") + " AS name";
            string typeExpr        = FirstExistingExpr(cols, new[]{"typetxt","type","card_type"}, "NULL") + " AS type";
            string costExpr        = "CAST(" + FirstExistingExpr(cols, new[]{"costtxt","cost","mana_cost"}, "NULL") + " AS INTEGER) AS cost";
            string civExpr         = FirstExistingExpr(cols, new[]{"civiltxt","civilization","civ"}, "NULL") + " AS civilization";
            string powerExpr       = "CAST(" + FirstExistingExpr(cols, new[]{"powertxt","power","base_power"}, "NULL") + " AS INTEGER) AS power";
            string raceExpr        = FirstExistingExpr(cols, new[]{"racetxt","race","tribe"}, "NULL") + " AS race";
            string textRawExpr     = FirstExistingExpr(cols, new[]{"abilitytxt","text_raw","text","rules_text"}, "''") + " AS text_raw";
            string twinImpactExpr  = FirstExistingExpr(cols, new[]{"twin_impact","twinImpact","is_twin_impact"}, "0") + " AS twinImpact";
            string hyperModeExpr   = FirstExistingExpr(cols, new[]{"hyper_mode","hyperMode","is_hyper_mode"}, "0") + " AS hyperMode";

            string selectList = string.Join(",\n  ", new[]{ idExpr, nameExpr, typeExpr, costExpr, civExpr, powerExpr, raceExpr, textRawExpr, twinImpactExpr, hyperModeExpr });

            var limitSql = (limit.HasValue && limit.Value >= 0) ? $" LIMIT {limit.Value}" : " LIMIT -1";
            string sql = $"SELECT\n  {selectList}\nFROM {table}{limitSql};";

            Console.WriteLine("[DbToAst] SQL:\n" + sql);

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var rec = new CardRec(rd);
                    if (skipTwin && rec.TwinImpact) continue;
                    if (skipHyper && rec.HyperMode) continue;

                    var ast = AstFromRecord(rec);
                    var file = Path.Combine(outDir, $"{SanitizeFileName(ast.cardId)}.json");
                    File.WriteAllText(file, JsonSerializer.Serialize(ast, new JsonSerializerOptions{WriteIndented=true}));
                    Console.WriteLine($"[DbToAst] wrote: {file}");
                    count++;
                }
            }
            return count;
        }

        static HashSet<string> ListTables(SqliteConnection con)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) set.Add(rd.GetString(0));
            return set;
        }

        static List<string> ListColumns(SqliteConnection con, string table)
        {
            var cols = new List<string>();
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({table});";
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) cols.Add(rd.GetString(1)); // name column
            return cols;
        }

        static string FirstExistingExpr(List<string> cols, IEnumerable<string> candidates, string fallbackLiteral)
        {
            foreach (var c in candidates)
            {
                if (cols.Any(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase)))
                    return c;
            }
            return fallbackLiteral; // literal like '','0',NULL
        }

        static string SanitizeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

        static AstCard AstFromRecord(CardRec r)
        {
            var flags = new List<string>();
            var raw = r.TextRaw ?? string.Empty;
            if (raw.Contains("シールド・トリガー")) flags.Add("ShieldTrigger");
            if (raw.Contains("スピードアタッカー")) flags.Add("SpeedAttacker");
            if (raw.Contains("ブロッカー")) flags.Add("Blocker");

            return new AstCard
            {
                schemaVersion = 1,
                cardId = r.Id,
                name = r.Name,
                types = new List<string>{ r.Type ?? "Unknown" },
                cost = r.Cost,
                civilizations = Split(r.Civilization),
                power = r.Power,
                race = Split(r.Race),
                effects = new List<Effect>(),
                flags = flags
            };
        }

        static List<string> Split(string? s)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(s)) return list;
            foreach (var part in s.Replace("／","/").Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                list.Add(part.Trim());
            }
            return list;
        }
    }

    public record CardRec
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Type { get; init; }
        public int? Cost { get; init; }
        public string? Civilization { get; init; }
        public int? Power { get; init; }
        public string? Race { get; init; }
        public string? TextRaw { get; init; }
        public bool TwinImpact { get; init; }
        public bool HyperMode { get; init; }

        public CardRec(SqliteDataReader rd)
        {
            Id = rd["id"]?.ToString() ?? Guid.NewGuid().ToString("N");
            Name = rd["name"]?.ToString() ?? "";
            Type = rd["type"]?.ToString();
            Civilization = rd["civilization"]?.ToString();
            Race = rd["race"]?.ToString();
            TextRaw = rd["text_raw"]?.ToString();
            TwinImpact = TryBool(rd["twinImpact"]);
            HyperMode = TryBool(rd["hyperMode"]);
            Cost = TryInt(rd["cost"]);
            Power = TryInt(rd["power"]);
        }

        static bool TryBool(object? v)
        {
            if (v is null) return false;
            var s = v.ToString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s)) return false;
            return s is "1" or "true" or "t" or "y" or "yes";
        }
        static int? TryInt(object? v)
        {
            if (v is null) return null;
            if (int.TryParse(v.ToString(), out var i)) return i;
            var s = v.ToString();
            if (string.IsNullOrWhiteSpace(s)) return null;
            var digits = System.Text.RegularExpressions.Regex.Replace(s, "[^0-9-]", "");
            if (int.TryParse(digits, out i)) return i;
            return null;
        }
    }

    public class AstCard
    {
        public int schemaVersion { get; set; }
        public string cardId { get; set; } = "";
        public string name { get; set; } = "";
        public List<string> types { get; set; } = new();
        public int? cost { get; set; }
        public List<string> civilizations { get; set; } = new();
        public int? power { get; set; }
        public List<string> race { get; set; } = new();
        public List<Effect> effects { get; set; } = new();
        public List<string> flags { get; set; } = new();
    }

    public class Effect
    {
        public string kind { get; set; } = "triggered";
        public string id { get; set; } = Guid.NewGuid().ToString("n");
        public string? text { get; set; }
    }

    public class Cli
    {
        public string OutDir { get; private set; } = Path.Combine("cards_ast","generated");
        public string DbPath { get; private set; } = "Duelmasters.db";
        public bool SkipTwinImpact { get; private set; } = false;
        public bool SkipHyperMode { get; private set; } = false;
        public int? Limit { get; private set; } = 200;
        public bool ShowHelp { get; private set; } = false;

        public static string Help => @"DbToAst
Usage:
  gen --out <dir> [--db Duelmasters.db] [--limit N] [--skipTwinImpact] [--skipHyperMode]
";

        public static Cli Parse(string[] args)
        {
            var o = new Cli();
            if (args.Length == 0) { o.ShowHelp = true; return o; }
            if (!string.Equals(args[0], "gen", StringComparison.OrdinalIgnoreCase)) { o.ShowHelp = true; return o; }
            for (int i = 1; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--out":
                        o.OutDir = (i+1<args.Length) ? args[++i] : o.OutDir; break;
                    case "--db":
                        o.DbPath = (i+1<args.Length) ? args[++i] : o.DbPath; break;
                    case "--limit":
                        if (i+1<args.Length && int.TryParse(args[++i], out var n)) o.Limit = n; break;
                    case "--skipTwinImpact":
                        o.SkipTwinImpact = true; break;
                    case "--skipHyperMode":
                        o.SkipHyperMode = true; break;
                    case "-h":
                    case "--help":
                        o.ShowHelp = true; break;
                }
            }
            return o;
        }
    }
}
