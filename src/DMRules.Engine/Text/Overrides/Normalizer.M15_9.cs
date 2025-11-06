// SPDX-License-Identifier: MIT
// M15.9 Official — Compat/Strict modes, finalized rules
using System;
using System.Text.RegularExpressions;

namespace DMRules.Engine.Text.Overrides
{
    public static class M15_9TextNormalizer
    {
        // 先行除去: 【…】
        private static readonly Regex RxSquareBrackets = new(@"【[^】]*】", RegexOptions.Compiled);

        // 接頭辞削除: 「S・トリガー」「シールド・トリガー」のみ（Sトリガーは残す）
        private static readonly Regex RxShieldTriggerPrefix = new(
            @"^(?:S・トリガー|シールド・トリガー)[:：]\s*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // 期間表現: …のはじめまで
        private static readonly Regex RxUntilStartOfNextTurn = new(
            @"(?<Head>(?:次の[^。、「」]*?ターン|(?:自分|相手)の次のターン))のはじめまで",
            RegexOptions.Compiled);

        // ランダム捨て（全角数字対応）
        private static readonly Regex RxRandomDiscard = new(
            @"ランダムに相手の手札を(?<N>[0-9０-９]+)枚?捨てる(?:。)?",
            RegexOptions.Compiled);

        private static string Mode => (Environment.GetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE") ?? "Compat").Trim();

        public static string ApplyAll(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var s = input;

            // 1) 【…】先行除去
            s = RxSquareBrackets.Replace(s, string.Empty);

            // 2) ShieldTrigger 接頭辞除去（行頭/複数行）
            s = RxShieldTriggerPrefix.Replace(s, string.Empty);

            // 3) ランダム捨て（Compat/Strictで文型を切替）
            s = RxRandomDiscard.Replace(s, m =>
            {
                var n = ToHalfDigits(m.Groups["N"].Value);
                if (IsStrict()) return $"相手は自身の手札をランダムに{n}枚捨てる";
                return $"相手の手札をランダムに{n}枚捨てる";
            });

            // 4) 期間表現
            if (IsStrict())
            {
                s = RxUntilStartOfNextTurn.Replace(s, m => $"{m.Groups["Head"].Value}中");
            }

            return s;
        }

        private static bool IsStrict() => Mode.Equals("Strict", StringComparison.OrdinalIgnoreCase);

        private static string ToHalfDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Replace("０","0").Replace("１","1").Replace("２","2").Replace("３","3").Replace("４","4")
                    .Replace("５","5").Replace("６","6").Replace("７","7").Replace("８","8").Replace("９","9");
        }

        // unit-test helpers
        public static string RemoveSquareBrackets(string input) => RxSquareBrackets.Replace(input ?? string.Empty, string.Empty);
        public static string RemoveShieldTriggerPrefix(string input) => RxShieldTriggerPrefix.Replace(input ?? string.Empty, string.Empty);
        public static string NormalizeRandomDiscardCompat(string input) => RxRandomDiscard.Replace(input ?? string.Empty, m => $"相手の手札をランダムに{ToHalfDigits(m.Groups["N"].Value)}枚捨てる");
        public static string NormalizeRandomDiscardStrict(string input) => RxRandomDiscard.Replace(input ?? string.Empty, m => $"相手は自身の手札をランダムに{ToHalfDigits(m.Groups["N"].Value)}枚捨てる");
        public static string NormalizeUntilStartOfNextTurnStrict(string input) => RxUntilStartOfNextTurn.Replace(input ?? string.Empty, m => $"{m.Groups["Head"].Value}中");
    }
}
