namespace InertiCorp.Core;

/// <summary>
/// Represents a game objective that can be evaluated against the organization state.
/// </summary>
public sealed record Objective
{
    /// <summary>
    /// Unique identifier for this objective.
    /// </summary>
    public string ObjectiveId { get; }

    /// <summary>
    /// Short title of the objective.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Human-readable description of the objective.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The condition that must be met for this objective to be satisfied.
    /// </summary>
    public IObjectiveCondition Condition { get; }

    public Objective(string objectiveId, string title, string description, IObjectiveCondition condition)
    {
        ObjectiveId = objectiveId;
        Title = title;
        Description = description;
        Condition = condition;
    }

    /// <summary>
    /// Evaluates whether this objective is satisfied by the given organization state.
    /// </summary>
    public bool IsMet(OrgState state) => Condition.IsMet(state);
}
