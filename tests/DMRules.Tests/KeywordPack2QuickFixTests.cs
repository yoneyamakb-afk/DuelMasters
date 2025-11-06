// M15.1j - Quick checks for fixes
using System.Linq;
using Xunit;
using DMRules.Engine.TextParsing;

namespace DMRules.Tests
{
    public class KeywordPack2QuickFixTests
    {
        [Fact]
        public void InvasionZero_Prioritized_Over_Invasion()
        {
            var r = CardTextParser.Parse("侵略 ZERO-自然のドラゴン：このクリーチャーが出た時、〜。");
            Assert.Contains(r.Tokens, t => t.Template == TemplateKey.InvasionZero);
        }

        [Fact]
        public void GZero_LineStart_Anchored()
        {
            var r = CardTextParser.Parse("G・ゼロ：相手のターン中ならコストを支払わずに召喚してもよい。");
            Assert.Contains(r.Tokens, t => t.Template == TemplateKey.GZero);
        }
    }
}
