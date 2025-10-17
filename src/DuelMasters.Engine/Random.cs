namespace DuelMasters.Engine;

public interface IRandomSource
{
    int Next();
    int Next(int min, int max);
    void Reseed(int seed);
}

public sealed class DefaultRandom : IRandomSource
{
    private System.Random _r;

    public DefaultRandom(int seed) { _r = new System.Random(seed); }
    public int Next() => _r.Next();
    public int Next(int min, int max) => _r.Next(min, max);
    public void Reseed(int seed) => _r = new System.Random(seed);
}
