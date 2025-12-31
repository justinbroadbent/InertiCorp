namespace InertiCorp.Core.Tests;

public class SeededRngTests
{
    [Fact]
    public void NextInt_SameSeed_ProducesIdenticalSequence()
    {
        const int seed = 12345;
        var rng1 = new SeededRng(seed);
        var rng2 = new SeededRng(seed);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(rng1.NextInt(0, 100), rng2.NextInt(0, 100));
        }
    }

    [Fact]
    public void NextDouble_SameSeed_ProducesIdenticalSequence()
    {
        const int seed = 54321;
        var rng1 = new SeededRng(seed);
        var rng2 = new SeededRng(seed);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(rng1.NextDouble(), rng2.NextDouble());
        }
    }

    [Fact]
    public void Shuffle_SameSeed_ProducesSameOrder()
    {
        const int seed = 99999;
        var list1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var list2 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var rng1 = new SeededRng(seed);
        var rng2 = new SeededRng(seed);

        rng1.Shuffle(list1);
        rng2.Shuffle(list2);

        Assert.Equal(list1, list2);
    }

    [Fact]
    public void Shuffle_ActuallyShuffles()
    {
        const int seed = 11111;
        var original = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var shuffled = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var rng = new SeededRng(seed);
        rng.Shuffle(shuffled);

        // With high probability, the shuffled list should differ from original
        Assert.NotEqual(original, shuffled);
        // But should contain the same elements
        Assert.Equal(original.OrderBy(x => x), shuffled.OrderBy(x => x));
    }

    [Fact]
    public void NextInt_RespectsRange()
    {
        var rng = new SeededRng(42);

        for (int i = 0; i < 1000; i++)
        {
            int value = rng.NextInt(10, 20);
            Assert.True(value >= 10 && value < 20, $"Value {value} out of range [10, 20)");
        }
    }

    [Fact]
    public void NextDouble_RespectsRange()
    {
        var rng = new SeededRng(42);

        for (int i = 0; i < 1000; i++)
        {
            double value = rng.NextDouble();
            Assert.True(value >= 0.0 && value < 1.0, $"Value {value} out of range [0.0, 1.0)");
        }
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new SeededRng(111);
        var rng2 = new SeededRng(222);

        // Collect first 10 values from each
        var seq1 = Enumerable.Range(0, 10).Select(_ => rng1.NextInt(0, 1000)).ToList();
        var seq2 = Enumerable.Range(0, 10).Select(_ => rng2.NextInt(0, 1000)).ToList();

        Assert.NotEqual(seq1, seq2);
    }
}
