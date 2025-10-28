// src/DMRules.Engine/Tracing/TraceExporter.cs
// Unified, single-source TraceExporter (TraceEvent fixed for EngineTrace object initializers)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DMRules.Engine.Tracing
{
    public static class TraceExporter
    {
        private static readonly object _gate = new object();

        private static string ResolveTraceDir(string? outputDir = null)
        {
            var env = Environment.GetEnvironmentVariable("DM_TRACE_DIR");
            var baseDir = outputDir ?? (!string.IsNullOrWhiteSpace(env) ? env : Environment.CurrentDirectory);
            return Path.Combine(baseDir, ".trace");
        }

        private static void Append(string text, string? outputDir = null)
        {
            // DM_TRACE=1 の時のみ有効化
            var traceEnabled = Environment.GetEnvironmentVariable("DM_TRACE");
            if (traceEnabled != "1")
                return;

            var dir = ResolveTraceDir(outputDir);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "trace.jsonl");
            lock (_gate)
            {
                using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var sw = new StreamWriter(fs, new UTF8Encoding(false));
                sw.WriteLine(text);
            }
        }


        // ---- Public API surface (tests/CLI/DemoRun expect these) ----

        public static void Initialize(TraceOptions? options = null)
        {
            var dir = ResolveTraceDir(options?.OutputDir);
            Directory.CreateDirectory(dir);
        }

        public static void Shutdown() { /* no-op */ }

        public static void Flush()
        {
            var dir = ResolveTraceDir();
            Directory.CreateDirectory(dir);
        }

        public static void Flush(string? outputDir)
        {
            var dir = ResolveTraceDir(outputDir);
            Directory.CreateDirectory(dir);
        }

        public static void Flush(TimeSpan delay)
        {
            // Legacy overload used by tests; ensure directory exists.
            Flush();
        }

        public static void Write(string message, string? outputDir = null)
            => Append(message, outputDir);

        public static void Write(TraceEvent evt, string? outputDir = null)
        {
            // JSONL形式でTraceEventを出力
            var json = JsonSerializer.Serialize(evt);
            Append(json, outputDir);
        }


        public static void Write(TimeSpan span, string? outputDir = null)
            => Append(span.ToString(), outputDir);

        public static void WriteJson(object obj, string? outputDir = null)
            => Append(JsonSerializer.Serialize(obj), outputDir);

        public static void WriteNdjson(object obj, string? outputDir = null)
            => Append(JsonSerializer.Serialize(obj), outputDir);

        public static void WriteNdjson<T>(IEnumerable<T> items, string? outputDir = null)
        {
            foreach (var it in items)
                Append(JsonSerializer.Serialize(it), outputDir);
        }

        public static void WriteNdjson(string s, string? outputDir = null)
            => Append(s, outputDir);
    }

    // Options bag referenced by tests
    public class TraceOptions
    {
        public bool? Enabled { get; set; } = true;
        public string? OutputDir { get; set; } = null;
    }

    // Stubs referenced by tests (kept minimal)
    public static class TriggerInstrumentation
    {
        public static void Start() { }
        public static void End() { }
        public static void TryProbeAndTraceOnce() { }
    }

    public static class PhaseInstrumentation
    {
        public static void Start() { }
        public static void End() { }
        public static void TryProbeAndTraceOnce() { }
    }

    // ---- Legacy compatibility class for EngineTrace references ----
    // EngineTrace uses object initializers: new TraceEvent { Phase=..., Action=..., ... }
    // Provide settable instance properties instead of static fields to satisfy CS1914.
    public sealed class TraceEvent
    {
        public string? Phase { get; set; }
        public string? Action { get; set; }
        public string? Player { get; set; }
        public string? Card { get; set; }
        public int?    StackSize { get; set; }
        public string? StateHash { get; set; }
        public Dictionary<string, object?>? Details { get; set; }

        public override string ToString()
            => $"Phase={Phase}, Action={Action}, Player={Player}, Card={Card}, StackSize={StackSize}, StateHash={StateHash}";
    }
}
