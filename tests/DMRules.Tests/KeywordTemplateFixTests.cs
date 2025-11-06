// M15.1e - Fix confirmation tests
using System.Linq;
using Xunit;
using DMRules.Engine.TextParsing;

namespace DMRules.Tests
{
    public class KeywordTemplateFixTests
    {
        [Theory]
        [InlineData("WORLD・BREAKER", TemplateKey.WorldBreaker)]
        [InlineData("ワールド・ブレイカー", TemplateKey.WorldBreaker)]
        [InlineData("チャージャー（この呪文を唱えた後、マナゾーンに置く）", TemplateKey.Charger)]
        [InlineData("チャージャー(この呪文を唱えた後、マナゾーンに置く)", TemplateKey.Charger)]
        public void WorldBreaker_CaseInsensitive_And_Charger_Parens(string text, TemplateKey key)
        {
            var r = CardTextParser.Parse(text);
            Assert.Contains(r.Tokens, t => t.Template == key);
        }

        [Fact]
        public void SetMarker_Kakko_Is_Ignored()
        {
            var r = CardTextParser.Parse("【lwn05】 このカードを出した時、カードを1枚引いてもよい。");
            Assert.DoesNotContain(r.UnresolvedPhrases, u => u.Contains("lwn05"));
        }

        [Fact]
        public void GNeo_Prioritized_Over_Neo()
        {
            var r = CardTextParser.Parse("G-NEO進化：自然のクリーチャー1体の上に置いてもよい");
            Assert.Contains(r.Tokens, t => t.Template == TemplateKey.GNeoEvo);
        }
    }
}
