namespace InertiCorp.Core;

/// <summary>
/// Composite condition that combines multiple conditions with AND or OR logic.
/// </summary>
public sealed class CompositeCondition : IObjectiveCondition
{
    private readonly IReadOnlyList<IObjectiveCondition> _conditions;
    private readonly bool _requireAll;

    private CompositeCondition(IReadOnlyList<IObjectiveCondition> conditions, bool requireAll)
    {
        _conditions = conditions;
        _requireAll = requireAll;
    }

    /// <summary>
    /// Creates a condition that requires ALL sub-conditions to be met (AND).
    /// </summary>
    public static CompositeCondition And(params IObjectiveCondition[] conditions)
    {
        return new CompositeCondition(conditions, requireAll: true);
    }

    /// <summary>
    /// Creates a condition that requires ANY sub-condition to be met (OR).
    /// </summary>
    public static CompositeCondition Or(params IObjectiveCondition[] conditions)
    {
        return new CompositeCondition(conditions, requireAll: false);
    }

    /// <inheritdoc />
    public bool IsMet(OrgState state)
    {
        if (_requireAll)
        {
            return _conditions.All(c => c.IsMet(state));
        }
        else
        {
            return _conditions.Any(c => c.IsMet(state));
        }
    }
}
