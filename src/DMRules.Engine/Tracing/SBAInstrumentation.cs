using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace DMRules.Engine.Tracing
{
    /// <summary>
    /// 既存エンジンのSBA実装に「軽量フック」するための反射ベースの呼び出し。
    /// エンジン本体コードに一切改変を加えず、SBA前後でEngineTraceを発行する。
    /// 見つからない場合は何もしない（安全無害）。
    /// </summary>
    public static class SBAInstrumentation
    {
        public static bool TryProbeAndRun() => TryProbeAndRunCore(out _);

        public static bool TryProbeAndRunCore(out string? methodInfoSummary)
        {
            methodInfoSummary = null;

            var engineAsm = typeof(SBAInstrumentation).Assembly;
            var types = engineAsm.GetTypes();
            var candidates = types.Where(t =>
                t.Name.Contains("StateBased", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("SBA", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("GameState", StringComparison.OrdinalIgnoreCase));

            MethodInfo? sbaMethod = null;
            object? instance = null;

            foreach (var t in candidates)
            {
                sbaMethod = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => NameLooksLikeSBA(m.Name));
                if (sbaMethod != null) { instance = null; break; }

                sbaMethod = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => NameLooksLikeSBA(m.Name));
                if (sbaMethod != null)
                {
                    try { instance = Activator.CreateInstance(t); } catch { instance = null; }
                    break;
                }
            }

            if (sbaMethod == null) return false;

            methodInfoSummary = $"{sbaMethod.DeclaringType?.FullName}.{sbaMethod.Name}({string.Join(",", sbaMethod.GetParameters().Select(p=>p.ParameterType.Name))})";

            // ダミー引数生成（default埋め）
            var ps = sbaMethod.GetParameters();
            var args = ps.Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();

            try
            {
                EngineTrace.SbaStart(new Dictionary<string, object?> { ["method"] = methodInfoSummary! });
                _ = sbaMethod.Invoke(instance, args);
                EngineTrace.SbaEnd(new Dictionary<string, object?> { ["method"] = methodInfoSummary! });
            }
            catch (Exception ex)
            {
                EngineTrace.Event("SBA_invoke_error", details: new() { ["method"] = methodInfoSummary!, ["ex"] = ex.GetType().Name, ["msg"] = ex.Message });
            }

            return true;
        }

        private static bool NameLooksLikeSBA(string name)
        {
            name = name.ToLowerInvariant();
            return (name.Contains("apply") || name.Contains("resolve") || name.Contains("force")) &&
                   (name.Contains("state") || name.Contains("sba"));
        }
    }
}
