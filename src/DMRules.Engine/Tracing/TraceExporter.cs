// Final unified TraceExporter (DMRules.Engine.Tracing)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DMRules.Engine.Tracing
{
    public static partial class TraceExporter
    {
        private static readonly object _gate = new object();

        public static void Append(string message, string? outputDir = null)
        {
            try
            {
                var dir = outputDir ?? Path.Combine(AppContext.BaseDirectory, "artifacts");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "trace.log");
                var line = $"{DateTime.UtcNow:O} | {message}";
                lock (_gate)
                {
                    File.AppendAllLines(path, new[] { line }, new UTF8Encoding(false));
                }
            }
            catch
            {
                // tracing must not crash the engine
            }
        }

        // Unified Write overloads
        public static void Write(string message, string? outputDir = null)
            => Append(message, outputDir);

        public static void Write(Dictionary<string, object?> dict, string? outputDir = null)
            => Append(JsonSerializer.Serialize(dict), outputDir);

        public static void Write(TraceEvent ev, string? outputDir = null)
            => Append(JsonSerializer.Serialize(ev), outputDir);
    }

    public class TraceEvent
    {
        public string? Phase { get; set; }
        public string? Action { get; set; }
        public string? Player { get; set; }
        public string? Card { get; set; }
        public int? StackSize { get; set; }
        public string? StateHash { get; set; }
        public Dictionary<string, object?>? Details { get; set; }
    }
}
