using System;
using System.Linq;
using System.Text.Json;

namespace DMRules.Tests.M23
{
    /// <summary>
    /// M23: スナップショット形式の統一基盤。
    /// - 旧式(legacy)フォーマット検出
    /// - JSONの正規化（将来的に変換もここに集約）
    /// </summary>
    public static class SnapshotConventions
    {
        public static bool IsLegacyGolden(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            var s = Trim(json);
            if (!s.StartsWith("[")) return false;

            try
            {
                using var doc = JsonDocument.Parse(s);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return false;

                var first = root[0];
                if (first.ValueKind != JsonValueKind.Object) return false;

                bool hasRank = first.EnumerateObject()
                    .Any(p => string.Equals(p.Name, "rank", StringComparison.OrdinalIgnoreCase));
                bool hasPhrase = first.EnumerateObject()
                    .Any(p => string.Equals(p.Name, "phrase", StringComparison.OrdinalIgnoreCase));

                return hasRank && hasPhrase;
            }
            catch
            {
                return false;
            }
        }

        public static string Canonicalize(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return CanonicalizeElement(doc.RootElement);
        }

        private static string CanonicalizeElement(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    var props = el.EnumerateObject()
                        .OrderBy(p => p.Name, StringComparer.Ordinal)
                        .Select(p => $"\"{Escape(p.Name)}\":{CanonicalizeElement(p.Value)}");
                    return "{" + string.Join(",", props) + "}";

                case JsonValueKind.Array:
                    var items = el.EnumerateArray().Select(CanonicalizeElement);
                    return "[" + string.Join(",", items) + "]";

                case JsonValueKind.String:
                    return $"\"{Escape(el.GetString() ?? string.Empty)}\"";

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    return el.GetRawText();

                default:
                    return "\"\"";
            }
        }

        private static string Escape(string s)
        {
            // バックスラッシュとダブルクォーテーションのエスケープ
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Trim(string s)
        {
            return s.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        }
    }
}
