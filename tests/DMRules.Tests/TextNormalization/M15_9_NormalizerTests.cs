using Xunit;
using DMRules.Engine.Text.Overrides;

namespace DMRules.Tests.TextNormalization
{
    public class M15_9_NormalizerTests
    {
        [Fact]
        public void RemoveShieldTriggerPrefix_WorksCorrectly()
        {
            var input1 = "S・トリガー：カードを1枚引く。";
            var input2 = "シールド・トリガー：相手のクリーチャーを破壊する。";
            var input3 = "Sトリガー：何もしない。"; // 残すべき
            var result1 = M15_9TextNormalizer.RemoveShieldTriggerPrefix(input1);
            var result2 = M15_9TextNormalizer.RemoveShieldTriggerPrefix(input2);
            var result3 = M15_9TextNormalizer.RemoveShieldTriggerPrefix(input3);
            Assert.Equal("カードを1枚引く。", result1);
            Assert.Equal("相手のクリーチャーを破壊する。", result2);
            Assert.Equal("Sトリガー：何もしない。", result3);
        }

        [Fact]
        public void SquareBracket_RemovedBeforeTagging()
        {
            var input = "【テスト】このターン、～";
            var result = M15_9TextNormalizer.RemoveSquareBrackets(input);
            Assert.Equal("このターン、～", result);
        }

        [Fact]
        public void RandomDiscard_Normalization_BothModes()
        {
            var compat = M15_9TextNormalizer.NormalizeRandomDiscardCompat("ランダムに相手の手札を２枚捨てる。");
            var strict = M15_9TextNormalizer.NormalizeRandomDiscardStrict("ランダムに相手の手札を２枚捨てる。");
            Assert.Equal("相手の手札をランダムに2枚捨てる", compat);
            Assert.Equal("相手は自身の手札をランダムに2枚捨てる", strict);
        }

        [Fact]
        public void UntilStartOfNextTurn_StandardizedInStrict()
        {
            var result = M15_9TextNormalizer.NormalizeUntilStartOfNextTurnStrict("相手の次のターンのはじめまで～");
            Assert.Equal("相手の次のターン中～", result);
        }

        [Fact]
        public void ApplyAll_WorksInStrictMode()
        {
            System.Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", "Strict");
            var text = "【注】S・トリガー：相手の次のターンのはじめまで、ランダムに相手の手札を１枚捨てる。";
            var result = M15_9TextNormalizer.ApplyAll(text);
            Assert.Contains("相手の次のターン中", result);
            Assert.Contains("相手は自身の手札をランダムに1枚捨てる", result);
        }
    }
}
