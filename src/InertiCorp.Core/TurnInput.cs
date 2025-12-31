namespace InertiCorp.Core;

/// <summary>
/// Input for a turn containing the player's chosen choice.
/// </summary>
public sealed record TurnInput
{
    /// <summary>
    /// The ID of the choice the player selected.
    /// </summary>
    public string ChosenChoiceId { get; }

    public TurnInput(string chosenChoiceId)
    {
        ChosenChoiceId = chosenChoiceId;
    }

    /// <summary>
    /// Empty input (for testing without events).
    /// </summary>
    public static TurnInput Empty => new(string.Empty);
}
