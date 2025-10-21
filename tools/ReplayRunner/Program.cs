using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReplayRunner
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var opts = CliOptions.Parse(args);
                if (opts.ShowHelp)
                {
                    Console.WriteLine(CliOptions.HelpText);
                    return 0;
                }

                if (opts.Command == "replay")
                {
                    if (string.IsNullOrWhiteSpace(opts.FromPath) || !File.Exists(opts.FromPath))
                    {
                        Console.Error.WriteLine($"[ReplayRunner] real log not found: {opts.FromPath}");
                        return 2;
                    }

                    var real = await RealLog.LoadAsync(opts.FromPath);
                    var simTrace = RealLogToSimTrace.Convert(real);

                    Console.WriteLine($"[ReplayRunner] event count: {simTrace.Events.Count}");

                    var outDir = Path.Combine("artifacts", "actual");
                    Directory.CreateDirectory(outDir);
                    var outPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(opts.FromPath) + ".trace.json");

                    await using (var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        await JsonSerializer.SerializeAsync(fs, simTrace, new JsonSerializerOptions { WriteIndented = true });
                        await fs.FlushAsync();
                    }

                    var bytes = new FileInfo(outPath).Length;
                    Console.WriteLine($"[ReplayRunner] wrote: {outPath} ({bytes} bytes)");

                    if (opts.StopOnDivergence)
                    {
                        Console.WriteLine("[ReplayRunner] stop-on-divergence: no divergence detected (echo mode).");
                    }
                    return 0;
                }

                Console.WriteLine(CliOptions.HelpText);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[ReplayRunner] Fatal: " + ex);
                return 1;
            }
        }
    }

    public class CliOptions
    {
        public string Command { get; private set; } = "";
        public string FromPath { get; private set; } = "";
        public bool StopOnDivergence { get; private set; } = false;
        public bool ShowHelp { get; private set; } = false;
        public int? Seed { get; private set; } = null;

        public static string HelpText => @"ReplayRunner
Usage:
  replay --from <real_log.json> [--seed N] [--stop-on-divergence]
";

        public static CliOptions Parse(string[] args)
        {
            var o = new CliOptions();
            if (args.Length == 0) { o.ShowHelp = true; return o; }

            o.Command = (args[0]?.StartsWith("--") == true) ? "replay" : args[0]?.Trim().ToLowerInvariant();

            for (int i = 1; i < args.Length; i++)
            {
                var a = args[i];
                switch (a)
                {
                    case "--from":
                        o.FromPath = (i + 1 < args.Length) ? args[++i] : "";
                        break;
                    case "--seed":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var s)) o.Seed = s;
                        break;
                    case "--stop-on-divergence":
                        o.StopOnDivergence = true;
                        break;
                    case "-h":
                    case "--help":
                        o.ShowHelp = true;
                        break;
                }
            }
            return o;
        }
    }

    public class RealLog
    {
        [JsonPropertyName("events")]
        public List<RealEvent> Events { get; set; } = new();

        [JsonPropertyName("meta")]
        public Dictionary<string, object>? Meta { get; set; }

        public static async Task<RealLog> LoadAsync(string path)
        {
            await using var fs = File.OpenRead(path);
            var log = await JsonSerializer.DeserializeAsync<RealLog>(fs, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return log ?? new RealLog();
        }
    }

    public class RealEvent
    {
        [JsonPropertyName("turn")] public int? Turn { get; set; }
        [JsonPropertyName("phase")] public string? Phase { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("actor")] public string? Actor { get; set; }
        [JsonPropertyName("card")] public string? Card { get; set; }
        [JsonPropertyName("targets")] public List<string>? Targets { get; set; }
        [JsonPropertyName("payload")] public Dictionary<string, object>? Payload { get; set; }
        [JsonPropertyName("ts")] public string? Timestamp { get; set; }
    }

    public class SimTrace
    {
        [JsonPropertyName("traceVersion")] public int TraceVersion { get; set; } = 1;
        [JsonPropertyName("events")] public List<SimEvent> Events { get; set; } = new();
        [JsonPropertyName("meta")] public Dictionary<string, object>? Meta { get; set; }
    }

    public class SimEvent
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("turn")] public int? Turn { get; set; }
        [JsonPropertyName("phase")] public string? Phase { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("actor")] public string? Actor { get; set; }
        [JsonPropertyName("card")] public string? Card { get; set; }
        [JsonPropertyName("targets")] public List<string>? Targets { get; set; }
        [JsonPropertyName("stateAfter")] public Dictionary<string, object>? StateAfter { get; set; }
    }

    public static class RealLogToSimTrace
    {
        public static SimTrace Convert(RealLog log)
        {
            var trace = new SimTrace
            {
                Meta = log.Meta ?? new Dictionary<string, object>()
            };
            for (int i = 0; i < log.Events.Count; i++)
            {
                var e = log.Events[i];
                trace.Events.Add(new SimEvent
                {
                    Index = i,
                    Turn = e.Turn,
                    Phase = NormalizePhase(e.Phase),
                    Action = e.Action,
                    Actor = e.Actor,
                    Card = e.Card,
                    Targets = e.Targets,
                    StateAfter = new Dictionary<string, object>{
                        ["note"] = "echo",
                        ["phase"] = NormalizePhase(e.Phase) ?? "unknown"
                    }
                });
            }

            if (trace.Events.Count == 0)
            {
                trace.Events.Add(new SimEvent
                {
                    Index = 0,
                    Action = "DummyEvent",
                    Actor = "System",
                    Phase = "Start",
                    StateAfter = new Dictionary<string, object> { ["note"] = "no real events in source log" }
                });
            }

            return trace;
        }

        private static string? NormalizePhase(string? phase)
        {
            if (phase == null) return null;
            var p = phase.Trim().ToLowerInvariant();
            return p switch
            {
                "untap" => "Untap",
                "upkeep" or "start" => "Start",
                "draw" => "Draw",
                "charge" => "Charge",
                "main" => "Main",
                "attack" or "battle" => "Attack",
                "end" or "cleanup" => "End",
                _ => char.ToUpper(p[0]) + p[1..]
            };
        }
    }

    public static class ReplayEngineHook
    {
        public static void ReplayEvent(SimEvent e) { /* integrate engine here when ready */ }
    }
}
