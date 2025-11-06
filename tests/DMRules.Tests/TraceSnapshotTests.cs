// M15.1a - Snapshot skeleton (unchanged)
using Xunit;
using System.IO;
using DMRules.Engine.TextParsing;
using DMRules.Engine.Tracing;
using System.Linq;

namespace DMRules.Tests
{
    public class TraceSnapshotTests
    {
        [Theory]
        [InlineData("BolmeteusWhiteDragon", "このクリーチャーが攻撃する時、相手のシールドを1つ選び、墓地に置く。")]
        [InlineData("AquaSurfer", "シールドトリガー。相手のクリーチャーを1体選び、持ち主の手札に戻す。")]
        public void CardTraceSnapshot_Basic(string cardName, string text)
        {
            var parsed = CardTextParser.Parse(text);
            var trace = new TemplateParseTrace();
            trace.Add(new TemplateParseTrace.Record
            {
                CardName = cardName,
                Tokens = parsed.Tokens.Select(t => t.ToString()).ToList(),
                Unresolved = parsed.UnresolvedPhrases.ToList()
            });

            var outDir = Path.Combine(Path.GetTempPath(), "dmrules_m15_snapshots");
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, $"{cardName}.json");
            trace.WriteJson(outPath);

            Assert.True(File.Exists(outPath));
        }
    }
}
