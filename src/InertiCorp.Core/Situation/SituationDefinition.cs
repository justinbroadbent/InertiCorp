using InertiCorp.Core;

namespace InertiCorp.Core.Situation;

/// <summary>
/// Defines a situation that can be triggered by playing cards.
/// Situations require CEO attention and have 4 response options.
/// </summary>
public sealed record SituationDefinition(
    string SituationId,
    string Title,
    string Description,
    string EmailSubject,
    string EmailBody,
    SituationSeverity Severity,
    IReadOnlyList<SituationResponse> Responses)
{
    /// <summary>
    /// Validates that this situation has exactly 4 responses (one of each type).
    /// </summary>
    public bool IsValid =>
        Responses.Count == 4 &&
        Responses.Any(r => r.Type == ResponseType.PC) &&
        Responses.Any(r => r.Type == ResponseType.Risk) &&
        Responses.Any(r => r.Type == ResponseType.Evil) &&
        Responses.Any(r => r.Type == ResponseType.Defer);

    /// <summary>
    /// Gets the response of the specified type.
    /// </summary>
    public SituationResponse GetResponse(ResponseType type) =>
        Responses.First(r => r.Type == type);

    /// <summary>
    /// Creates a situation with increased severity (for escalation when deferred).
    /// </summary>
    public SituationDefinition WithEscalatedSeverity()
    {
        var newSeverity = Severity < SituationSeverity.Critical
            ? Severity + 1
            : SituationSeverity.Critical;
        return this with { Severity = newSeverity };
    }

    /// <summary>
    /// Whether this situation can be deferred (Critical situations cannot).
    /// </summary>
    public bool CanDefer => Severity != SituationSeverity.Critical;

    /// <summary>
    /// Converts this situation to an EventCard for use in the Crisis phase.
    /// </summary>
    public EventCard ToEventCard()
    {
        var choices = new List<Choice>();

        foreach (var response in Responses)
        {
            var choiceId = $"{SituationId}_{response.Type}";
            var choice = response.Type switch
            {
                ResponseType.PC => Choice.WithPCCost(
                    choiceId,
                    response.Label,
                    response.PCCost ?? 2,
                    response.Outcomes),
                ResponseType.Evil => Choice.Corporate(
                    choiceId,
                    response.Label,
                    response.Outcomes,
                    response.EvilDelta > 0 ? response.EvilDelta : 2),
                ResponseType.Risk => Choice.Tiered(
                    choiceId,
                    response.Label,
                    response.Outcomes),
                ResponseType.Defer => Choice.Flat(
                    choiceId,
                    response.Label),
                _ => Choice.Tiered(
                    choiceId,
                    response.Label,
                    response.Outcomes)
            };
            choices.Add(choice);
        }

        return new EventCard(SituationId, Title, Description, choices);
    }
}
