namespace InertiCorp.Core;

/// <summary>
/// Interface for objective conditions that can be evaluated against organization state.
/// </summary>
public interface IObjectiveCondition
{
    /// <summary>
    /// Evaluates whether this condition is met for the given organization state.
    /// </summary>
    /// <param name="state">The organization state to evaluate.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    bool IsMet(OrgState state);
}
