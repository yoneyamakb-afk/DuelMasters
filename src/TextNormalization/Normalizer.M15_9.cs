// SPDX-License-Identifier: MIT
// M15.9 Normalization Overlay with Compat/Strict modes.
// Namespace aligned with engine.
using System;
using System.Text.RegularExpressions;

namespace DMRules.Engine.Text.Overrides
{
    public static class M15_9TextNormalizer
    {
        private static readonly Regex RxSquareBrackets = new Regex(@"【[^】]*】", RegexOptions.Compiled);
        // Limit to only proper Japanese spellings with middle dot.
        private static readonly Regex RxShieldTriggerPrefix = new Regex(
            @"^(?:S・トリガー|シールド・トリガー)[:：]\s*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex RxUntilStartOfNextTurn = new Regex(
            @"(?<Head>(?:次の[^。、「」]*?ターン|(?:自分|相手)の次のターン))のはじめまで",
            RegexOptions.Compiled);

        private static readonly Regex RxRandomDiscard = new Regex(
            @"ランダムに相手の手札を(?<N>[0-9０-９]+)枚?捨てる(?:。)?",
            RegexOptions.Compiled);

        private static string Mode => (Environment.GetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE") ?? "Compat").Trim();

        private static string ToHalfDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            return s.Replace("０","0").Replace("１","1").Replace("２","2").Replace("３","3").Replace("４","4")
                    .Replace("５","5").Replace("６","6").Replace("７","7").Replace("８","8").Replace("９","9");
        }

        public static string ApplyAll(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var s = input;

            // 1) remove 【...】 early
            s = RxSquareBrackets.Replace(s, string.Empty);

            // 2) remove proper Shield Trigger prefixes
            s = RxShieldTriggerPrefix.Replace(s, string.Empty);

            // 3) random discard
            s = RxRandomDiscard.Replace(s, m =>
            {
                var n = ToHalfDigits(m.Groups["N"].Value);
                if (string.Equals(Mode, "Strict", StringComparison.OrdinalIgnoreCase))
                {
                    return $"相手は自身の手札をランダムに{n}枚捨てる";
                }
                // Compat: keep legacy phrasing/order
                return $"相手の手札をランダムに{n}枚捨てる";
            });

            // 4) period expression
            if (string.Equals(Mode, "Strict", StringComparison.OrdinalIgnoreCase))
            {
                s = RxUntilStartOfNextTurn.Replace(s, m => $"{m.Groups["Head"].Value}中");
            }
            // Compat: keep "のはじめまで" as-is

            return s;
        }

        // Utilities for testing
        public static string RemoveShieldTriggerPrefix(string input) => RxShieldTriggerPrefix.Replace(input ?? string.Empty, string.Empty);
        public static string RemoveSquareBrackets(string input) => RxSquareBrackets.Replace(input ?? string.Empty, string.Empty);
        public static string NormalizeRandomDiscard(string input) => RxRandomDiscard.Replace(input ?? string.Empty, m => $"相手の手札をランダムに{ToHalfDigits(m.Groups["N"].Value)}枚捨てる");
        public static string NormalizeUntilStartOfNextTurnStrict(string input) => RxUntilStartOfNextTurn.Replace(input ?? string.Empty, m => $"{m.Groups["Head"].Value}中");
    }
}
