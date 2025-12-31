namespace InertiCorp.Core;

/// <summary>
/// A board directive that defines requirements evaluated at Resolution.
/// Now uses floor-based targets instead of growth requirements for sustainability.
/// Requirements scale with board pressure level.
/// </summary>
public sealed record BoardDirective(
    string DirectiveId,
    string Title,
    Func<int, int> RequirementFormula,
    Func<int, int, int, bool> EvaluationFunc)
{
    /// <summary>
    /// Gets the required amount for the given pressure level.
    /// </summary>
    public int GetRequiredAmount(int pressureLevel) => RequirementFormula(pressureLevel);

    /// <summary>
    /// Checks if the directive is met given last profit, current profit, and pressure.
    /// </summary>
    public bool IsMet(int lastProfit, int currentProfit, int pressureLevel) =>
        EvaluationFunc(lastProfit, currentProfit, pressureLevel);

    /// <summary>
    /// Gets a description of the directive including the required amount.
    /// </summary>
    public string GetDescription(int pressureLevel)
    {
        var required = GetRequiredAmount(pressureLevel);
        return $"{Title}: ${required}M target";
    }

    /// <summary>
    /// Generates a board directive for the current pressure level.
    /// For now, always returns ProfitFloor. Future: random selection from pool.
    /// </summary>
    public static BoardDirective Generate(int pressureLevel, IRng rng)
    {
        // For now, always profit floor. Future versions could have a pool.
        _ = rng; // Reserved for future random selection
        _ = pressureLevel; // Reserved for scaling difficulty
        return ProfitFloor;
    }

    /// <summary>
    /// Profit floor directive - achieve at least $X profit this quarter.
    /// Much more forgiving than the old "increase by $X" requirement.
    /// Formula: Required = 5 + (pressure * 2), capped at reasonable levels.
    /// Early game (P1-2): $7-9M floor (very achievable)
    /// Mid game (P4-6): $13-17M floor (requires good play)
    /// Late game (P7-8): $19-21M floor (challenging but base ops helps)
    /// </summary>
    public static BoardDirective ProfitFloor { get; } = new(
        DirectiveId: "DIR_PROFIT_FLOOR",
        Title: "Achieve Quarterly Profit",
        RequirementFormula: pressure =>
        {
            // Linear growth with cap - more predictable than sqrt
            // Scales from $7M to $21M over the game
            return Math.Min(21, 5 + pressure * 2);
        },
        EvaluationFunc: (_, currentProfit, pressure) =>
        {
            var required = Math.Min(21, 5 + pressure * 2);
            // Just need to hit the floor, not grow from last quarter
            return currentProfit >= required;
        }
    );

    /// <summary>
    /// Legacy profit increase directive (kept for reference/testing).
    /// </summary>
    public static BoardDirective ProfitIncrease { get; } = new(
        DirectiveId: "DIR_PROFIT_INCREASE",
        Title: "Increase Quarterly Profit",
        RequirementFormula: pressure =>
        {
            int growth = (int)Math.Floor(Math.Sqrt(pressure * 8));
            return 5 + growth;
        },
        EvaluationFunc: (lastProfit, currentProfit, pressure) =>
        {
            int growth = (int)Math.Floor(Math.Sqrt(pressure * 8));
            var required = 5 + growth;
            var increase = currentProfit - lastProfit;
            return increase >= required;
        }
    );
}
