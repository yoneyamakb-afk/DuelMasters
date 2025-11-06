// M15.9 Official smoke tests (Compat & Strict)
using Xunit;
using System;
using DMRules.Engine.Text.Overrides;

namespace DMRules.Tests.TextNormalization
{
    public class M15_9_SmokeTests
    {
        [Fact]
        public void Compat_Defaults()
        {
            Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", null);
            Assert.Equal("カードを1枚引く。", M15_9TextNormalizer.RemoveShieldTriggerPrefix("S・トリガー：カードを1枚引く。"));
            Assert.Equal("Sトリガー：何もしない。", M15_9TextNormalizer.RemoveShieldTriggerPrefix("Sトリガー：何もしない。"));
            Assert.Equal("相手の手札をランダムに2枚捨てる", M15_9TextNormalizer.NormalizeRandomDiscardCompat("ランダムに相手の手札を２枚捨てる。"));
            Assert.Equal("このターン、～", M15_9TextNormalizer.RemoveSquareBrackets("【注】このターン、～"));
        }

        [Fact]
        public void Strict_Mode_Behavior()
        {
            Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", "Strict");
            var s = M15_9TextNormalizer.ApplyAll("【注】S・トリガー：相手の次のターンのはじめまで、ランダムに相手の手札を１枚捨てる。");
            Assert.Contains("相手の次のターン中", s);
            Assert.Contains("相手は自身の手札をランダムに1枚捨てる", s);
        }
    }
}
