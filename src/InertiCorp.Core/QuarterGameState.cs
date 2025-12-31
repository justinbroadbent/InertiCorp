using InertiCorp.Core.Cards;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Email;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core;

/// <summary>
/// Immutable game state for the card-based CEO survival game.
/// </summary>
public sealed record QuarterGameState
{
    /// <summary>
    /// The seed used to initialize this game (for reproducibility).
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// The organization's current state (meters).
    /// </summary>
    public OrgState Org { get; init; }

    /// <summary>
    /// The current quarter and phase state.
    /// </summary>
    public QuarterState Quarter { get; init; }

    /// <summary>
    /// The CEO's tenure, pressure, and favorability state.
    /// </summary>
    public CEOState CEO { get; init; }

    /// <summary>
    /// The three phase-specific decks (for crisis events).
    /// </summary>
    public DeckSet EventDecks { get; init; }

    /// <summary>
    /// The player's playable card deck.
    /// </summary>
    public CardDeck CardDeck { get; init; }

    /// <summary>
    /// The player's current hand of playable cards.
    /// </summary>
    public CardHand Hand { get; init; }

    /// <summary>
    /// The player's email inbox.
    /// </summary>
    public Inbox Inbox { get; init; }

    /// <summary>
    /// The current crisis event card (null when not in Crisis phase).
    /// </summary>
    public EventCard? CurrentCrisis { get; init; }

    /// <summary>
    /// The current board directive for this quarter.
    /// </summary>
    public BoardDirective? CurrentDirective { get; init; }

    /// <summary>
    /// Cards played this quarter (max 3).
    /// </summary>
    public IReadOnlyList<PlayableCard> CardsPlayedThisQuarter { get; init; }

    /// <summary>
    /// Resources beyond core meters (Political Capital, etc.).
    /// </summary>
    public ResourceState Resources { get; init; }

    /// <summary>
    /// Active and resolved crises.
    /// </summary>
    public CrisisState Crises { get; init; }

    /// <summary>
    /// ID of the thread with an active reply chain (null when no chain active).
    /// </summary>
    public string? ActiveReplyChainThreadId { get; init; }

    /// <summary>
    /// Situations queued to fire in upcoming quarters.
    /// </summary>
    public IReadOnlyList<PendingSituation> PendingSituations { get; init; }

    /// <summary>
    /// Situations the player deferred (may resurface with escalated severity).
    /// </summary>
    public IReadOnlyList<PendingSituation> DeferredSituations { get; init; }

    /// <summary>
    /// Projects eligible for follow-up events (played in recent quarters).
    /// </summary>
    public IReadOnlyList<PendingFollowUp> PendingFollowUps { get; init; }

    /// <summary>
    /// Maximum cards that can be played per quarter.
    /// </summary>
    public const int MaxCardsPerQuarter = 3;

    /// <summary>
    /// Maximum deferred situations before oldest auto-escalates.
    /// </summary>
    public const int MaxDeferredSituations = 5;

    /// <summary>
    /// Gets the thread with an active reply chain (null when no chain active).
    /// </summary>
    public EmailThread? ActiveReplyChainThread =>
        ActiveReplyChainThreadId is not null
            ? Inbox.GetThread(ActiveReplyChainThreadId)
            : null;

    /// <summary>
    /// Whether there is an active reply chain awaiting player response.
    /// </summary>
    public bool HasActiveReplyChain => ActiveReplyChainThreadId is not null;

    private QuarterGameState(
        int seed,
        OrgState org,
        QuarterState quarter,
        CEOState ceo,
        DeckSet eventDecks,
        CardDeck cardDeck,
        CardHand hand,
        Inbox inbox,
        EventCard? currentCrisis,
        BoardDirective? currentDirective,
        IReadOnlyList<PlayableCard> cardsPlayedThisQuarter,
        ResourceState resources,
        CrisisState crises,
        string? activeReplyChainThreadId = null,
        IReadOnlyList<PendingSituation>? pendingSituations = null,
        IReadOnlyList<PendingSituation>? deferredSituations = null,
        IReadOnlyList<PendingFollowUp>? pendingFollowUps = null)
    {
        Seed = seed;
        Org = org;
        Quarter = quarter;
        CEO = ceo;
        EventDecks = eventDecks;
        CardDeck = cardDeck;
        Hand = hand;
        Inbox = inbox;
        CurrentCrisis = currentCrisis;
        CurrentDirective = currentDirective;
        CardsPlayedThisQuarter = cardsPlayedThisQuarter;
        Resources = resources;
        Crises = crises;
        ActiveReplyChainThreadId = activeReplyChainThreadId;
        PendingSituations = pendingSituations ?? Array.Empty<PendingSituation>();
        DeferredSituations = deferredSituations ?? Array.Empty<PendingSituation>();
        PendingFollowUps = pendingFollowUps ?? Array.Empty<PendingFollowUp>();
    }

    /// <summary>
    /// Creates a new game with the given seed and decks.
    /// </summary>
    public static QuarterGameState NewGame(int seed, DeckSet eventDeckSet, IReadOnlyList<PlayableCard> playableCards)
    {
        var rng = new SeededRng(seed);

        // Shuffle event decks
        eventDeckSet.CrisisDeck.Shuffle(rng);
        eventDeckSet.BoardDeck.Shuffle(rng);
        eventDeckSet.ProjectDeck.Shuffle(rng);

        // Create and shuffle playable card deck
        var cardDeck = CardDeck.Create(playableCards, rng);

        // Draw initial hand (5 cards)
        var (newCardDeck, initialCards) = cardDeck.DrawMultiple(CardHand.MaxHandSize, rng);
        var hand = CardHand.Empty.WithCardsAdded(initialCards);

        // Generate initial board directive
        var directive = BoardDirective.Generate(CEOState.Initial.BoardPressureLevel, rng);

        // Create welcome email from the board
        var emailGenerator = new EmailGenerator(seed);
        var welcomeThread = emailGenerator.CreateWelcomeThread();
        var initialInbox = Inbox.Empty.WithThreadAdded(welcomeThread);

        return new QuarterGameState(
            seed: seed,
            org: OrgState.Default,
            quarter: QuarterState.Initial,
            ceo: CEOState.Initial,
            eventDecks: eventDeckSet,
            cardDeck: newCardDeck,
            hand: hand,
            inbox: initialInbox,
            currentCrisis: null,
            currentDirective: directive,
            cardsPlayedThisQuarter: Array.Empty<PlayableCard>(),
            resources: ResourceState.Initial,
            crises: CrisisState.Empty
        );
    }

    /// <summary>
    /// Creates a new game with the given seed and deck set (legacy overload).
    /// </summary>
    public static QuarterGameState NewGame(int seed, DeckSet deckSet)
    {
        return NewGame(seed, deckSet, Array.Empty<PlayableCard>());
    }

    /// <summary>
    /// Whether the player can still play cards this quarter.
    /// </summary>
    public bool CanPlayCard => CardsPlayedThisQuarter.Count < MaxCardsPerQuarter && !Hand.IsEmpty;

    /// <summary>
    /// Whether the player can afford to play the next card (has enough PC).
    /// </summary>
    public bool CanAffordNextCard => Resources.PoliticalCapital >= GetNextCardPCCost();

    /// <summary>
    /// Gets the PC cost for playing a card at the given position (0-indexed).
    /// Playing cards is free - PC is only earned from restraint (not playing cards).
    /// </summary>
    public static int GetCardPCCost(int _) => 0;

    /// <summary>
    /// Gets the additional bad outcome risk modifier for a card at the given position (0-indexed).
    /// Card 1: 0%, Card 2: +10%, Card 3: +20%
    /// </summary>
    public static int GetCardRiskModifier(int position) => position switch
    {
        0 => 0,
        1 => 10,
        2 => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(position), $"Invalid card position: {position}")
    };

    /// <summary>
    /// Gets the PC cost for playing the next card (based on current cards played).
    /// </summary>
    public int GetNextCardPCCost() => GetCardPCCost(CardsPlayedThisQuarter.Count);

    /// <summary>
    /// Gets the risk modifier for playing the next card (based on current cards played).
    /// </summary>
    public int GetNextCardRiskModifier() => GetCardRiskModifier(CardsPlayedThisQuarter.Count);

    /// <summary>
    /// Calculates affinity synergy bonus for a card based on matching affinities already played.
    /// 1 matching affinity already played: +5% good outcome chance
    /// 2+ matching affinities already played: +10% good outcome chance
    /// </summary>
    public int GetAffinitySynergyBonus(PlayableCard card)
    {
        if (card.MeterAffinity is null) return 0;

        int matchingCount = CardsPlayedThisQuarter
            .Count(c => c.MeterAffinity == card.MeterAffinity);

        return matchingCount switch
        {
            >= 2 => 10,
            1 => 5,
            _ => 0
        };
    }

    /// <summary>
    /// Returns a new state with updated org.
    /// </summary>
    public QuarterGameState WithOrg(OrgState org) =>
        new(Seed, org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated quarter.
    /// </summary>
    public QuarterGameState WithQuarter(QuarterState quarter) =>
        new(Seed, Org, quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated CEO state.
    /// </summary>
    public QuarterGameState WithCEO(CEOState ceo) =>
        new(Seed, Org, Quarter, ceo, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated event decks.
    /// </summary>
    public QuarterGameState WithEventDecks(DeckSet decks) =>
        new(Seed, Org, Quarter, CEO, decks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated card deck.
    /// </summary>
    public QuarterGameState WithCardDeck(CardDeck deck) =>
        new(Seed, Org, Quarter, CEO, EventDecks, deck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated hand.
    /// </summary>
    public QuarterGameState WithHand(CardHand hand) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated inbox.
    /// </summary>
    public QuarterGameState WithInbox(Inbox inbox) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated crisis card.
    /// </summary>
    public QuarterGameState WithCurrentCrisis(EventCard? card) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, card, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated directive.
    /// </summary>
    public QuarterGameState WithCurrentDirective(BoardDirective? directive) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, directive, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with a card added to played cards.
    /// </summary>
    public QuarterGameState WithCardPlayed(PlayableCard card) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective,
            CardsPlayedThisQuarter.Append(card).ToList(), Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with played cards cleared (for new quarter).
    /// </summary>
    public QuarterGameState WithPlayedCardsCleared() =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective,
            Array.Empty<PlayableCard>(), Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated resources.
    /// </summary>
    public QuarterGameState WithResources(ResourceState resources) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated crises.
    /// </summary>
    public QuarterGameState WithCrises(CrisisState crises) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with the active reply chain set or cleared.
    /// </summary>
    public QuarterGameState WithActiveReplyChain(string? threadId) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, threadId, PendingSituations, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Convenience: returns a new state with Political Capital changed by delta.
    /// </summary>
    public QuarterGameState WithPoliticalCapitalChange(int delta) =>
        WithResources(Resources.WithPoliticalCapitalChange(delta));

    /// <summary>
    /// Returns a new state with a situation added to the pending queue.
    /// </summary>
    public QuarterGameState WithSituationQueued(PendingSituation situation) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId,
            PendingSituations.Append(situation).ToList(), DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with a situation moved to the deferred queue.
    /// </summary>
    public QuarterGameState WithSituationDeferred(PendingSituation situation)
    {
        var pending = PendingSituations.Where(s => s.SituationId != situation.SituationId).ToList();
        var deferred = DeferredSituations.Append(situation.WithDeferred(Quarter.QuarterNumber)).ToList();

        // If we exceed max deferred, oldest auto-escalates (removed from deferred, would fire next quarter)
        if (deferred.Count > MaxDeferredSituations)
        {
            var oldest = deferred.OrderBy(s => s.QueuedAtQuarter).First();
            deferred = deferred.Where(s => s != oldest).ToList();
            pending = pending.Append(oldest with { ScheduledQuarter = Quarter.QuarterNumber + 1 }).ToList();
        }

        return new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, pending, deferred, PendingFollowUps);
    }

    /// <summary>
    /// Returns a new state with a situation removed from the pending queue.
    /// </summary>
    public QuarterGameState WithSituationResolved(string situationId) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId,
            PendingSituations.Where(s => s.SituationId != situationId).ToList(), DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated pending situations list.
    /// </summary>
    public QuarterGameState WithPendingSituations(IReadOnlyList<PendingSituation> pending) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, pending, DeferredSituations, PendingFollowUps);

    /// <summary>
    /// Returns a new state with updated deferred situations list.
    /// </summary>
    public QuarterGameState WithDeferredSituations(IReadOnlyList<PendingSituation> deferred) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, deferred, PendingFollowUps);

    /// <summary>
    /// Returns a new state with a follow-up added to the queue.
    /// </summary>
    public QuarterGameState WithFollowUpQueued(PendingFollowUp followUp) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations,
            PendingFollowUps.Append(followUp).ToList());

    /// <summary>
    /// Returns a new state with a follow-up removed from the queue.
    /// </summary>
    public QuarterGameState WithFollowUpRemoved(string cardId) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations,
            PendingFollowUps.Where(f => f.CardId != cardId).ToList());

    /// <summary>
    /// Returns a new state with expired follow-ups removed.
    /// </summary>
    public QuarterGameState WithExpiredFollowUpsRemoved() =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations,
            PendingFollowUps.Where(f => f.IsEligible(Quarter.QuarterNumber)).ToList());

    /// <summary>
    /// Returns a new state with updated pending follow-ups list.
    /// </summary>
    public QuarterGameState WithPendingFollowUps(IReadOnlyList<PendingFollowUp> followUps) =>
        new(Seed, Org, Quarter, CEO, EventDecks, CardDeck, Hand, Inbox, CurrentCrisis, CurrentDirective, CardsPlayedThisQuarter, Resources, Crises, ActiveReplyChainThreadId, PendingSituations, DeferredSituations, followUps);
}
