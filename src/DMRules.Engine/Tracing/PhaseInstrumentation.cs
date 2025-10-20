using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DMRules.Engine.Tracing
{
    /// <summary>
    /// フェイズ遷移らしきメソッドを反射で検出し、署名を JSONL に出力（Invoke はしない安全版）。
    /// 出力: phase_change_start / phase_change_end（署名ベースの疑似イベント）
    /// </summary>
    public static class PhaseInstrumentation
    {
        public static bool TryProbeAndTraceOnce()
        {
            var asm = typeof(PhaseInstrumentation).Assembly;
            var types = asm.GetTypes();
            var candidates = types.Where(t =>
                t.Name.Contains("Phase", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Turn", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Duel", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Game", StringComparison.OrdinalIgnoreCase));

            bool any = false;

            foreach (var t in candidates)
            {
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (LooksLikePhaseStart(m.Name))
                    {
                        any = true;
                        Emit("phase_change_start", t, m, null);
                    }
                    else if (LooksLikePhaseEnd(m.Name))
                    {
                        any = true;
                        Emit("phase_change_end", t, m, null);
                    }
                }
            }

            return any;
        }

        private static bool LooksLikePhaseStart(string name)
        {
            name = name.ToLowerInvariant();
            return (name.Contains("enter") || name.Contains("begin") || name.Contains("start") || name.Contains("execute") || name.Contains("next") || name.Contains("advance"))
                && (name.Contains("phase") || name.Contains("main") || name.Contains("battle") || name.Contains("end"));
        }

        private static bool LooksLikePhaseEnd(string name)
        {
            name = name.ToLowerInvariant();
            return (name.Contains("leave") || name.Contains("end") || name.Contains("exit") || name.Contains("complete") || name.Contains("finish"))
                && (name.Contains("phase") || name.Contains("main") || name.Contains("battle") || name.Contains("end"));
        }

        private static void Emit(string action, Type t, MethodInfo m, Dictionary<string, object?>? details)
        {
            try
            {
                var d = details ?? new();
                d["declaringType"] = t.FullName ?? t.Name;
                d["method"] = m.Name;
                d["params"] = string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name));
                EngineTrace.Event(action, details: d);
            }
            catch { }
        }
    }
}
