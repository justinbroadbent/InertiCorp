namespace InertiCorp.Core;

/// <summary>
/// An event card that presents a situation and choices to the player.
/// </summary>
public sealed record EventCard
{
    public string EventId { get; }
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<Choice> Choices { get; }

    public EventCard(
        string EventId,
        string Title,
        string Description,
        IReadOnlyList<Choice> Choices)
    {
        if (Choices.Count < 2)
            throw new ArgumentException("EventCard must have at least 2 choices.", nameof(Choices));

        if (Choices.Count > 4)
            throw new ArgumentException("EventCard must have at most 4 choices.", nameof(Choices));

        var choiceIds = Choices.Select(c => c.ChoiceId).ToHashSet();
        if (choiceIds.Count != Choices.Count)
            throw new ArgumentException("Choice IDs must be unique within an EventCard.", nameof(Choices));

        this.EventId = EventId;
        this.Title = Title;
        this.Description = Description;
        this.Choices = Choices;
    }

    /// <summary>
    /// Gets a choice by its ID.
    /// </summary>
    /// <exception cref="ArgumentException">If the choice ID is not found.</exception>
    public Choice GetChoice(string choiceId)
    {
        var choice = Choices.FirstOrDefault(c => c.ChoiceId == choiceId);
        if (choice is null)
            throw new ArgumentException($"Choice '{choiceId}' not found in event '{EventId}'.", nameof(choiceId));
        return choice;
    }
}
