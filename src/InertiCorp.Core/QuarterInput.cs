namespace InertiCorp.Core;

/// <summary>
/// Input for a phase advance.
/// - Crisis phase: ChoiceId for responding to event
/// - PlayCards phase: PlayedCardId for card to play, meter exchange, or PC spending
/// - Other phases: Empty
/// </summary>
public sealed record QuarterInput(
    string ChoiceId = "",
    string PlayedCardId = "",
    bool EndPlayPhase = false,
    Meter? ExchangeMeter = null,
    int ExchangeAmount = 0,
    Meter? BoostMeter = null,
    bool SchmoozeBoard = false,
    bool ReorgHand = false,
    bool RedeemEvil = false,
    string ReplyChoiceId = "",
    bool IsRetirementChoice = false,
    bool IsQueuedCard = false)
{
    /// <summary>
    /// Empty input for phases that don't need player input.
    /// </summary>
    public static QuarterInput Empty => new();

    /// <summary>
    /// Whether this input has a choice specified (for crisis phase).
    /// </summary>
    public bool HasChoice => !string.IsNullOrEmpty(ChoiceId);

    /// <summary>
    /// Whether this input has a card to play.
    /// </summary>
    public bool HasPlayedCard => !string.IsNullOrEmpty(PlayedCardId);

    /// <summary>
    /// Whether this input has a meter exchange request.
    /// </summary>
    public bool HasMeterExchange => ExchangeMeter.HasValue && ExchangeAmount > 0;

    /// <summary>
    /// Whether this input is a request to spend PC to boost a meter.
    /// </summary>
    public bool HasMeterBoost => BoostMeter.HasValue;

    /// <summary>
    /// Whether this input is a request to schmooze the board.
    /// </summary>
    public bool HasBoardSchmooze => SchmoozeBoard;

    /// <summary>
    /// Whether this input is a request to re-org (discard hand and draw new cards).
    /// </summary>
    public bool HasReorg => ReorgHand;

    /// <summary>
    /// Whether this input is a request to redeem evil score (2 PC = -1 evil).
    /// </summary>
    public bool HasEvilRedemption => RedeemEvil;

    /// <summary>
    /// Whether this input is a reply chain response.
    /// </summary>
    public bool HasReplyChoice => !string.IsNullOrEmpty(ReplyChoiceId);

    /// <summary>
    /// Creates input for crisis choice.
    /// </summary>
    public static QuarterInput ForChoice(string choiceId) => new(ChoiceId: choiceId);

    /// <summary>
    /// Creates input for playing a card.
    /// </summary>
    public static QuarterInput ForPlayCard(string cardId, bool endPhase = false) =>
        new(PlayedCardId: cardId, EndPlayPhase: endPhase);

    /// <summary>
    /// Creates input for playing a queued card (suppresses fluff email generation).
    /// </summary>
    public static QuarterInput ForQueuedCard(string cardId) =>
        new(PlayedCardId: cardId, EndPlayPhase: false, IsQueuedCard: true);

    /// <summary>
    /// Creates input to skip playing cards and end the phase.
    /// </summary>
    public static QuarterInput EndCardPlay => new(EndPlayPhase: true);

    /// <summary>
    /// Creates input for exchanging meter value for Political Capital.
    /// Amount is how many times to apply the exchange rate.
    /// </summary>
    public static QuarterInput ForMeterExchange(Meter meter, int amount = 1) =>
        new(ExchangeMeter: meter, ExchangeAmount: amount);

    /// <summary>
    /// Creates input for spending 1 PC to boost a meter by 5 points.
    /// </summary>
    public static QuarterInput ForMeterBoost(Meter meter) =>
        new(BoostMeter: meter);

    /// <summary>
    /// Creates input for spending 2 PC to schmooze the board.
    /// Results in 1-5% favorability gain, with small chance of backfire.
    /// </summary>
    public static QuarterInput ForBoardSchmooze => new(SchmoozeBoard: true);

    /// <summary>
    /// Creates input for spending 3 PC to re-org (discard hand and draw 5 new cards).
    /// </summary>
    public static QuarterInput ForReorg => new(ReorgHand: true);

    /// <summary>
    /// Creates input for spending 2 PC to reduce evil score by 1.
    /// Represents PR campaigns, charity galas, sustainability initiatives, etc.
    /// </summary>
    public static QuarterInput ForEvilRedemption => new(RedeemEvil: true);

    /// <summary>
    /// Creates input for responding to a reply chain email.
    /// </summary>
    public static QuarterInput ForReplyChain(string choiceId) => new(ReplyChoiceId: choiceId);

    /// <summary>
    /// Whether this input is a retirement choice (victory condition).
    /// </summary>
    public bool HasRetirement => IsRetirementChoice;

    /// <summary>
    /// Creates input for CEO retirement (victory ending).
    /// </summary>
    public static QuarterInput ForRetirement => new(IsRetirementChoice: true);
}
