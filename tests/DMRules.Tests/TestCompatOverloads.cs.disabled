#nullable enable
using DMRules.Engine;

namespace DMRules.Tests;

public static class TestCompatOverloads
{
    public static MinimalState Step(MinimalState s, string phase)
        => StaticCompat.Step(s, phase);

    public static MinimalState Step(MinimalState s, string phase, string priority)
        => StaticCompat.Step(s, phase, priority);

    public static MinimalState ApplyReplacement(MinimalState s, object replacement)
        => StaticCompat.ApplyReplacement(s, replacement);
}
