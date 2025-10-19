using DMRules.Engine;
using Xunit;

namespace DMRules.Tests;

public static class TestUtils
{
    public static GameState NewState(Phase phase = Phase.Setup, PlayerId active = PlayerId.P0, int bf = 0, int gy = 0)
        => new GameState(phase, activePlayer: active, battlefieldCount: bf, graveyardCount: gy);
}
