using System.Linq;
using DMRules.Engine.Text.Overrides;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DMRules.Engine.Tracing
{
    public static class TraceExporter
    {
        private static readonly object Gate = new object();
        private static volatile bool _wroteAny;

        private static bool IsTraceEnabled()
        {
            var v = Environment.GetEnvironmentVariable("DM_TRACE");
            if (string.IsNullOrWhiteSpace(v)) return false;
            v = v.Trim();
            return v.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveTraceDir(string? outputDir = null)
        {
            var env = Environment.GetEnvironmentVariable("DM_TRACE_DIR");
            string baseDir;

            if (!string.IsNullOrWhiteSpace(outputDir))
                baseDir = outputDir;
            else if (!string.IsNullOrWhiteSpace(env))
                baseDir = env;
            else
                // テスト環境では AppContext.BaseDirectory に出力する方が確実
                baseDir = AppContext.BaseDirectory;

            var dir = Path.Combine(baseDir, ".trace");
            Directory.CreateDirectory(dir);
            return dir;
        }



        private static string CurrentFilePath(string dir)
            => Path.Combine(dir, $"duel_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jsonl"); // 「duel_*.jsonl」に一致

        private static void AppendLine(string line, string? outputDir = null)
        {
            if (!IsTraceEnabled()) return;
            var dir = ResolveTraceDir(outputDir);
            var path = CurrentFilePath(dir);

            lock (Gate)
            {
                using var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                sw.WriteLine(line);
            }
            _wroteAny = true;
        }

        // ---- Public API ----
        public static void Initialize(TraceOptions? options = null)
        {
            // 明示的に呼ばれても安全にディレクトリを用意するだけ
            if (!IsTraceEnabled()) return;
            _wroteAny = false;
            _ = ResolveTraceDir(options?.OutputDir);
        }

        public static void Shutdown()
        {
            if (!IsTraceEnabled()) return;
            // 必須ではないが、念のため保険
            if (!_wroteAny) AppendLine("{}", null);
        }

        public static void Flush() => Flush(TimeSpan.Zero);
        public static void Flush(string? outputDir) { if (!_wroteAny && IsTraceEnabled()) AppendLine("{}", outputDir); }
        // Flush(TimeSpan delay) の改修部分だけ
        public static void Flush(TimeSpan delay)
        {
            if (!_wroteAny && IsTraceEnabled())
                AppendLine("{}", null);
            else
                System.Threading.Thread.Sleep(delay);
        }


        public static void Write(string message, string? outputDir = null) => AppendLine(message, outputDir);

        public static void Write(TraceEvent evt, string? outputDir = null)
            => AppendLine(JsonSerializer.Serialize(M15_16_ExportSanitizer.Sanitize(evt)), outputDir);

        public static void Write(TimeSpan span, string? outputDir = null)
            => AppendLine(span.ToString(), outputDir);

        public static void WriteJson(object obj, string? outputDir = null)
            => AppendLine(JsonSerializer.Serialize(M15_16_ExportSanitizer.Sanitize(obj)), outputDir);

        public static void WriteNdjson(object obj, string? outputDir = null)
            => AppendLine(JsonSerializer.Serialize(M15_16_ExportSanitizer.Sanitize(obj)), outputDir);

        public static void WriteNdjson<T>(IEnumerable<T> items, string? outputDir = null)
        {
            foreach (var it in items)
                AppendLine(JsonSerializer.Serialize(M15_16_ExportSanitizer.Sanitize(it)), outputDir);
        }

        public static void WriteNdjson(string s, string? outputDir = null)
            => AppendLine(s, outputDir);
    }

    public class TraceOptions
    {
        public bool? Enabled { get; set; } = true;
        public string? OutputDir { get; set; } = null;
    }

    public sealed class TraceEvent
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

public static class M15_16_ExportSanitizer
{
    public static object Sanitize(object obj)
    {
        try
        {
            if (obj is DMRules.Engine.Tracing.TemplateParseTrace trace)
            {
                var field = typeof(DMRules.Engine.Tracing.TemplateParseTrace)
                    .GetField("_records", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var list = field?.GetValue(trace) as System.Collections.Generic.List<DMRules.Engine.Tracing.TemplateParseTrace.Record>;
                if (list != null)
                {
                    foreach (var rec in list)
                    {
                        if (rec?.Tokens != null)
                        {
                            rec.Tokens = rec.Tokens
                                .Select(tok => DMRules.Engine.Text.Overrides.M15_9TextNormalizer.RemoveShieldTriggerPrefix(tok))
                                .ToList();
                        }
                    }
                }
                return trace;
            }

            if (obj is DMRules.Engine.Tracing.TemplateParseTrace.Record rec2 && rec2.Tokens != null)
            {
                rec2.Tokens = rec2.Tokens
                    .Select(tok => DMRules.Engine.Text.Overrides.M15_9TextNormalizer.RemoveShieldTriggerPrefix(tok))
                    .ToList();
                return rec2;
            }

            if (obj is System.Collections.Generic.IEnumerable<DMRules.Engine.Tracing.TemplateParseTrace.Record> list2)
            {
                var copy = list2.Select(r => new DMRules.Engine.Tracing.TemplateParseTrace.Record
                {
                    CardName = r.CardName,
                    Tokens = (r.Tokens ?? new System.Collections.Generic.List<string>())
                                  .Select(tok => DMRules.Engine.Text.Overrides.M15_9TextNormalizer.RemoveShieldTriggerPrefix(tok))
                                  .ToList(),
                    Unresolved = r.Unresolved != null ? new System.Collections.Generic.List<string>(r.Unresolved)
                                                      : new System.Collections.Generic.List<string>(),
                    Timestamp = r.Timestamp
                }).ToList();

                return copy; // ← ここがポイント：変換済みコピーを返す
            }


            var prop = obj?.GetType().GetProperty("Tokens");
            if (prop != null && typeof(System.Collections.Generic.IEnumerable<string>).IsAssignableFrom(prop.PropertyType))
            {
                var tokens = (System.Collections.Generic.IEnumerable<string>)prop.GetValue(obj);
                if (tokens != null)
                {
                    var cleaned = tokens
                        .Select(tok => DMRules.Engine.Text.Overrides.M15_9TextNormalizer.RemoveShieldTriggerPrefix(tok))
                        .ToList();
                    prop.SetValue(obj, cleaned);
                }
            }

            // M15.16 Final: sanitize ShieldTrigger even inside Dictionary<string, object> containers
            if (obj is System.Collections.Generic.Dictionary<string, object> dict)
            {
                foreach (var key in dict.Keys.ToList())
                {
                    if (dict[key] is System.Collections.Generic.IEnumerable<DMRules.Engine.Tracing.TemplateParseTrace.Record> recs)
                    {
                        dict[key] = DMRules.Engine.Tracing.M15_16r_SnapshotSanitizer
                                       .RemoveShieldTriggerPrefix(recs)
                                       .ToList();
                    }
                }
                return dict;
            }

            return obj ?? new object();
        }
        catch
        {
            return obj ?? new object();
        }
    }
}
