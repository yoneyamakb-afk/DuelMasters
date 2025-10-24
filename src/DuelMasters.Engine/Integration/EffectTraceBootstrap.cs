
using System;
using System.IO;
using DMRules.Effects.Trace;

namespace DuelMasters.Engine.Integration
{
    /// <summary>
    /// M12.5: トレース出力の簡易初期化ヘルパ。
    /// 例: EffectTraceBootstrap.EnableJsonl(); // .\artifacts\effects_trace.jsonl に出力
    /// </summary>
    public static class EffectTraceBootstrap
    {
        public static string EnableJsonl(string? path = null, bool append = true)
        {
            var p = path ?? Path.Combine(Environment.CurrentDirectory, "artifacts", "effects_trace.jsonl");
            EffectTrace.EnableJsonl(p, append);
            return p;
        }

        public static void Disable() => EffectTrace.Reset();
    }
}
