namespace InertiCorp.Core;

/// <summary>
/// A deterministic random number generator backed by a seeded System.Random.
/// Given the same seed, produces identical sequences of random values.
/// </summary>
public sealed class SeededRng : IRng
{
    private readonly Random _random;

    public SeededRng(int seed)
    {
        _random = new Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }

    public void Shuffle<T>(IList<T> list)
    {
        // Fisher-Yates shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
