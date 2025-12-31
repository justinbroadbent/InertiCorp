namespace InertiCorp.Core;

/// <summary>
/// Pool of available objectives from which to draw at game start.
/// </summary>
public sealed class ObjectivePool
{
    private readonly IReadOnlyList<Objective> _objectives;

    /// <summary>
    /// Number of objectives in the pool.
    /// </summary>
    public int Count => _objectives.Count;

    public ObjectivePool(IEnumerable<Objective> objectives)
    {
        _objectives = objectives.ToList();
    }

    /// <summary>
    /// Draws the specified number of unique objectives from the pool.
    /// </summary>
    /// <param name="count">Number of objectives to draw.</param>
    /// <param name="rng">Random number generator for selection.</param>
    /// <returns>List of drawn objectives.</returns>
    /// <exception cref="InvalidOperationException">Thrown if pool has fewer objectives than requested.</exception>
    public IReadOnlyList<Objective> Draw(int count, IRng rng)
    {
        if (_objectives.Count < count)
        {
            throw new InvalidOperationException(
                $"Cannot draw {count} objectives from pool with only {_objectives.Count} objectives.");
        }

        // Create a copy and shuffle to get random selection
        var shuffled = _objectives.ToList();
        rng.Shuffle(shuffled);

        return shuffled.Take(count).ToList();
    }
}
