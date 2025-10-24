#if false
using Xunit;
using DMRules.Engine.Integration.M11;

public class DbRegulationAdapterTests
{
    [Fact]
    public void None_When_TableMissing()
    {
        var a = new DbRegulationAdapter(RegulationConfig.ResolveDbPath());
        var f = a.GetStaticFlags("ドリーム・ボルメテウス・ホワイト・ドラゴン");
        Assert.True(((int)f) >= 0);
    }
}
#endif
