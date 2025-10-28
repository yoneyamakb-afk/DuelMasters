// tests/DMRules.Tests/TraceCompat.cs
// Force-compiled shim so tests using `using DMRules.Trace;` resolve TimeSpan overloads.
using System;
using System.Collections.Generic;
using System.Text.Json;
using DMRules.Engine.Tracing;

namespace DMRules.Trace
{
    public static class TraceExporter
    {
        public static void Write(string message, string? outputDir = null)
            => DMRules.Engine.Tracing.TraceExporter.Write(message, outputDir);

        public static void Write(Dictionary<string, object?> dict, string? outputDir = null)
            => DMRules.Engine.Tracing.TraceExporter.Write(dict, outputDir);

        // *** This is the critical path: eliminate CS1503 by forwarding TimeSpan ***
        public static void Write(TimeSpan span, string? outputDir = null)
            => DMRules.Engine.Tracing.TraceExporter.Write(span, outputDir);

        // Serialize TraceEvent to string to match engine Write(string)
        public static void Write(TraceEvent ev, string? outputDir = null)
            => DMRules.Engine.Tracing.TraceExporter.Write(JsonSerializer.Serialize(ev), outputDir);
    }

    // Minimal shape used by tests
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
