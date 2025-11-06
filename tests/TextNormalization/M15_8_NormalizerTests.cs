// xUnit tests for M15.8 overlay (Fix: using DMRules.Engine.Text.Overrides)
using Xunit;
using DMRules.Engine.Text.Overrides;

namespace DMRules.Tests.TextNormalization
{
    public class M15_8_NormalizerTests
    {
        [Theory]
        [InlineData("【EX】このターン、～", "このターン、～")]
        [InlineData("効果【ABC】を発動。【DEF】", "効果を発動。")]
        public void RemoveSquareBrackets(string input, string expected)
        {
            Assert.Equal(expected, M15_8TextNormalizer.RemoveSquareBrackets(input));
        }

        [Theory]
        [InlineData("S・トリガー：カードを1枚引く。", "カードを1枚引く。")]
        [InlineData("シールド・トリガー: ブロッカーを持つ。", "ブロッカーを持つ。")]
        [InlineData("Sトリガー：何もしない。", "Sトリガー：何もしない。")] // 誤表記は保持（行頭一致のみ）
        public void RemoveShieldTriggerPrefix(string input, string expected)
        {
            Assert.Equal(expected, M15_8TextNormalizer.RemoveShieldTriggerPrefix(input));
        }

        [Theory]
        [InlineData("相手の次のターンのはじめまで、パワー+3000。", "相手の次のターン中、パワー+3000。")]
        [InlineData("自分の次のターンのはじめまでブロッカー。", "自分の次のターン中ブロッカー。")]
        [InlineData("次の相手のターンのはじめまで攻撃できない。", "次の相手のターン中攻撃できない。")]
        public void NormalizeUntilStartOfNextTurn(string input, string expected)
        {
            Assert.Equal(expected, M15_8TextNormalizer.NormalizeUntilStartOfNextTurn(input));
        }

        [Theory]
        [InlineData("ランダムに相手の手札を1枚捨てる", "相手は自身の手札をランダムに1枚捨てる")]
        [InlineData("ランダムに相手の手札を２枚捨てる", "相手は自身の手札をランダムに2枚捨てる")]
        public void NormalizeRandomDiscard(string input, string expected)
        {
            Assert.Equal(expected, M15_8TextNormalizer.NormalizeRandomDiscard(input));
        }

        [Theory]
        [InlineData("【注】S・トリガー：相手の次のターンのはじめまで、ブロッカー。", "相手の次のターン中、ブロッカー。")]
        public void ApplyAll_OrderGuarantee(string input, string expected)
        {
            Assert.Equal(expected, M15_8TextNormalizer.ApplyAll(input));
        }
    }
}
