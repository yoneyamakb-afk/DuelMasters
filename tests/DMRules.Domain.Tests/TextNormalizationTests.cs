using Xunit;
using DMRules.Domain;

namespace DMRules.Domain.Tests;

public class TextNormalizationTests
{
    [Theory]
    [InlineData("6", 6)]
    [InlineData("コスト：5", 5)]
    [InlineData("12000", 12000)]
    [InlineData("-", null)]
    [InlineData("—", null)]
    public void ParseIntLoose_Works(string input, int? expected)
    {
        Assert.Equal(expected, TextNormalization.ParseIntLoose(input));
    }

    [Fact]
    public void CivSplit_Normalizes()
    {
        var civs = TextNormalization.NormalizeCivs("光 / 水・闇");
        Assert.Contains("Light", civs);
        Assert.Contains("Water", civs);
        Assert.Contains("Darkness", civs);
    }

    [Fact]
    public void Types_Normalizes()
    {
        var types = TextNormalization.NormalizeTypes("クリーチャー / 呪文");
        Assert.Contains("Creature", types);
        Assert.Contains("Spell", types);
    }
}
