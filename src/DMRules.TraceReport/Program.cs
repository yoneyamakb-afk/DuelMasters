using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DMRules.Engine;

namespace DMRules.TraceReport
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0) { PrintHelp(); return 1; }

            string? inPath = null;
            string inFormat = "json"; // json|ndjson
            string outPath = "artifacts/trace_report.md";
            string outFormat = "md";  // md|json

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--in": case "-i":
                        if (i + 1 < args.Length) inPath = args[++i];
                        break;
                    case "--in-format":
                        if (i + 1 < args.Length) inFormat = args[++i];
                        break;
                    case "--out": case "-o":
                        if (i + 1 < args.Length) outPath = args[++i];
                        break;
                    case "--out-format": case "-f":
                        if (i + 1 < args.Length) outFormat = args[++i];
                        break;
                    case "--help": case "-h":
                        PrintHelp(); return 0;
                }
            }

            if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
            {
                Console.Error.WriteLine("ERROR: --in <trace.(json|ndjson)> is required.");
                return 2;
            }

            var trace = ReadTrace(inPath!, inFormat);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

            if (outFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var summary = trace.GroupBy(t => t.Kind)
                                   .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
                File.WriteAllText(outPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"Wrote JSON summary: {outPath}");
                return 0;
            }
            else
            {
                var md = BuildMarkdown(trace);
                File.WriteAllText(outPath, md);
                Console.WriteLine($"Wrote Markdown report: {outPath}");
                return 0;
            }
        }

        private static List<TraceEntry> ReadTrace(string path, string format)
        {
            if (format.Equals("ndjson", StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<TraceEntry>();
                foreach (var line in File.ReadLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    list.Add(JsonSerializer.Deserialize<TraceEntry>(line)!);
                }
                return list;
            }
            else
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<TraceEntry>>(json)!;
            }
        }

        private static string BuildMarkdown(IReadOnlyList<TraceEntry> trace)
        {
            // Markdownテーブル安全化
            static string Esc(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                return s
                    .Replace("\\", "\\\\")    // バックスラッシュを1段だけエスケープ
                    .Replace("|", "\\|")       // テーブル区切り文字を保護
                    .Replace("\r", "")
                    .Replace("\n", "<br/>");   // 改行→HTML改行タグ
            }

            var sb = new StringBuilder();
            sb.AppendLine("# Trace Summary");
            sb.AppendLine();
            sb.AppendLine("| Kind | Count |");
            sb.AppendLine("|------|------:|");
            foreach (var g in trace.GroupBy(t => t.Kind)
                                   .OrderByDescending(g => g.Count())
                                   .ThenBy(g => g.Key, StringComparer.Ordinal))
                sb.AppendLine($"| {Esc(g.Key)} | {g.Count()} |");

            sb.AppendLine();
            sb.AppendLine("# Trace Details");
            sb.AppendLine();
            sb.AppendLine("| # | Kind | Detail |");
            sb.AppendLine("|---:|------|--------|");
            for (int i = 0; i < trace.Count; i++)
                sb.AppendLine($"| {trace[i].Ordinal} | {Esc(trace[i].Kind)} | {Esc(trace[i].Detail)} |");

            return sb.ToString();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("TraceReport - summarize a trace file");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project src/DMRules.TraceReport -- --in artifacts/trace.json --out artifacts/trace_report.md");
            Console.WriteLine("Options:");
            Console.WriteLine("  --in, -i <path>            Input trace (json or ndjson)");
            Console.WriteLine("  --in-format <json|ndjson>  Input format (default json)");
            Console.WriteLine("  --out, -o <path>           Output file (default artifacts/trace_report.md)");
            Console.WriteLine("  --out-format, -f <md|json> Output format (md: markdown table, json: summary counts)");
        }
    }
}
