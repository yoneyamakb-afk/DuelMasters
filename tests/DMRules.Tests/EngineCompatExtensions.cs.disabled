#nullable enable
using DMRules.Engine;

namespace DMRules.Engine;

public static class EngineCompatExtensions
{
    public static MinimalState Step(MinimalState s, string phase)
        => DMRules.Tests.StaticCompat.Step(s, phase);

    public static MinimalState Step(MinimalState s, string phase, string priority)
        => DMRules.Tests.StaticCompat.Step(s, phase, priority);

    public static MinimalState ApplyReplacement(MinimalState s, object replacement)
        => DMRules.Tests.StaticCompat.ApplyReplacement(s, replacement);
}
