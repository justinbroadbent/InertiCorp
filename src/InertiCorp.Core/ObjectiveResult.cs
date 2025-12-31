namespace InertiCorp.Core;

/// <summary>
/// Result of evaluating an objective at end of game.
/// </summary>
public sealed record ObjectiveResult(Objective Objective, bool IsMet);
