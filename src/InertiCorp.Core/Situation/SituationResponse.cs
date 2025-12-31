namespace InertiCorp.Core.Situation;

/// <summary>
/// Outcome probability weights (must sum to 100).
/// </summary>
public sealed record OutcomeWeights(int Good, int Expected, int Bad);

/// <summary>
/// A response option for handling a situation.
/// Each situation has exactly 4 responses: PC, Risk, Evil, and Defer.
/// </summary>
public sealed record SituationResponse(
    ResponseType Type,
    string Label,
    string Description,
    int? PCCost,
    int EvilDelta,
    OutcomeProfile Outcomes)
{
    /// <summary>
    /// Gets the outcome weights for this response type.
    /// </summary>
    public static OutcomeWeights GetWeightsForType(ResponseType type) => type switch
    {
        ResponseType.PC => new OutcomeWeights(Good: 70, Expected: 20, Bad: 10),
        ResponseType.Risk => new OutcomeWeights(Good: 40, Expected: 40, Bad: 20),
        ResponseType.Evil => new OutcomeWeights(Good: 60, Expected: 20, Bad: 20),
        ResponseType.Defer => new OutcomeWeights(Good: 0, Expected: 100, Bad: 0), // No immediate outcome
        _ => new OutcomeWeights(Good: 40, Expected: 40, Bad: 20)
    };

    /// <summary>
    /// Creates a PC-type response with specified cost and outcomes.
    /// </summary>
    public static SituationResponse CreatePC(
        string label,
        string description,
        int pcCost,
        OutcomeProfile outcomes) =>
        new(ResponseType.PC, label, description, pcCost, EvilDelta: 0, outcomes);

    /// <summary>
    /// Creates a Risk-type response (roll the dice).
    /// </summary>
    public static SituationResponse CreateRisk(
        string label,
        string description,
        OutcomeProfile outcomes) =>
        new(ResponseType.Risk, label, description, PCCost: null, EvilDelta: 0, outcomes);

    /// <summary>
    /// Creates an Evil-type response with evil score increase.
    /// </summary>
    public static SituationResponse CreateEvil(
        string label,
        string description,
        int evilDelta,
        OutcomeProfile outcomes) =>
        new(ResponseType.Evil, label, description, PCCost: null, evilDelta, outcomes);

    /// <summary>
    /// Creates a Defer response (put aside for later).
    /// </summary>
    public static SituationResponse CreateDefer(
        string label = "Put this aside for now",
        string description = "This might come back later with increased severity") =>
        new(ResponseType.Defer, label, description, PCCost: null, EvilDelta: 0,
            new OutcomeProfile(
                Good: Array.Empty<IEffect>(),
                Expected: Array.Empty<IEffect>(),
                Bad: Array.Empty<IEffect>()));
}
