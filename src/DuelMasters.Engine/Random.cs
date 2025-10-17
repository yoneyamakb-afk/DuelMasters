
using System;

namespace DuelMasters.Engine;

public sealed class SeededRandom : IRandomSource
{
    private Random _random;
    public SeededRandom(int seed) => _random = new Random(seed);
    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    public void Reseed(int seed) => _random = new Random(seed);
}
