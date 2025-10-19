using System;
namespace DMRules.Engine;
public interface IChooser
{
    // Generic choice among N candidates; describeIndex(i) should return a human-readable label.
    int Choose(int count, Func<int, string> describeIndex);
}

// Default: deterministic index 0
public sealed class DefaultChooser : IChooser
{
    public int Choose(int count, Func<int, string> describeIndex) => 0;
}
