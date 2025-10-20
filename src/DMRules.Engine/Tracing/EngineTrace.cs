using System;
using System.Collections.Generic;
using DMRules.Trace;

namespace DMRules.Engine.Tracing
{
    /// <summary>Engine側から呼べるトレース補助。外部依存なし。</summary>
    public static class EngineTrace
    {
        public static void Event(string action, string? phase = null, string? player = null, string? card = null, int? stackSize = null, string? state = null, Dictionary<string, object?>? details = null)
        {
            try
            {
                global::DMRules.Trace.TraceExporter.Write(new global::DMRules.Trace.TraceEvent
                {
                    Phase = phase,
                    Action = action,
                    Player = player,
                    Card = card,
                    StackSize = stackSize,
                    StateHash = state,
                    Details = details
                });
            }
            catch { /* no-throw */ }
        }

        public static void SbaStart(Dictionary<string, object?>? details = null) => Event("SBA_resolve_start", details: details);
        public static void SbaEnd(Dictionary<string, object?>? details = null) => Event("SBA_resolve_end", details: details);
        public static void Destroy(string? card, string? reason = null) => Event("destroy", card: card, details: new() { ["reason"] = reason ?? "SBA" });
        public static void Move(string? card, string from, string to) => Event("move", card: card, details: new() { ["from"] = from, ["to"] = to });
    }
}
