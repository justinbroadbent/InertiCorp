namespace InertiCorp.Core;

/// <summary>
/// Abstraction for random number generation.
/// All simulation randomness must flow through this interface to ensure determinism.
/// </summary>
public interface IRng
{
    /// <summary>
    /// Returns a random integer in the range [minInclusive, maxExclusive).
    /// </summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>
    /// Returns a random double in the range [0.0, 1.0).
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Shuffles the list in place using the Fisher-Yates algorithm.
    /// </summary>
    void Shuffle<T>(IList<T> list);
}
