// src/DMRules.Engine/Tracing/TraceExporter.Compat.Final.cs
using System;

namespace DMRules.Engine.Tracing
{
    public static partial class TraceExporter
    {
        public static void Initialize() { }
        public static void Initialize(TraceOptions? options) { }
        public static void Flush() { }
        public static void Flush(string? outputDir) { }
        public static void Shutdown() { }
        public static void Write(TimeSpan span, string? outputDir = null)
            => Append(span.ToString(), outputDir);

        public static void Flush(TimeSpan delay)
        {
            try
            {
                // 実際の出力処理を呼び出すか、必要ならSleep代わりにする
                Flush();
            }
            catch { }
        }

    }

    public class TraceOptions
    {
        public bool? Enabled { get; set; } = true;
        public string? OutputDir { get; set; } = null;
    }

    public static class PhaseInstrumentation
    {
        public static void Start() { }
        public static void End() { }
        public static void TryProbeAndTraceOnce() { }
    }

    public static class TriggerInstrumentation
    {
        public static void Start() { }
        public static void End() { }
        public static void TryProbeAndTraceOnce() { }
    }
}
