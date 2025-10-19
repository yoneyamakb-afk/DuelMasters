using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DMRules.Engine;

static class Program
{
    static int Main(string[] args)
    {
        string inPath = "";
        string inFormat = "json";   // json|ndjson
        string outPath = "";
        string outFormat = "md";    // json|ndjson|md (default md)

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in":
                case "-i":
                    if (i + 1 < args.Length) inPath = args[++i];
                    break;
                case "--in-format":
                    if (i + 1 < args.Length) inFormat = args[++i];
                    break;
                case "--out":
                case "-o":
                    if (i + 1 < args.Length) outPath = args[++i];
                    break;
                case "--out-format":
                case "-f":
                    if (i + 1 < args.Length) outFormat = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
            }
        }

        if (string.IsNullOrWhiteSpace(inPath) || !File.Exists(inPath))
        {
            Console.Error.WriteLine("ERROR: --in <trace file> is required and must exist.");
            PrintHelp();
            return 2;
        }

        var trace = ReadTrace(inPath, inFormat);
        string output = outFormat switch
        {
            "json" => TraceSerializer.ToJson(trace),
            "ndjson" => TraceSerializer.ToNdjson(trace),
            "md" or "markdown" => TraceSerializer.ToMarkdown(trace),
            _ => TraceSerializer.ToMarkdown(trace)
        };

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            File.WriteAllText(outPath, output);
            Console.WriteLine($"Trace converted: {outPath}");
        }
        else
        {
            Console.WriteLine(output);
        }

        return 0;
    }

    static IEnumerable<TraceEntry> ReadTrace(string path, string format)
    {
        if (format.Equals("ndjson", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                yield return JsonSerializer.Deserialize<TraceEntry>(line)!;
            }
        }
        else
        {
            var json = File.ReadAllText(path);
            var arr = JsonSerializer.Deserialize<TraceEntry[]>(json)!;
            foreach (var t in arr) yield return t;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("DMRules.TraceTool - convert trace files");
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/DMRules.TraceTool -- --in artifacts/trace.json --out artifacts/trace.md --out-format md");
        Console.WriteLine("Options:");
        Console.WriteLine("  --in, -i <path>               Input trace file (json or ndjson)");
        Console.WriteLine("  --in-format <json|ndjson>     Input format (default json)");
        Console.WriteLine("  --out, -o <path>              Output file path");
        Console.WriteLine("  --out-format, -f <json|ndjson|md>  Output format (default md)");
    }
}
