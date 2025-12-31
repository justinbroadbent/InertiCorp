using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Email;
using InertiCorp.Core.Quarter;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Tests;

/// <summary>
/// Automated game simulator for the new QuarterLoopEngine with situation tracking.
/// </summary>
public class QuarterLoopSimulator
{
    public enum Strategy
    {
        Random,           // Random choices
        Conservative,     // Avoid risks, spend PC on situations
        Aggressive,       // Take risks, use evil options
        Balanced,         // Moderate approach
        DeferHeavy,       // Defer situations when possible
        PCHoarder         // Never spend PC, always roll dice
    }

    public record SituationMetrics
    {
        public int SituationsTriggered { get; init; }
        public int CrisisDraws { get; init; }
        public int SituationsDeferred { get; init; }
        public int SituationsResurfaced { get; init; }
        public int SituationsFaded { get; init; }
        public int PCSpentOnSituations { get; init; }
        public int EvilGainedFromSituations { get; init; }
        public Dictionary<ResponseType, int> ResponseTypeUsed { get; init; } = new();
        public Dictionary<SituationSeverity, int> SituationsBySeverity { get; init; } = new();
        public Dictionary<OutcomeTier, int> SituationOutcomes { get; init; } = new();
    }

    public record GameResult
    {
        public int QuartersSurvived { get; init; }
        public int TotalProfit { get; init; }
        public int FinalFavorability { get; init; }
        public int FinalEvilScore { get; init; }
        public int FinalPC { get; init; }
        public OrgState FinalOrg { get; init; } = new OrgState(50, 50, 50, 50, 50);
        public int ProjectsSelected { get; init; }
        public int ReorgsUsed { get; init; }
        public string DeathReason { get; init; } = "";
        public Strategy UsedStrategy { get; init; }
        public int Seed { get; init; }
        public SituationMetrics Situations { get; init; } = new();
    }

    public record SimulationStats
    {
        public int GamesPlayed { get; init; }
        public Strategy Strategy { get; init; }

        // Survival metrics
        public double AvgQuartersSurvived { get; init; }
        public double MedianQuartersSurvived { get; init; }
        public int MaxQuartersSurvived { get; init; }
        public int MinQuartersSurvived { get; init; }
        public double SurvivalRate5Q { get; init; }
        public double SurvivalRate10Q { get; init; }
        public double SurvivalRate20Q { get; init; }

        // Performance metrics
        public double AvgTotalProfit { get; init; }
        public double AvgFinalFavorability { get; init; }
        public double AvgEvilScore { get; init; }
        public double AvgFinalPC { get; init; }
        public double AvgProjectsPerQuarter { get; init; }

        // Situation metrics
        public double AvgSituationsPerGame { get; init; }
        public double AvgCrisisDrawsPerGame { get; init; }
        public double AvgDeferralsPerGame { get; init; }
        public double AvgResurfacesPerGame { get; init; }
        public double AvgFadesPerGame { get; init; }
        public double AvgPCSpentOnSituations { get; init; }
        public double AvgEvilFromSituations { get; init; }
        public Dictionary<ResponseType, double> AvgResponseTypeUsage { get; init; } = new();
        public Dictionary<SituationSeverity, double> AvgSituationsBySeverity { get; init; } = new();
        public Dictionary<OutcomeTier, double> AvgSituationOutcomes { get; init; } = new();

        // Death reasons
        public Dictionary<string, int> DeathReasons { get; init; } = new();
        public int[] QuarterDistribution { get; init; } = Array.Empty<int>();
    }

    private readonly Strategy _strategy;

    public QuarterLoopSimulator(Strategy strategy = Strategy.Balanced)
    {
        _strategy = strategy;
    }

    /// <summary>
    /// Runs a single game and returns the result.
    /// </summary>
    public GameResult PlayGame(int seed)
    {
        var rng = new SeededRng(seed);
        var deckSet = GameContent.CreateDeckSet();
        var playableCards = GameContent.PlayableCardDeck;

        // Initialize game state
        var loopState = QuarterLoopState.Initial;
        var org = new OrgState(50, 50, 50, 50, 50);  // Balanced starting meters
        var ceo = CEOState.Initial;
        var resources = ResourceState.Initial;
        var crises = CrisisState.Empty;
        var deck = new CardDeck(playableCards.ToList(), Array.Empty<PlayableCard>());
        var (initialDeck, initialCards) = deck.DrawMultiple(CardHand.MaxHandSize, rng);
        var hand = CardHand.Empty.WithCardsAdded(initialCards);
        deck = initialDeck;
        var inbox = Inbox.Empty;

        var pendingSituations = new List<PendingSituation>();
        var deferredSituations = new List<PendingSituation>();

        // Track metrics
        int projectsSelected = 0;
        int reorgsUsed = 0;
        string deathReason = "";
        var situationMetrics = new SituationMetrics
        {
            ResponseTypeUsed = new Dictionary<ResponseType, int>
            {
                [ResponseType.PC] = 0,
                [ResponseType.Risk] = 0,
                [ResponseType.Evil] = 0,
                [ResponseType.Defer] = 0
            },
            SituationsBySeverity = new Dictionary<SituationSeverity, int>
            {
                [SituationSeverity.Minor] = 0,
                [SituationSeverity.Moderate] = 0,
                [SituationSeverity.Major] = 0,
                [SituationSeverity.Critical] = 0
            },
            SituationOutcomes = new Dictionary<OutcomeTier, int>
            {
                [OutcomeTier.Good] = 0,
                [OutcomeTier.Expected] = 0,
                [OutcomeTier.Bad] = 0
            }
        };
        int situationsTriggered = 0;
        int crisisDraws = 0;
        int situationsDeferred = 0;
        int situationsResurfaced = 0;
        int situationsFaded = 0;
        int pcSpentOnSituations = 0;
        int evilGainedFromSituations = 0;

        const int MaxQuarters = 100;

        while (!ceo.IsOusted && loopState.QuarterNumber <= MaxQuarters)
        {
            try
            {
                // Projects phase
                if (loopState.Phase == QuarterPhase.Projects)
                {
                    var choice = DecideProjectsPhase(hand, loopState, rng);

                    if (choice is ProjectsPhaseChoice.ReorgAndSelectOne)
                        reorgsUsed++;

                    var result = QuarterLoopEngine.ProcessProjectsPhase(
                        loopState, org, ceo, resources, crises, hand, deck, inbox,
                        choice, rng, seed, pendingSituations, deferredSituations);

                    projectsSelected += result.NewLoopState.ProjectCount;

                    // Track situations triggered from cards
                    var newPendingCount = result.PendingSituations.Count;
                    var oldPendingCount = pendingSituations.Count;
                    if (newPendingCount > oldPendingCount)
                    {
                        var triggeredCount = newPendingCount - oldPendingCount;
                        situationsTriggered += triggeredCount;
                    }

                    loopState = result.NewLoopState;
                    org = result.NewOrg;
                    ceo = result.NewCEO;
                    resources = result.NewResources;
                    hand = result.NewHand;
                    deck = result.NewCardDeck;
                    inbox = result.NewInbox;
                    pendingSituations = result.PendingSituations.ToList();
                    deferredSituations = result.DeferredSituations.ToList();
                }

                // Situation phase
                while (loopState.Phase == QuarterPhase.Situation)
                {
                    // Check if there's a situation to handle
                    var activeSituation = loopState.ActiveSituation
                        ?? pendingSituations.FirstOrDefault(s => s.IsDueAt(loopState.QuarterNumber));

                    SituationPhaseChoice situationChoice;

                    if (activeSituation is null)
                    {
                        situationChoice = new SituationPhaseChoice.Skip();
                    }
                    else
                    {
                        var situationDef = SituationContent.Get(activeSituation.SituationId);
                        situationChoice = DecideSituationPhase(activeSituation, situationDef, resources, ceo, rng);

                        // Track metrics for this situation
                        if (situationDef is not null)
                        {
                            situationMetrics.SituationsBySeverity[situationDef.Severity]++;

                            if (activeSituation.OriginCardId == "RANDOM_CRISIS")
                                crisisDraws++;
                        }

                        // Track response type
                        var responseType = situationChoice switch
                        {
                            SituationPhaseChoice.SpendPC => ResponseType.PC,
                            SituationPhaseChoice.RollDice => ResponseType.Risk,
                            SituationPhaseChoice.EvilOption => ResponseType.Evil,
                            SituationPhaseChoice.Defer => ResponseType.Defer,
                            _ => ResponseType.Risk
                        };

                        if (responseType != ResponseType.Defer || (situationDef?.CanDefer ?? false))
                        {
                            situationMetrics.ResponseTypeUsed[responseType]++;
                        }

                        if (responseType == ResponseType.Defer && (situationDef?.CanDefer ?? false))
                            situationsDeferred++;

                        if (responseType == ResponseType.PC && situationDef is not null)
                        {
                            var pcResponse = situationDef.GetResponse(ResponseType.PC);
                            if (pcResponse?.PCCost is not null && resources.PoliticalCapital >= pcResponse.PCCost)
                                pcSpentOnSituations += pcResponse.PCCost.Value;
                        }

                        if (responseType == ResponseType.Evil)
                        {
                            evilGainedFromSituations += situationDef?.GetResponse(ResponseType.Evil)?.EvilDelta ?? 1;
                        }
                    }

                    var pendingBefore = pendingSituations.Count;
                    var deferredBefore = deferredSituations.Count;

                    var result = QuarterLoopEngine.ProcessSituationPhase(
                        loopState, org, ceo, resources, crises, hand, deck, inbox,
                        situationChoice, rng, seed, pendingSituations, deferredSituations);

                    // Track resurfaced and faded
                    if (result.DeferredSituations.Count < deferredBefore &&
                        result.PendingSituations.Count > pendingBefore)
                    {
                        situationsResurfaced++;
                    }

                    if (result.DeferredSituations.Count < deferredBefore &&
                        result.PendingSituations.Count <= pendingBefore)
                    {
                        situationsFaded++;
                    }

                    loopState = result.NewLoopState;
                    org = result.NewOrg;
                    ceo = result.NewCEO;
                    resources = result.NewResources;
                    hand = result.NewHand;
                    deck = result.NewCardDeck;
                    inbox = result.NewInbox;
                    pendingSituations = result.PendingSituations.ToList();
                    deferredSituations = result.DeferredSituations.ToList();

                    if (result.GameOver)
                        break;
                }

                // Board Meeting phase
                if (loopState.Phase == QuarterPhase.BoardMeeting)
                {
                    var boardChoice = DecideBoardMeetingPhase(resources, ceo, rng);

                    var result = QuarterLoopEngine.ProcessBoardMeetingPhase(
                        loopState, org, ceo, resources, crises, hand, deck, inbox,
                        boardChoice, rng, seed, pendingSituations, deferredSituations);

                    loopState = result.NewLoopState;
                    org = result.NewOrg;
                    ceo = result.NewCEO;
                    resources = result.NewResources;
                    hand = result.NewHand;
                    deck = result.NewCardDeck;
                    inbox = result.NewInbox;
                    pendingSituations = result.PendingSituations.ToList();
                    deferredSituations = result.DeferredSituations.ToList();

                    if (result.GameOver)
                    {
                        deathReason = DetermineDeathReason(org, ceo, loopState);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                deathReason = $"Error: {ex.Message}";
                break;
            }
        }

        if (loopState.QuarterNumber > MaxQuarters)
        {
            deathReason = "Survived max quarters (victory!)";
        }

        return new GameResult
        {
            QuartersSurvived = loopState.QuarterNumber - 1,
            TotalProfit = ceo.TotalProfit,
            FinalFavorability = ceo.BoardFavorability,
            FinalEvilScore = ceo.EvilScore,
            FinalPC = resources.PoliticalCapital,
            FinalOrg = org,
            ProjectsSelected = projectsSelected,
            ReorgsUsed = reorgsUsed,
            DeathReason = deathReason,
            UsedStrategy = _strategy,
            Seed = seed,
            Situations = new SituationMetrics
            {
                SituationsTriggered = situationsTriggered,
                CrisisDraws = crisisDraws,
                SituationsDeferred = situationsDeferred,
                SituationsResurfaced = situationsResurfaced,
                SituationsFaded = situationsFaded,
                PCSpentOnSituations = pcSpentOnSituations,
                EvilGainedFromSituations = evilGainedFromSituations,
                ResponseTypeUsed = situationMetrics.ResponseTypeUsed,
                SituationsBySeverity = situationMetrics.SituationsBySeverity,
                SituationOutcomes = situationMetrics.SituationOutcomes
            }
        };
    }

    private ProjectsPhaseChoice DecideProjectsPhase(CardHand hand, QuarterLoopState loopState, IRng rng)
    {
        var cards = hand.Cards.ToList();

        // Not enough cards for normal selection
        if (cards.Count < 3)
        {
            // Reorg and select one
            var card = cards[rng.NextInt(0, cards.Count)];
            return new ProjectsPhaseChoice.ReorgAndSelectOne(card.CardId);
        }

        return _strategy switch
        {
            Strategy.Random => DecideProjectsRandom(cards, rng),
            Strategy.Conservative => DecideProjectsConservative(cards, rng),
            Strategy.Aggressive => DecideProjectsAggressive(cards, rng),
            Strategy.Balanced => DecideProjectsBalanced(cards, rng),
            Strategy.DeferHeavy => DecideProjectsBalanced(cards, rng),
            Strategy.PCHoarder => DecideProjectsBalanced(cards, rng),
            _ => DecideProjectsRandom(cards, rng)
        };
    }

    private ProjectsPhaseChoice DecideProjectsRandom(List<PlayableCard> cards, IRng rng)
    {
        // Shuffle and pick first 3
        var shuffled = cards.OrderBy(_ => rng.NextInt(0, 1000)).Take(3).ToList();
        return new ProjectsPhaseChoice.SelectProjects(shuffled.Select(c => c.CardId).ToList());
    }

    private ProjectsPhaseChoice DecideProjectsConservative(List<PlayableCard> cards, IRng rng)
    {
        // Prefer non-corporate, low-risk cards
        var defaultOrg = new OrgState(50, 50, 50, 50, 50);
        var ranked = cards
            .OrderBy(c => c.CorporateIntensity)
            .ThenByDescending(c => c.GetAffinityModifier(defaultOrg))
            .Take(3)
            .ToList();
        return new ProjectsPhaseChoice.SelectProjects(ranked.Select(c => c.CardId).ToList());
    }

    private ProjectsPhaseChoice DecideProjectsAggressive(List<PlayableCard> cards, IRng rng)
    {
        // Prefer revenue and corporate cards
        var ranked = cards
            .OrderByDescending(c => c.Category == CardCategory.Revenue ? 10 : 0)
            .ThenByDescending(c => c.CorporateIntensity)
            .Take(3)
            .ToList();
        return new ProjectsPhaseChoice.SelectProjects(ranked.Select(c => c.CardId).ToList());
    }

    private ProjectsPhaseChoice DecideProjectsBalanced(List<PlayableCard> cards, IRng rng)
    {
        // Mix of revenue and safe cards
        var defaultOrg = new OrgState(50, 50, 50, 50, 50);
        var ranked = cards
            .OrderByDescending(c => c.Category == CardCategory.Revenue ? 5 : 0)
            .ThenByDescending(c => c.GetAffinityModifier(defaultOrg))
            .ThenBy(c => c.CorporateIntensity)
            .Take(3)
            .ToList();
        return new ProjectsPhaseChoice.SelectProjects(ranked.Select(c => c.CardId).ToList());
    }

    private SituationPhaseChoice DecideSituationPhase(
        PendingSituation situation,
        SituationDefinition? situationDef,
        ResourceState resources,
        CEOState ceo,
        IRng rng)
    {
        if (situationDef is null)
            return new SituationPhaseChoice.RollDice();

        // Account for severity escalation from previous deferrals
        var escalatedDef = situationDef;
        for (int i = 0; i < situation.DeferCount; i++)
        {
            escalatedDef = escalatedDef.WithEscalatedSeverity();
        }

        // Can't defer critical situations (check escalated severity)
        var canDefer = escalatedDef.CanDefer;
        var pcResponse = escalatedDef.GetResponse(ResponseType.PC);
        var canAffordPC = pcResponse?.PCCost is null || resources.PoliticalCapital >= pcResponse.PCCost;

        return _strategy switch
        {
            Strategy.Random => DecideSituationRandom(canDefer, canAffordPC, rng),
            Strategy.Conservative => DecideSituationConservative(canDefer, canAffordPC, escalatedDef, resources),
            Strategy.Aggressive => DecideSituationAggressive(canDefer, canAffordPC, escalatedDef, ceo, rng),
            Strategy.Balanced => DecideSituationBalanced(canDefer, canAffordPC, escalatedDef, resources, ceo),
            Strategy.DeferHeavy => DecideSituationDeferHeavy(canDefer, canAffordPC, escalatedDef, rng),
            Strategy.PCHoarder => DecideSituationPCHoarder(canDefer, rng),
            _ => new SituationPhaseChoice.RollDice()
        };
    }

    private SituationPhaseChoice DecideSituationRandom(bool canDefer, bool canAffordPC, IRng rng)
    {
        var options = new List<SituationPhaseChoice>
        {
            new SituationPhaseChoice.RollDice(),
            new SituationPhaseChoice.EvilOption()
        };

        if (canAffordPC) options.Add(new SituationPhaseChoice.SpendPC());
        if (canDefer) options.Add(new SituationPhaseChoice.Defer());

        var choice = options[rng.NextInt(0, options.Count)];
        // Guard against trying to defer critical situations
        if (choice is SituationPhaseChoice.Defer && !canDefer)
            return new SituationPhaseChoice.RollDice();
        return choice;
    }

    private SituationPhaseChoice DecideSituationConservative(
        bool canDefer, bool canAffordPC, SituationDefinition situationDef, ResourceState resources)
    {
        // Always spend PC if possible for serious situations
        if (canAffordPC && situationDef.Severity >= SituationSeverity.Moderate)
            return new SituationPhaseChoice.SpendPC();

        // Defer minor situations if possible (and situation allows it)
        if (canDefer && situationDef.CanDefer && situationDef.Severity == SituationSeverity.Minor)
            return new SituationPhaseChoice.Defer();

        // Otherwise roll dice (never use evil option)
        return new SituationPhaseChoice.RollDice();
    }

    private SituationPhaseChoice DecideSituationAggressive(
        bool canDefer, bool canAffordPC, SituationDefinition situationDef, CEOState ceo, IRng rng)
    {
        // Use evil option if evil score is still low
        if (ceo.EvilScore < 10)
            return new SituationPhaseChoice.EvilOption();

        // Otherwise roll dice for the gamble
        return new SituationPhaseChoice.RollDice();
    }

    private SituationPhaseChoice DecideSituationBalanced(
        bool canDefer, bool canAffordPC, SituationDefinition situationDef, ResourceState resources, CEOState ceo)
    {
        // Spend PC on major/critical situations
        if (canAffordPC && situationDef.Severity >= SituationSeverity.Major)
            return new SituationPhaseChoice.SpendPC();

        // Use evil option if evil score is very low
        if (ceo.EvilScore < 5)
            return new SituationPhaseChoice.EvilOption();

        // Defer minor situations if allowed
        if (canDefer && situationDef.CanDefer && situationDef.Severity == SituationSeverity.Minor)
            return new SituationPhaseChoice.Defer();

        // Default to rolling dice
        return new SituationPhaseChoice.RollDice();
    }

    private SituationPhaseChoice DecideSituationDeferHeavy(
        bool canDefer, bool canAffordPC, SituationDefinition situationDef, IRng rng)
    {
        // Always defer if possible (and situation allows it)
        if (canDefer && situationDef.CanDefer)
            return new SituationPhaseChoice.Defer();

        // For critical situations (can't defer), spend PC if possible
        if (canAffordPC)
            return new SituationPhaseChoice.SpendPC();

        // Otherwise roll dice
        return new SituationPhaseChoice.RollDice();
    }

    private SituationPhaseChoice DecideSituationPCHoarder(bool canDefer, IRng rng)
    {
        // Never spend PC - always roll dice or use evil
        if (rng.NextInt(0, 100) < 30)
            return new SituationPhaseChoice.EvilOption();

        return new SituationPhaseChoice.RollDice();
    }

    private BoardMeetingChoice DecideBoardMeetingPhase(ResourceState resources, CEOState ceo, IRng rng)
    {
        // For now, always accept - board influence not implemented in detail
        return new BoardMeetingChoice.Accept();
    }

    private string DetermineDeathReason(OrgState org, CEOState ceo, QuarterLoopState loopState)
    {
        var reasons = new List<string>();

        if (ceo.BoardFavorability < 20) reasons.Add("LowFavorability");
        if (ceo.EvilScore >= 15) reasons.Add("HighEvil");
        if (org.Runway < 20) reasons.Add("LowRunway");
        if (org.Morale < 20) reasons.Add("LowMorale");
        if (org.Delivery < 20) reasons.Add("LowDelivery");
        if (org.Governance < 20) reasons.Add("LowGovernance");
        if (org.Alignment < 20) reasons.Add("LowAlignment");

        if (loopState.BoardReview is not null)
        {
            if (loopState.ConsecutivePoorQuarters >= 2)
                reasons.Add("ConsecutivePoorRatings");
        }

        if (reasons.Count == 0) reasons.Add("BoardVote");

        return string.Join("+", reasons);
    }

    /// <summary>
    /// Runs multiple games and compiles statistics.
    /// </summary>
    public static SimulationStats RunSimulation(int gameCount, Strategy strategy, int baseSeed = 42)
    {
        var results = new List<GameResult>();
        var rng = new SeededRng(baseSeed);

        for (int i = 0; i < gameCount; i++)
        {
            var gameSeed = rng.NextInt(0, int.MaxValue);
            var simulator = new QuarterLoopSimulator(strategy);
            results.Add(simulator.PlayGame(gameSeed));
        }

        var quarters = results.Select(r => r.QuartersSurvived).OrderBy(x => x).ToList();
        var deathReasons = results
            .Where(r => !string.IsNullOrEmpty(r.DeathReason))
            .GroupBy(r => r.DeathReason)
            .ToDictionary(g => g.Key, g => g.Count());

        // Quarter distribution histogram (buckets of 4 quarters = 1 year)
        var maxQ = quarters.Count > 0 ? quarters.Max() : 0;
        var bucketCount = (maxQ / 4) + 1;
        var distribution = new int[Math.Max(bucketCount, 8)];
        foreach (var q in quarters)
        {
            var bucket = Math.Min(q / 4, distribution.Length - 1);
            distribution[bucket]++;
        }

        // Calculate average response type usage
        var avgResponseTypeUsage = new Dictionary<ResponseType, double>
        {
            [ResponseType.PC] = results.Average(r => r.Situations.ResponseTypeUsed.GetValueOrDefault(ResponseType.PC, 0)),
            [ResponseType.Risk] = results.Average(r => r.Situations.ResponseTypeUsed.GetValueOrDefault(ResponseType.Risk, 0)),
            [ResponseType.Evil] = results.Average(r => r.Situations.ResponseTypeUsed.GetValueOrDefault(ResponseType.Evil, 0)),
            [ResponseType.Defer] = results.Average(r => r.Situations.ResponseTypeUsed.GetValueOrDefault(ResponseType.Defer, 0))
        };

        var avgSituationsBySeverity = new Dictionary<SituationSeverity, double>
        {
            [SituationSeverity.Minor] = results.Average(r => r.Situations.SituationsBySeverity.GetValueOrDefault(SituationSeverity.Minor, 0)),
            [SituationSeverity.Moderate] = results.Average(r => r.Situations.SituationsBySeverity.GetValueOrDefault(SituationSeverity.Moderate, 0)),
            [SituationSeverity.Major] = results.Average(r => r.Situations.SituationsBySeverity.GetValueOrDefault(SituationSeverity.Major, 0)),
            [SituationSeverity.Critical] = results.Average(r => r.Situations.SituationsBySeverity.GetValueOrDefault(SituationSeverity.Critical, 0))
        };

        return new SimulationStats
        {
            GamesPlayed = gameCount,
            Strategy = strategy,

            // Survival
            AvgQuartersSurvived = results.Average(r => r.QuartersSurvived),
            MedianQuartersSurvived = quarters.Count > 0 ? quarters[quarters.Count / 2] : 0,
            MaxQuartersSurvived = quarters.Count > 0 ? quarters.Max() : 0,
            MinQuartersSurvived = quarters.Count > 0 ? quarters.Min() : 0,
            SurvivalRate5Q = (double)results.Count(r => r.QuartersSurvived >= 5) / gameCount * 100,
            SurvivalRate10Q = (double)results.Count(r => r.QuartersSurvived >= 10) / gameCount * 100,
            SurvivalRate20Q = (double)results.Count(r => r.QuartersSurvived >= 20) / gameCount * 100,

            // Performance
            AvgTotalProfit = results.Average(r => r.TotalProfit),
            AvgFinalFavorability = results.Average(r => r.FinalFavorability),
            AvgEvilScore = results.Average(r => r.FinalEvilScore),
            AvgFinalPC = results.Average(r => r.FinalPC),
            AvgProjectsPerQuarter = results.Where(r => r.QuartersSurvived > 0)
                .Average(r => (double)r.ProjectsSelected / r.QuartersSurvived),

            // Situations
            AvgSituationsPerGame = results.Average(r => r.Situations.SituationsTriggered),
            AvgCrisisDrawsPerGame = results.Average(r => r.Situations.CrisisDraws),
            AvgDeferralsPerGame = results.Average(r => r.Situations.SituationsDeferred),
            AvgResurfacesPerGame = results.Average(r => r.Situations.SituationsResurfaced),
            AvgFadesPerGame = results.Average(r => r.Situations.SituationsFaded),
            AvgPCSpentOnSituations = results.Average(r => r.Situations.PCSpentOnSituations),
            AvgEvilFromSituations = results.Average(r => r.Situations.EvilGainedFromSituations),
            AvgResponseTypeUsage = avgResponseTypeUsage,
            AvgSituationsBySeverity = avgSituationsBySeverity,

            // Death
            DeathReasons = deathReasons,
            QuarterDistribution = distribution
        };
    }

    /// <summary>
    /// Formats simulation stats as a readable report.
    /// </summary>
    public static string FormatReport(SimulationStats stats)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"╔══════════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║     QuarterLoop Simulation Report: {stats.Strategy,-20}         ║");
        sb.AppendLine($"╚══════════════════════════════════════════════════════════════════╝");
        sb.AppendLine($"Games Played: {stats.GamesPlayed}");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────┐");
        sb.AppendLine("│            SURVIVAL METRICS             │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        sb.AppendLine($"│ Average Quarters:  {stats.AvgQuartersSurvived,6:F1} ({stats.AvgQuartersSurvived / 4:F1} years)   │");
        sb.AppendLine($"│ Median Quarters:   {stats.MedianQuartersSurvived,6}                │");
        sb.AppendLine($"│ Range:             {stats.MinQuartersSurvived,3} - {stats.MaxQuartersSurvived,-3}               │");
        sb.AppendLine($"│ Survive 5+ Q:      {stats.SurvivalRate5Q,5:F1}%               │");
        sb.AppendLine($"│ Survive 10+ Q:     {stats.SurvivalRate10Q,5:F1}%               │");
        sb.AppendLine($"│ Survive 20+ Q:     {stats.SurvivalRate20Q,5:F1}%               │");
        sb.AppendLine("└─────────────────────────────────────────┘");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────┐");
        sb.AppendLine("│           PERFORMANCE METRICS           │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        sb.AppendLine($"│ Avg Total Profit:      ${stats.AvgTotalProfit,7:F0}M        │");
        sb.AppendLine($"│ Avg Final Favorability: {stats.AvgFinalFavorability,5:F1}%          │");
        sb.AppendLine($"│ Avg Evil Score:         {stats.AvgEvilScore,5:F1}           │");
        sb.AppendLine($"│ Avg Final PC:           {stats.AvgFinalPC,5:F1}           │");
        sb.AppendLine($"│ Projects/Quarter:       {stats.AvgProjectsPerQuarter,5:F2}           │");
        sb.AppendLine("└─────────────────────────────────────────┘");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────┐");
        sb.AppendLine("│           SITUATION METRICS             │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        sb.AppendLine($"│ Situations/Game:       {stats.AvgSituationsPerGame,6:F1}           │");
        sb.AppendLine($"│ Crisis Draws/Game:     {stats.AvgCrisisDrawsPerGame,6:F1}           │");
        sb.AppendLine($"│ Deferrals/Game:        {stats.AvgDeferralsPerGame,6:F1}           │");
        sb.AppendLine($"│ Resurfaces/Game:       {stats.AvgResurfacesPerGame,6:F1}           │");
        sb.AppendLine($"│ Fades/Game:            {stats.AvgFadesPerGame,6:F1}           │");
        sb.AppendLine($"│ PC Spent on Situations:{stats.AvgPCSpentOnSituations,6:F1}           │");
        sb.AppendLine($"│ Evil from Situations:  {stats.AvgEvilFromSituations,6:F1}           │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        sb.AppendLine("│ Response Types:                         │");
        sb.AppendLine($"│   PC:    {stats.AvgResponseTypeUsage.GetValueOrDefault(ResponseType.PC, 0),5:F1}   Risk:  {stats.AvgResponseTypeUsage.GetValueOrDefault(ResponseType.Risk, 0),5:F1}        │");
        sb.AppendLine($"│   Evil:  {stats.AvgResponseTypeUsage.GetValueOrDefault(ResponseType.Evil, 0),5:F1}   Defer: {stats.AvgResponseTypeUsage.GetValueOrDefault(ResponseType.Defer, 0),5:F1}        │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        sb.AppendLine("│ Situations by Severity:                 │");
        sb.AppendLine($"│   Minor:   {stats.AvgSituationsBySeverity.GetValueOrDefault(SituationSeverity.Minor, 0),5:F1}  Moderate: {stats.AvgSituationsBySeverity.GetValueOrDefault(SituationSeverity.Moderate, 0),5:F1}    │");
        sb.AppendLine($"│   Major:   {stats.AvgSituationsBySeverity.GetValueOrDefault(SituationSeverity.Major, 0),5:F1}  Critical: {stats.AvgSituationsBySeverity.GetValueOrDefault(SituationSeverity.Critical, 0),5:F1}    │");
        sb.AppendLine("└─────────────────────────────────────────┘");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────┐");
        sb.AppendLine("│      QUARTER DISTRIBUTION (by year)     │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        for (int i = 0; i < Math.Min(stats.QuarterDistribution.Length, 10); i++)
        {
            var count = stats.QuarterDistribution[i];
            var pct = (double)count / stats.GamesPlayed * 100;
            var barLength = (int)(pct / 3);
            var bar = new string('█', Math.Min(barLength, 20));
            sb.AppendLine($"│ Year {i}: {count,4} ({pct,5:F1}%) {bar,-20}│");
        }
        sb.AppendLine("└─────────────────────────────────────────┘");
        sb.AppendLine();

        sb.AppendLine("┌─────────────────────────────────────────┐");
        sb.AppendLine("│              DEATH REASONS              │");
        sb.AppendLine("├─────────────────────────────────────────┤");
        foreach (var (reason, count) in stats.DeathReasons.OrderByDescending(x => x.Value).Take(10))
        {
            var pct = (double)count / stats.GamesPlayed * 100;
            var displayReason = reason.Length > 28 ? reason[..25] + "..." : reason;
            sb.AppendLine($"│ {displayReason,-28} {count,3} ({pct,4:F0}%)│");
        }
        sb.AppendLine("└─────────────────────────────────────────┘");

        return sb.ToString();
    }

    /// <summary>
    /// Runs simulations for all strategies and returns a comparison report.
    /// </summary>
    public static string RunAllStrategiesComparison(int gamesPerStrategy = 100, int baseSeed = 42)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    STRATEGY COMPARISON REPORT                                    ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════════╣");
        sb.AppendLine("║ Strategy      │ Avg Q │ Med Q │ 10Q% │ Profit │ Fav  │ Evil │ Sit/G │ Crisis/G  ║");
        sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════════╣");

        foreach (var strategy in Enum.GetValues<Strategy>())
        {
            var stats = RunSimulation(gamesPerStrategy, strategy, baseSeed);
            sb.AppendLine($"║ {strategy,-13} │ {stats.AvgQuartersSurvived,5:F1} │ {stats.MedianQuartersSurvived,5} │ {stats.SurvivalRate10Q,4:F0}% │ ${stats.AvgTotalProfit,5:F0}M │ {stats.AvgFinalFavorability,4:F0}% │ {stats.AvgEvilScore,4:F1} │ {stats.AvgSituationsPerGame,5:F1} │ {stats.AvgCrisisDrawsPerGame,5:F1}     ║");
        }

        sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════════╝");
        return sb.ToString();
    }
}
