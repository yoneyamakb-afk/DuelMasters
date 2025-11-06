using Xunit;
using DMRules.Engine.Text.Overrides;

namespace DMRules.Tests.TextNormalization
{
    public class KeywordTemplateTests
    {
        [Fact]
        public void PeriodExpression_Strict_NormalizesToInTurn()
        {
            System.Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", "Strict");
            var s = M15_9TextNormalizer.ApplyAll("相手の次のターンのはじめまで、このクリーチャーはブロックされない。");
            Assert.Contains("相手の次のターン中", s);
        }

        [Fact]
        public void PeriodExpression_Compat_KeepsOriginalPhrase()
        {
            System.Environment.SetEnvironmentVariable("DMRULES_TEXT_NORMALIZATION_MODE", "Compat");
            var s = M15_9TextNormalizer.ApplyAll("自分の次のターンのはじめまで、パワー+2000。");
            Assert.Contains("自分の次のターンのはじめまで", s);
        }

        [Fact]
        public void SquareBrackets_AreRemovedBeforeTagging()
        {
            var s = M15_9TextNormalizer.ApplyAll("【テスト】このターン、～");
            Assert.DoesNotContain("【", s);
        }
    }
}
