using System;
using System.Linq;
using System.Reflection;
using DMRules.Engine.Tracing;
using Xunit;

namespace DMRules.Tests
{
    /// <summary>
    /// ReflectionベースのSBAトレース・プローブ。
    /// エンジンの公開APIに依存せず、名前ヒューリスティクスで見つかったメソッドを呼び出し、前後状態を記録する。
    /// 条件に合致しない場合はスキップ（成功扱い）するので安全。
    /// </summary>
    public sealed class SBATraceProbe
    {
        [Fact(DisplayName = "SBA Trace Probe (reflection)")]
        public void ProbeSbaViaReflection()
        {
            // DM_TRACEが有効でない環境では何もしない
            var enabled = Environment.GetEnvironmentVariable("DM_TRACE");
            if (string.IsNullOrWhiteSpace(enabled)) return;

            // 1) エンジンアセンブリを得る
            var engineAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name is "DMRules.Engine" or "DuelMasters.Engine" or "DMRules");
            if (engineAsm == null)
            {
                TraceExporter.Write(new TraceEvent { Action = "sba_probe_skipped", Details = new() { ["reason"] = "engine_assembly_not_found" } });
                return;
            }

            // 2) "StateBased" / "SBA" を含む型やメソッドを探索
            var types = engineAsm.GetTypes();
            var candidateTypes = types.Where(t =>
                    t.Name.Contains("StateBased", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("SBA", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("GameState", StringComparison.OrdinalIgnoreCase) ).ToArray();

            MethodInfo? sbaMethod = null;
            object? instance = null;

            foreach (var t in candidateTypes)
            {
                // 静的メソッド候補
                sbaMethod = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.Name.Contains("Apply", StringComparison.OrdinalIgnoreCase) &&
                        (m.Name.Contains("State", StringComparison.OrdinalIgnoreCase) || m.Name.Contains("SBA", StringComparison.OrdinalIgnoreCase)));
                if (sbaMethod != null) break;

                // インスタンスメソッド候補
                sbaMethod = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                        m.Name.Contains("Apply", StringComparison.OrdinalIgnoreCase) &&
                        (m.Name.Contains("State", StringComparison.OrdinalIgnoreCase) || m.Name.Contains("SBA", StringComparison.OrdinalIgnoreCase)));
                if (sbaMethod != null)
                {
                    try { instance = Activator.CreateInstance(t); } catch { instance = null; }
                    break;
                }
            }

            if (sbaMethod == null)
            {
                TraceExporter.Write(new TraceEvent { Action = "sba_probe_skipped", Details = new() { ["reason"] = "method_not_found" } });
                return;
            }

            // 3) ダミー引数を生成（足りない分は default）
            var ps = sbaMethod.GetParameters();
            var args = ps.Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();

            // 4) 実行前の簡易情報
            TraceExporter.Write(new TraceEvent
            {
                Action = "sba_probe_before",
                Details = new()
                {
                    ["declaringType"] = sbaMethod.DeclaringType?.FullName ?? "?",
                    ["method"] = sbaMethod.Name,
                    ["paramCount"] = ps.Length
                }
            });

            // 5) 呼び出し（例外は握りつぶして安全に進む）
            try { _ = sbaMethod.Invoke(instance, args); }
            catch (Exception ex)
            {
                TraceExporter.Write(new TraceEvent
                {
                    Action = "sba_probe_invoke_error",
                    Details = new() { ["exception"] = ex.GetType().Name, ["msg"] = ex.Message }
                });
            }

            // 6) 実行後
            TraceExporter.Write(new TraceEvent
            {
                Action = "sba_probe_after",
                Details = new()
                {
                    ["declaringType"] = sbaMethod.DeclaringType?.FullName ?? "?",
                    ["method"] = sbaMethod.Name
                }
            });

            TraceExporter.Flush(TimeSpan.FromSeconds(2));
        }
    }
}
