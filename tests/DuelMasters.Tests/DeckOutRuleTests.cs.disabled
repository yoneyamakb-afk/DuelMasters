
using DMRules.Engine;
using FluentAssertions;
using Xunit;

namespace DMRules.Tests;

public class DeckOutRuleTests
{
    [Fact(DisplayName = "SBA: Player who cannot draw loses (deck-out)")]
    public void PlayerWhoCannotDraw_Loses()
    {
        IGameState s = new MinimalState(libraryCountTP: 0, attemptDrawTP: true);
        s = Adapter.Instance.DoSBAUntilStable(s);
        s.Losers.Should().Contain("TP");
    }
}
