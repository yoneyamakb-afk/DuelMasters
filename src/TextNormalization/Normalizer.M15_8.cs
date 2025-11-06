// SPDX-License-Identifier: MIT
// M15.8 Text Normalization Overlay (Fix) 窶・namespace aligned to DMRules.Engine.Text.Overrides
using System;
using System.Text.RegularExpressions;

namespace DMRules.Engine.Text.Overrides
{
    public static class M15_9TextNormalizer
    {
        // 繝ｫ繝ｼ繝ｫ1: 縲舌代・髯､蜴ｻ・医ち繧ｰ蜃ｦ逅・ｈ繧雁燕・・        private static readonly Regex RxSquareBrackets = new Regex(@"縲深^縲曽*縲・, RegexOptions.Compiled);

        // 繝ｫ繝ｼ繝ｫ2: ShieldTrigger 謗･鬆ｭ霎槭・谿狗蕗・郁｡碁ｭ髯仙ｮ壹〒髯､蜴ｻ・・        private static readonly Regex RxShieldTriggerPrefix = new Regex(
            @"^(?:S繝ｻ?繝医Μ繧ｬ繝ｼ|繧ｷ繝ｼ繝ｫ繝峨・繝医Μ繧ｬ繝ｼ)[:・咯\s*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        // 繝ｫ繝ｼ繝ｫ3: 縲梧ｬ｡縺ｮ.*繧ｿ繝ｼ繝ｳ縺ｮ縺ｯ縺倥ａ縺ｾ縺ｧ縲坂・縲梧ｬ｡縺ｮ.*繧ｿ繝ｼ繝ｳ荳ｭ縲・        private static readonly Regex RxUntilStartOfNextTurn = new Regex(
            @"(?<Head>(?:谺｡縺ｮ[^縲ゅ√後江*?繧ｿ繝ｼ繝ｳ|(?:閾ｪ蛻・逶ｸ謇・縺ｮ谺｡縺ｮ繧ｿ繝ｼ繝ｳ))縺ｮ縺ｯ縺倥ａ縺ｾ縺ｧ",
            RegexOptions.Compiled);

        // 繝ｫ繝ｼ繝ｫ4: 繝ｩ繝ｳ繝繝縺ｫ逶ｸ謇九・謇区惆繧誰譫壽昏縺ｦ繧・竊・逶ｸ謇九・閾ｪ霄ｫ縺ｮ謇区惆繧偵Λ繝ｳ繝繝縺ｫN譫壽昏縺ｦ繧・        private static readonly Regex RxRandomDiscard = new Regex(
            @"繝ｩ繝ｳ繝繝縺ｫ逶ｸ謇九・謇区惆繧・?<N>[0-9・・・兢+)譫・謐ｨ縺ｦ繧・,
            RegexOptions.Compiled);

        public static string ApplyAll(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var s = input;
            s = RxSquareBrackets.Replace(s, string.Empty);
            s = RxShieldTriggerPrefix.Replace(s, string.Empty);
            s = RxUntilStartOfNextTurn.Replace(s, m => $"{m.Groups["Head"].Value}荳ｭ");
            s = RxRandomDiscard.Replace(s, m =>
            {
                var n = m.Groups["N"].Value;
                n = n.Replace("・・,"0").Replace("・・,"1").Replace("・・,"2").Replace("・・,"3").Replace("・・,"4")
                     .Replace("・・,"5").Replace("・・,"6").Replace("・・,"7").Replace("・・,"8").Replace("・・,"9");
                return $"逶ｸ謇九・閾ｪ霄ｫ縺ｮ謇区惆繧偵Λ繝ｳ繝繝縺ｫ{n}譫壽昏縺ｦ繧・;
            });
            return s;
        }

        // 蛟句挨驕ｩ逕ｨ繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ
        public static string RemoveSquareBrackets(string input) => RxSquareBrackets.Replace(input ?? string.Empty, string.Empty);
        public static string RemoveShieldTriggerPrefix(string input) => RxShieldTriggerPrefix.Replace(input ?? string.Empty, string.Empty);
        public static string NormalizeUntilStartOfNextTurn(string input) => RxUntilStartOfNextTurn.Replace(input ?? string.Empty, m => $"{m.Groups["Head"].Value}荳ｭ");
        public static string NormalizeRandomDiscard(string input) => RxRandomDiscard.Replace(input ?? string.Empty, m => $"逶ｸ謇九・閾ｪ霄ｫ縺ｮ謇区惆繧偵Λ繝ｳ繝繝縺ｫ{m.Groups["N"].Value}譫壽昏縺ｦ繧・);
    }
}

