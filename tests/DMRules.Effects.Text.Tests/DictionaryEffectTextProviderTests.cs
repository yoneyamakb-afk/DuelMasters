
using DMRules.Effects.Text;
using Xunit;

public class DictionaryEffectTextProviderTests
{
    [Fact]
    public void InMemoryProvider_Works()
    {
        var p = new DictionaryEffectTextProvider()
            .AddByFaceId(123, "on summon: draw 2")
            .AddByName("Example Card", "on attack: draw 1");

        Assert.Equal("on summon: draw 2", p.GetEffectTextByFaceId(123));
        Assert.Equal("on attack: draw 1", p.GetEffectTextByName("Example Card"));
        Assert.Null(p.GetEffectTextByFaceId(999)); // unknown -> null
    }
}
