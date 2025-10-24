
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DMRules.Effects
{
    public static class EffectTextNormalizer
    {
        private static readonly Dictionary<char, int> KanjiDigits = new()
        {
            ['零'] = 0, ['〇'] = 0, ['一'] = 1, ['二'] = 2, ['三'] = 3, ['四'] = 4,
            ['五'] = 5, ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9, ['十'] = 10,
        };

        public static string? NormalizeToDsl(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var s = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ").Trim();
            s = s.Replace('：', ':').Replace('･', '・').Replace('．', '.').Replace('，', ',');
            s = s.Replace(" ：", ":").Replace(": ", ":");
            s = s.Replace("。", " ").Replace("、", " ").Trim();
            s = Regex.Replace(s, @"\s+", " ");

            // --- Enter (召喚/出た時) -> optional draw ---
            if (Regex.IsMatch(s, @"(召喚時|召喚されたとき|バトルゾーンに出たとき|バトルゾーンに出た時|出たとき|出た時)"))
            {
                var n = TryExtractDrawCount(s);
                return n.HasValue ? $"on summon: draw {n.Value}" : "on summon";
            }

            // --- Attack declared (攻撃時/攻撃宣言時) -> optional draw ---
            if (Regex.IsMatch(s, @"(攻撃宣言時|攻撃する時|攻撃時|攻撃したとき|攻撃した時)"))
            {
                var n = TryExtractDrawCount(s);
                return n.HasValue ? $"on attack: draw {n.Value}" : "on attack";
            }

            // --- Destroyed (破壊された時 / バトルに敗北して破壊 etc.) ---
            if (Regex.IsMatch(s, @"(破壊されたとき|破壊された時|破壊時|このクリーチャーが破壊されるとき)"))
            {
                return "on destroyed";
            }

            // --- Wins battle (バトルに勝った時) ---
            if (Regex.IsMatch(s, @"(バトルに勝ったとき|バトルに勝った時|勝利時)"))
            {
                return "on battle win";
            }

            // --- Shield break (シールドをブレイクした時 / ブレイク時) ---
            if (Regex.IsMatch(s, @"(シールドをブレイクしたとき|シールドをブレイクした時|ブレイクしたとき|ブレイク時)"))
            {
                return "on shield break";
            }

            // English DSL passthroughs
            if (Regex.IsMatch(s, @"^\s*on\s+(summon|enter)\s*:\s*draw\s+\d+\s*$")) return s;
            if (Regex.IsMatch(s, @"^\s*on\s+attack\s*:\s*draw\s+\d+\s*$")) return s;
            if (Regex.IsMatch(s, @"^\s*on\s+(summon|attack|destroyed|battle win|shield break)\s*$")) return s;

            return null;
        }

        private static int? TryExtractDrawCount(string s)
        {
            var m = Regex.Match(s, @"(\d+)\s*枚?(?:引く|ドロー)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n1)) return n1;

            m = Regex.Match(s, @"カードを\s*(\d+)\s*枚\s*引く");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n2)) return n2;

            m = Regex.Match(s, @"([一二三四五六七八九十〇零])\s*枚?(?:引く|ドロー)");
            if (m.Success)
            {
                var n = KanjiToInt(m.Groups[1].Value);
                if (n.HasValue) return n.Value;
            }

            m = Regex.Match(s, @"カードを\s*([一二三四五六七八九十〇零])\s*枚\s*引く");
            if (m.Success)
            {
                var n = KanjiToInt(m.Groups[1].Value);
                if (n.HasValue) return n.Value;
            }

            return null;
        }

        private static int? KanjiToInt(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            if (token.Length == 1 && KanjiDigits.TryGetValue(token[0], out var d))
            {
                return d == 10 ? 10 : d;
            }

            int total = 0;
            int current = 0;
            foreach (var ch in token)
            {
                if (!KanjiDigits.TryGetValue(ch, out var val)) return null;
                if (val == 10)
                {
                    if (current == 0) current = 1;
                    current *= 10;
                    total += current;
                    current = 0;
                }
                else
                {
                    current += val;
                }
            }
            total += current;
            return total == 0 ? (int?)null : total;
        }
    }
}
