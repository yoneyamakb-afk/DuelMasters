// src/DMRules.Engine/Tracing/TraceExporter.JsonCompat.cs
// Add JSON helpers expected by DemoRun.
using System.Text.Json;

namespace DMRules.Engine.Tracing
{
    public static partial class TraceExporter
    {
        public static void WriteJson(object obj, string? outputDir = null)
            => Append(JsonSerializer.Serialize(obj), outputDir);

        public static void WriteNdjson(object obj, string? outputDir = null)
            => Append(JsonSerializer.Serialize(obj), outputDir);
    }
}
