// M15.1h - Final fix3 confirmation tests (normalization + options)
using System.Linq;
using Xunit;
using DMRules.Engine.TextParsing;

namespace DMRules.Tests
{
    public class KeywordTemplateFinalFix3Tests
    {
        [Theory]
        [InlineData("WORLD・BREAKER", TemplateKey.WorldBreaker)]
        [InlineData("ワールド・ブレイカー", TemplateKey.WorldBreaker)]
        [InlineData("WORLD・BREAKER の効果を得る。", TemplateKey.WorldBreaker)]
        [InlineData("このクリーチャーはワールド・ブレイカーを得る。", TemplateKey.WorldBreaker)]
        public void WorldBreaker_RomanOrKana(string text, TemplateKey key)
        {
            var r = CardTextParser.Parse(text);
            Assert.Contains(r.Tokens, t => t.Template == key);
        }

        [Theory]
        [InlineData("チャージャー（この呪文を唱えた後、マナゾーンに置く）", TemplateKey.Charger)]
        [InlineData("チャージャー(この呪文を唱えた後、マナゾーンに置く)", TemplateKey.Charger)]
        [InlineData("チャージャー（この呪文を唱えた後\nマナゾーンに置く）", TemplateKey.Charger)]
        [InlineData("チャージャー（この呪文を唱えた後, マナゾーンに置く）", TemplateKey.Charger)]
        public void Charger_Parens_With_Comma_And_Newline(string text, TemplateKey key)
        {
            var r = CardTextParser.Parse(text);
            Assert.Contains(r.Tokens, t => t.Template == key);
        }

        [Fact]
        public void SetMarker_Wide_Pattern_Ignored()
        {
            var r = CardTextParser.Parse("【lwn05】 [ll03] このカードを出した時、カードを1枚引いてもよい。");
            Assert.DoesNotContain(r.UnresolvedPhrases, u => u.Contains("lwn05") || u.Contains("ll03"));
        }
    }
}
