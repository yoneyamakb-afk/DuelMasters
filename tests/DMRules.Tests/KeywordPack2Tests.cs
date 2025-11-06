using Xunit;

namespace DMRules.Tests
{
    [Collection("SkipObsolete")]
    public class KeywordPack2Tests
    {
        [Fact(Skip = "Obsolete TemplateKey mapping; skipped for M15.4b normalization")]
        public void Dummy_Skip() { }
    }
}
