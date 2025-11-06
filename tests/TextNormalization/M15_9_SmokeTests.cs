// Smoke tests validating both modes (Compat / Strict)
using Xunit;
using System;
using DMRules.Engine.Text.Overrides;

namespace DMRules.Tests.TextNormalization
{
    public class M15_9_SmokeTests
    {
        [Fact]
        public void Compat_Mode_Default()
        {
            Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", null);
            Assert.Equal("カードを1枚引く。", M15_9TextNormalizer.RemoveShieldTriggerPrefix("S・トリガー：カードを1枚引く。"));
            Assert.Equal("Sトリガー：何もしない。", M15_9TextNormalizer.RemoveShieldTriggerPrefix("Sトリガー：何もしない。")); // should stay
            Assert.Equal("相手の手札をランダムに2枚捨てる", M15_9TextNormalizer.NormalizeRandomDiscard("ランダムに相手の手札を２枚捨てる"));
            Assert.Equal("相手の次のターンのはじめまで攻撃できない。", M15_9TextNormalizer.ApplyAll("相手の次のターンのはじめまで攻撃できない。"));
            Assert.Equal("このターン、", M15_9TextNormalizer.RemoveSquareBrackets("【EX】このターン、"));
        }

        [Fact]
        public void Strict_Mode()
        {
            Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", "Strict");
            var s = M15_9TextNormalizer.ApplyAll("【注】S・トリガー：相手の次のターンのはじめまで、ランダムに相手の手札を１枚捨てる。");
            Assert.Contains("相手の次のターン中", s);
            Assert.Contains("相手は自身の手札をランダムに1枚捨てる", s);
        }
    }
}
