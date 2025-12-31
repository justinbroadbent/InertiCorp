namespace InertiCorp.Core;

/// <summary>
/// Interface for effects that modify game state.
/// Effects must be deterministic given the same RNG state.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Applies this effect to the game state.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="rng">Random number generator for any randomness.</param>
    /// <returns>New game state and log entries describing what happened.</returns>
    (GameState NewState, IEnumerable<LogEntry> Entries) Apply(GameState state, IRng rng);
}
