using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DMRules.Engine.Tracing
{
    /// <summary>
    /// 反射ベースで Trigger 登録/解決らしきメソッドを検出して前後でトレースを出力します。
    /// 見つからなければ何もしません（安全）。
    /// </summary>
    public static class TriggerInstrumentation
    {
        public static bool TryProbeAndTraceOnce()
        {
            var asm = typeof(TriggerInstrumentation).Assembly;
            var types = asm.GetTypes();
            var candidates = types.Where(t =>
                t.Name.Contains("Trigger", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Resolver", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Queue", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("APNAP", StringComparison.OrdinalIgnoreCase));

            bool any = false;

            foreach (var t in candidates)
            {
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    // register/enqueue 系
                    if (LooksLikeRegister(m.Name))
                    {
                        any = true;
                        Emit("trigger_registered", t, m, null);
                        // Invokeは副作用の恐れがあるため行わない
                    }
                    // resolve/dequeue 系（APNAP含む）
                    else if (LooksLikeResolve(m.Name))
                    {
                        any = true;
                        Emit("trigger_resolving", t, m, null);
                        Emit("trigger_resolved", t, m, null);
                    }
                    // apnap 系キーワード
                    else if (m.Name.IndexOf("APNAP", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        any = true;
                        Emit("apnap_step", t, m, null);
                    }
                }
            }

            return any;
        }

        private static bool LooksLikeRegister(string name)
        {
            name = name.ToLowerInvariant();
            return name.Contains("register") || name.Contains("enqueue") || name.Contains("addtrigger") || name.Contains("schedule");
        }

        private static bool LooksLikeResolve(string name)
        {
            name = name.ToLowerInvariant();
            return name.Contains("resolve") || name.Contains("dequeue") || name.Contains("process") || name.Contains("execute");
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
