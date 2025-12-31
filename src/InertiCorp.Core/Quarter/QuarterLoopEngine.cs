using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Email;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Quarter;

/// <summary>
/// Result of advancing through a quarter phase.
/// </summary>
public sealed record QuarterLoopResult(
    QuarterLoopState NewLoopState,
    OrgState NewOrg,
    CEOState NewCEO,
    ResourceState NewResources,
    CrisisState NewCrises,
    CardHand NewHand,
    CardDeck NewCardDeck,
    Inbox NewInbox,
    QuarterLog Log,
    bool GameOver,
    IReadOnlyList<PendingSituation> PendingSituations,
    IReadOnlyList<PendingSituation> DeferredSituations)
{
    /// <summary>
    /// Creates a result with empty situation queues (for backwards compatibility).
    /// </summary>
    public static QuarterLoopResult Create(
        QuarterLoopState newLoopState,
        OrgState newOrg,
        CEOState newCEO,
        ResourceState newResources,
        CrisisState newCrises,
        CardHand newHand,
        CardDeck newCardDeck,
        Inbox newInbox,
        QuarterLog log,
        bool gameOver,
        IReadOnlyList<PendingSituation>? pendingSituations = null,
        IReadOnlyList<PendingSituation>? deferredSituations = null) =>
        new(newLoopState, newOrg, newCEO, newResources, newCrises, newHand, newCardDeck, newInbox, log, gameOver,
            pendingSituations ?? Array.Empty<PendingSituation>(),
            deferredSituations ?? Array.Empty<PendingSituation>());
}

/// <summary>
/// Engine for the new quarterly loop system.
/// Handles Projects → Crisis → BoardMeeting phase progression.
/// </summary>
public static class QuarterLoopEngine
{
    /// <summary>
    /// Processes the Projects phase with player's selection.
    /// </summary>
    public static QuarterLoopResult ProcessProjectsPhase(
        QuarterLoopState loopState,
        OrgState org,
        CEOState ceo,
        ResourceState resources,
        CrisisState crises,
        CardHand hand,
        CardDeck deck,
        Inbox inbox,
        ProjectsPhaseChoice choice,
        IRng rng,
        int seed,
        IReadOnlyList<PendingSituation>? pendingSituations = null,
        IReadOnlyList<PendingSituation>? deferredSituations = null)
    {
        var log = QuarterLog.Create(loopState.QuarterNumber, GamePhase.PlayCards);
        var newHand = hand;
        var newDeck = deck;
        var newOrg = org;
        var newCEO = ceo;
        var newResources = resources;
        var newInbox = inbox;
        var newLoopState = loopState;

        IReadOnlyList<string> selectedIds;

        switch (choice)
        {
            case ProjectsPhaseChoice.SelectProjects select:
                if (!select.IsValid)
                    throw new ArgumentException("Must select exactly 3 distinct projects");

                // Validate all cards are in hand
                foreach (var cardId in select.CardIds)
                {
                    if (!hand.Contains(cardId))
                        throw new ArgumentException($"Card {cardId} not in hand");
                }

                selectedIds = select.CardIds;
                log = log.WithEntry(LogEntry.Info($"Selected {selectedIds.Count} projects"));
                break;

            case ProjectsPhaseChoice.ReorgAndSelectOne reorg:
                // Reorg: discard hand, draw new hand
                log = log.WithEntry(LogEntry.Info("Reorganization: discarding hand and redrawing"));

                foreach (var card in hand.Cards)
                {
                    newDeck = newDeck.Discard(card);
                }
                newHand = CardHand.Empty;

                // Draw new hand
                var (deckAfterDraw, drawnCards) = newDeck.DrawMultiple(CardHand.MaxHandSize, rng);
                newHand = newHand.WithCardsAdded(drawnCards);
                newDeck = deckAfterDraw;

                if (!newHand.Contains(reorg.CardId))
                    throw new ArgumentException($"Card {reorg.CardId} not in new hand");

                selectedIds = new[] { reorg.CardId };
                newLoopState = newLoopState.WithReorgUsed();
                log = log.WithEntry(LogEntry.Info($"Selected 1 project after reorg"));
                break;

            default:
                throw new ArgumentException("Invalid projects phase choice");
        }

        // Resolve each selected project
        int totalProfit = 0;
        int totalFines = 0;
        var emailGen = new EmailGenerator(seed);
        var newPendingSituations = new List<PendingSituation>(pendingSituations ?? Array.Empty<PendingSituation>());
        var newDeferredSituations = new List<PendingSituation>(deferredSituations ?? Array.Empty<PendingSituation>());

        foreach (var cardId in selectedIds)
        {
            var card = newHand.Cards.First(c => c.CardId == cardId);
            log = log.WithEntry(LogEntry.Info($"Executing project: {card.Title}"));

            // Roll for outcome
            var outcomeTier = OutcomeRoller.Roll(
                card.Outcomes,
                newOrg.Alignment,
                ceo.BoardPressureLevel,
                rng,
                ceo.EvilScore);

            var effects = card.Outcomes.GetEffectsForTier(outcomeTier);
            log = log.WithEntry(LogEntry.Info($"Outcome: {outcomeTier}"));

            // Track meter changes for email
            var meterDeltas = new List<(Meter, int)>();
            int projectFines = 0;

            // Apply effects
            foreach (var effect in effects)
            {
                if (effect is MeterEffect meterEffect)
                {
                    newOrg = newOrg.WithMeterChange(meterEffect.Meter, meterEffect.Delta);
                    meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
                    log = log.WithEntry(LogEntry.Info($"  {meterEffect.Meter}: {meterEffect.Delta:+#;-#;0}"));
                }
                else if (effect is FineEffect fineEffect)
                {
                    projectFines += fineEffect.Amount;
                    log = log.WithEntry(LogEntry.Info($"  Fine: ${fineEffect.Amount}M ({fineEffect.Reason})"));
                }
            }

            // Calculate profit contribution from this project
            int projectProfit = CalculateProjectProfit(outcomeTier, meterDeltas);
            totalProfit += projectProfit;
            totalFines += projectFines;
            log = log.WithEntry(LogEntry.Info($"  Profit contribution: ${projectProfit}M"));

            // Handle corporate cards
            if (card.IsCorporate)
            {
                newCEO = newCEO.WithEvilScoreChange(card.CorporateIntensity);
                var favBump = 1 + (card.CorporateIntensity - 1);
                newCEO = newCEO.WithFavorabilityChange(favBump);
                log = log.WithEntry(LogEntry.Info($"  Corporate initiative: EvilScore +{card.CorporateIntensity}"));
            }

            // Generate email thread for this project FIRST (so we can link situations to it)
            var thread = emailGen.CreateCardThread(
                card, outcomeTier, meterDeltas,
                loopState.QuarterNumber, newOrg.Alignment);
            newInbox = newInbox.WithThreadAdded(thread);

            // Check for situation trigger from this card
            var seededRng = new SeededRng(HashCode.Combine(seed, loopState.QuarterNumber, card.CardId));
            PendingSituation? triggeredSituation = null;

            // First check card-specific situation mappings
            var cardSituations = CardSituationMappings.GetMappings(card.CardId);
            if (cardSituations is not null)
            {
                triggeredSituation = SituationResolver.CheckForTrigger(
                    cardSituations, outcomeTier, loopState.QuarterNumber, seededRng,
                    originatingThreadId: thread.ThreadId);
            }

            // If no specific trigger, check generic situation pool
            // Chance increases with quarters played (5% base + 2% per quarter, max 25%)
            if (triggeredSituation is null)
            {
                triggeredSituation = SituationResolver.CheckGenericTrigger(
                    card.CardId, outcomeTier, loopState.QuarterNumber, seededRng,
                    originatingThreadId: thread.ThreadId);
            }

            if (triggeredSituation is not null)
            {
                newPendingSituations.Add(triggeredSituation);
                var situationDef = SituationContent.Get(triggeredSituation.SituationId);
                if (situationDef is not null)
                {
                    var delay = triggeredSituation.ScheduledQuarter - loopState.QuarterNumber;
                    if (delay == 0)
                    {
                        log = log.WithEntry(LogEntry.Info($"  ⚠ Situation triggered: {situationDef.Title} (immediate)"));
                    }
                    else
                    {
                        log = log.WithEntry(LogEntry.Info($"  ⚠ Situation brewing: {situationDef.Title} (Q+{delay})"));
                    }
                }
            }

            // Remove card from hand and discard
            newHand = newHand.WithCardRemoved(cardId);
            newDeck = newDeck.Discard(card);
        }

        // Update loop state
        newLoopState = newLoopState
            .WithProjectsSelected(selectedIds)
            .WithProfitAdded(totalProfit)
            .WithFineAdded(totalFines)
            .NextPhase(); // Move to Situation phase

        return QuarterLoopResult.Create(
            newLoopState: newLoopState,
            newOrg: newOrg,
            newCEO: newCEO,
            newResources: newResources,
            newCrises: crises,
            newHand: newHand,
            newCardDeck: newDeck,
            newInbox: newInbox,
            log: log,
            gameOver: false,
            pendingSituations: newPendingSituations,
            deferredSituations: newDeferredSituations);
    }

    /// <summary>
    /// Processes the Situation phase.
    /// Handles: random crisis draws (33% chance), card-triggered situations, and deferred situations.
    /// </summary>
    public static QuarterLoopResult ProcessSituationPhase(
        QuarterLoopState loopState,
        OrgState org,
        CEOState ceo,
        ResourceState resources,
        CrisisState crises,
        CardHand hand,
        CardDeck deck,
        Inbox inbox,
        SituationPhaseChoice choice,
        IRng rng,
        int seed,
        IReadOnlyList<PendingSituation> pendingSituations,
        IReadOnlyList<PendingSituation> deferredSituations)
    {
        var log = QuarterLog.Create(loopState.QuarterNumber, GamePhase.Crisis); // Reuse Crisis phase for log
        var newOrg = org;
        var newCEO = ceo;
        var newResources = resources;
        var newInbox = inbox;
        var newLoopState = loopState;
        var newPendingSituations = new List<PendingSituation>(pendingSituations);
        var newDeferredSituations = new List<PendingSituation>(deferredSituations);

        // Roll for random crisis (33% chance per quarter)
        var crisisRng = new SeededRng(HashCode.Combine(seed, loopState.QuarterNumber, "crisis_roll"));
        var crisisRoll = crisisRng.NextInt(1, 101);
        if (crisisRoll <= 33)
        {
            // Draw a random crisis situation
            var crisisSituation = SelectRandomCrisisSituation(loopState.QuarterNumber, seed);
            if (crisisSituation is not null)
            {
                var pending = PendingSituation.Create(
                    crisisSituation.SituationId,
                    originCardId: "RANDOM_CRISIS",
                    currentQuarter: loopState.QuarterNumber,
                    delayQuarters: 0); // Immediate
                newPendingSituations.Insert(0, pending); // Crisis gets priority
                log = log.WithEntry(LogEntry.Info($"⚠ Crisis emerged: {crisisSituation.Title}"));
            }
        }

        // Check for resurfacing deferred situations
        var resurfacedSituation = CheckForResurfacingSituation(
            newDeferredSituations, loopState.QuarterNumber, seed, rng);

        if (resurfacedSituation is not null)
        {
            // Move from deferred to pending with escalated severity
            newDeferredSituations = newDeferredSituations
                .Where(s => s.SituationId != resurfacedSituation.SituationId)
                .ToList();
            newPendingSituations.Add(resurfacedSituation with { ScheduledQuarter = loopState.QuarterNumber });
            log = log.WithEntry(LogEntry.Info($"⚠ Deferred situation resurfaced"));
        }

        // Find the first situation that is due this quarter
        var activeSituation = loopState.ActiveSituation
            ?? newPendingSituations.FirstOrDefault(s => s.IsDueAt(loopState.QuarterNumber));

        // If no situation, skip to next phase
        if (activeSituation is null || choice is SituationPhaseChoice.Skip)
        {
            newLoopState = newLoopState.NextPhase();
            return QuarterLoopResult.Create(
                newLoopState: newLoopState,
                newOrg: newOrg,
                newCEO: newCEO,
                newResources: newResources,
                newCrises: crises,
                newHand: hand,
                newCardDeck: deck,
                newInbox: newInbox,
                log: log.WithEntry(LogEntry.Info("No situations to handle this quarter")),
                gameOver: false,
                pendingSituations: newPendingSituations,
                deferredSituations: newDeferredSituations);
        }

        // Apply decay roll for delayed situations
        var seededRng = new SeededRng(HashCode.Combine(seed, loopState.QuarterNumber, activeSituation.SituationId, "decay"));
        if (!SituationResolver.CheckDecay(activeSituation, loopState.QuarterNumber, seededRng))
        {
            // Situation decayed away
            newPendingSituations = newPendingSituations
                .Where(s => s.SituationId != activeSituation.SituationId)
                .ToList();
            log = log.WithEntry(LogEntry.Info($"Situation faded: {activeSituation.SituationId}"));

            // Check for more situations
            var nextSituation = newPendingSituations.FirstOrDefault(s => s.IsDueAt(loopState.QuarterNumber));
            if (nextSituation is null)
            {
                newLoopState = newLoopState.NextPhase();
            }
            else
            {
                newLoopState = newLoopState.WithActiveSituation(nextSituation);
            }

            return QuarterLoopResult.Create(
                newLoopState: newLoopState,
                newOrg: newOrg,
                newCEO: newCEO,
                newResources: newResources,
                newCrises: crises,
                newHand: hand,
                newCardDeck: deck,
                newInbox: newInbox,
                log: log,
                gameOver: false,
                pendingSituations: newPendingSituations,
                deferredSituations: newDeferredSituations);
        }

        // Get situation definition
        var situationDef = SituationContent.Get(activeSituation.SituationId);
        if (situationDef is null)
        {
            throw new InvalidOperationException($"Unknown situation: {activeSituation.SituationId}");
        }

        // Apply escalation for deferred situations
        if (activeSituation.DeferCount > 0)
        {
            for (int i = 0; i < activeSituation.DeferCount; i++)
            {
                situationDef = situationDef.WithEscalatedSeverity();
            }
            log = log.WithEntry(LogEntry.Info($"Situation escalated to {situationDef.Severity} after {activeSituation.DeferCount} deferrals"));
        }

        log = log.WithEntry(LogEntry.Info($"Situation: {situationDef.Title} ({situationDef.Severity})"));

        // Generate situation email
        var emailGen = new EmailGenerator(seed);
        var situationTone = situationDef.Severity >= SituationSeverity.Major ? EmailTone.Panicked : EmailTone.Professional;

        // Check if this situation has an originating card thread to append to
        if (activeSituation.OriginatingThreadId is not null &&
            newInbox.GetThread(activeSituation.OriginatingThreadId) is not null)
        {
            // Append situation as follow-up to the original card's email thread
            var followUp = emailGen.CreateFollowUpReply(
                activeSituation.OriginatingThreadId,
                situationDef.EmailSubject,
                situationDef.EmailBody,
                Array.Empty<(Meter, int)>(),  // Effects applied separately
                loopState.QuarterNumber,
                org.Alignment,
                SenderArchetype.HR);
            newInbox = newInbox.WithFollowUpAdded(activeSituation.OriginatingThreadId, followUp);
        }
        else
        {
            // Random crisis or orphaned situation - create standalone thread
            var situationThread = emailGen.CreateNotificationThread(
                activeSituation.SituationId,
                situationDef.EmailSubject,
                situationDef.EmailBody,
                SenderArchetype.HR,
                loopState.QuarterNumber,
                situationTone);
            newInbox = newInbox.WithThreadAdded(situationThread);
        }

        // Determine response type from choice
        var responseType = choice switch
        {
            SituationPhaseChoice.SpendPC => ResponseType.PC,
            SituationPhaseChoice.RollDice => ResponseType.Risk,
            SituationPhaseChoice.EvilOption => ResponseType.Evil,
            SituationPhaseChoice.Defer => ResponseType.Defer,
            _ => ResponseType.Risk // Default fallback
        };

        // Handle defer
        if (responseType == ResponseType.Defer)
        {
            if (!situationDef.CanDefer)
            {
                throw new InvalidOperationException("Cannot defer Critical situations");
            }

            // Move to deferred queue
            newPendingSituations = newPendingSituations
                .Where(s => s.SituationId != activeSituation.SituationId)
                .ToList();
            newDeferredSituations.Add(activeSituation.WithDeferred(loopState.QuarterNumber));

            log = log.WithEntry(LogEntry.Info($"Deferred situation: {situationDef.Title}"));

            // Check for more situations
            var nextSituation = newPendingSituations.FirstOrDefault(s => s.IsDueAt(loopState.QuarterNumber));
            if (nextSituation is null)
            {
                newLoopState = newLoopState.NextPhase();
            }
            else
            {
                newLoopState = newLoopState.WithActiveSituation(nextSituation);
            }

            return QuarterLoopResult.Create(
                newLoopState: newLoopState,
                newOrg: newOrg,
                newCEO: newCEO,
                newResources: newResources,
                newCrises: crises,
                newHand: hand,
                newCardDeck: deck,
                newInbox: newInbox,
                log: log,
                gameOver: false,
                pendingSituations: newPendingSituations,
                deferredSituations: newDeferredSituations);
        }

        // Resolve the response
        var resolveRng = new SeededRng(HashCode.Combine(seed, loopState.QuarterNumber, activeSituation.SituationId, "resolve"));
        var result = SituationResolver.ResolveResponse(situationDef, responseType, resolveRng);

        log = log.WithEntry(LogEntry.Info($"Response: {responseType} → {result.Outcome}"));

        // Check PC cost for PC response
        if (responseType == ResponseType.PC)
        {
            var pcResponse = situationDef.GetResponse(ResponseType.PC);
            if (pcResponse?.PCCost is not null && resources.PoliticalCapital < pcResponse.PCCost)
            {
                throw new InvalidOperationException($"Not enough PC: need {pcResponse.PCCost}, have {resources.PoliticalCapital}");
            }
            if (pcResponse?.PCCost is not null)
            {
                newResources = newResources.WithSpend(pcResponse.PCCost.Value);
                log = log.WithEntry(LogEntry.Info($"  Spent {pcResponse.PCCost} PC"));
            }
        }

        // Apply evil delta
        if (result.EvilDelta != 0)
        {
            newCEO = newCEO.WithEvilScoreChange(result.EvilDelta);
            log = log.WithEntry(LogEntry.Info($"  Evil score: {result.EvilDelta:+#;-#;0}"));
        }

        // Apply effects
        foreach (var effect in result.Effects)
        {
            if (effect is MeterEffect meterEffect)
            {
                newOrg = newOrg.WithMeterChange(meterEffect.Meter, meterEffect.Delta);
                log = log.WithEntry(LogEntry.Info($"  {meterEffect.Meter}: {meterEffect.Delta:+#;-#;0}"));
            }
            else if (effect is FineEffect fineEffect)
            {
                newLoopState = newLoopState.WithFineAdded(fineEffect.Amount);
                log = log.WithEntry(LogEntry.Info($"  Fine: ${fineEffect.Amount}M ({fineEffect.Reason})"));
            }
        }

        // Remove from pending
        newPendingSituations = newPendingSituations
            .Where(s => s.SituationId != activeSituation.SituationId)
            .ToList();

        // Check for more situations this quarter
        var remainingSituation = newPendingSituations.FirstOrDefault(s => s.IsDueAt(loopState.QuarterNumber));
        if (remainingSituation is null)
        {
            newLoopState = newLoopState.NextPhase();
        }
        else
        {
            newLoopState = newLoopState.WithActiveSituation(remainingSituation);
        }

        return QuarterLoopResult.Create(
            newLoopState: newLoopState,
            newOrg: newOrg,
            newCEO: newCEO,
            newResources: newResources,
            newCrises: crises,
            newHand: hand,
            newCardDeck: deck,
            newInbox: newInbox,
            log: log,
            gameOver: false,
            pendingSituations: newPendingSituations,
            deferredSituations: newDeferredSituations);
    }

    /// <summary>
    /// Checks for deferred situations that resurface this quarter.
    /// </summary>
    private static PendingSituation? CheckForResurfacingSituation(
        List<PendingSituation> deferredSituations,
        int currentQuarter,
        int seed,
        IRng rng)
    {
        // Check each deferred situation for fade or resurface
        var toRemove = new List<PendingSituation>();

        foreach (var situation in deferredSituations.ToList())
        {
            // Check if should fade (4+ quarters in deferred queue)
            if (SituationResolver.ShouldFade(situation, currentQuarter))
            {
                toRemove.Add(situation);
                continue;
            }

            // Check for resurface (30% chance per quarter)
            var seededRng = new SeededRng(HashCode.Combine(seed, currentQuarter, situation.SituationId, "resurface"));
            if (SituationResolver.CheckResurface(situation, seededRng))
            {
                return situation;
            }
        }

        // Remove faded situations
        foreach (var situation in toRemove)
        {
            deferredSituations.Remove(situation);
        }

        return null;
    }

    /// <summary>
    /// Selects a random crisis-level situation for the quarter.
    /// Returns Major or Critical severity situations.
    /// </summary>
    private static SituationDefinition? SelectRandomCrisisSituation(int quarterNumber, int seed)
    {
        // Get all Major and Critical situations
        var crisisSituations = SituationContent.All.Values
            .Where(s => s.Severity >= SituationSeverity.Major)
            .ToList();

        if (crisisSituations.Count == 0) return null;

        // Deterministic selection based on seed and quarter
        var index = Math.Abs(HashCode.Combine(seed, quarterNumber, "crisis_select")) % crisisSituations.Count;
        return crisisSituations[index];
    }

    /// <summary>
    /// Processes the Board Meeting phase.
    /// </summary>
    public static QuarterLoopResult ProcessBoardMeetingPhase(
        QuarterLoopState loopState,
        OrgState org,
        CEOState ceo,
        ResourceState resources,
        CrisisState crises,
        CardHand hand,
        CardDeck deck,
        Inbox inbox,
        BoardMeetingChoice choice,
        IRng rng,
        int seed,
        IReadOnlyList<PendingSituation>? pendingSituations = null,
        IReadOnlyList<PendingSituation>? deferredSituations = null)
    {
        var log = QuarterLog.Create(loopState.QuarterNumber, GamePhase.Resolution);
        var newOrg = org;
        var newCEO = ceo;
        var newResources = resources;
        var newInbox = inbox;
        var newHand = hand;
        var newDeck = deck;

        log = log.WithEntry(LogEntry.Info($"Q{loopState.QuarterNumber} Board Meeting"));
        log = log.WithEntry(LogEntry.Info($"Gross Profit: ${loopState.QuarterProfit}M"));
        if (loopState.QuarterFines > 0)
        {
            log = log.WithEntry(LogEntry.Info($"Fines/Settlements: -${loopState.QuarterFines}M"));
            log = log.WithEntry(LogEntry.Info($"Net Profit: ${loopState.NetProfit}M"));
        }

        // Apply board influence if chosen
        BoardInfluenceResult? influence = null;
        if (choice is BoardMeetingChoice.Influence inf)
        {
            var package = BoardInfluenceContent.GetPackage(inf.InfluencePackageId);
            if (package is null)
                throw new ArgumentException($"Unknown influence package: {inf.InfluencePackageId}");

            if (!resources.CanAfford(package.CostPC))
                throw new InvalidOperationException($"Cannot afford influence: need {package.CostPC} PC");

            newResources = newResources.WithSpend(package.CostPC);
            influence = BoardInfluenceContent.ApplyInfluence(package, seed, loopState.QuarterNumber);
            log = log.WithEntry(LogEntry.Info($"Board influence: {package.Title} (cost {package.CostPC} PC)"));
        }

        // Calculate board review (uses net profit after fines)
        var review = BoardReviewCalculator.Calculate(
            loopState.QuarterNumber,
            loopState.NetProfit,
            newOrg,
            newCEO,
            newResources,
            influence,
            loopState.ConsecutivePoorQuarters,
            rng);

        log = log.WithEntry(LogEntry.Info($"Board Rating: {review.RatingLabel} ({review.ModifiedScore})"));
        log = log.WithEntry(LogEntry.Info($"Decision: {review.Decision}"));

        // Generate board meeting email thread
        var emailGen = new EmailGenerator(seed);
        var boardThread = GenerateBoardMeetingEmail(
            emailGen, review, loopState.QuarterNumber, newOrg.Alignment,
            loopState.QuarterProfit, loopState.QuarterFines);
        newInbox = newInbox.WithThreadAdded(boardThread);

        // Update CEO state (net profit after fines)
        newCEO = newCEO.WithProfitAdded(loopState.NetProfit);

        // Apply PC earning based on org metrics
        newResources = newResources.WithTurnEndAdjustments(newOrg);

        bool gameOver = review.Decision == EmploymentDecision.Terminate;

        if (gameOver)
        {
            newCEO = newCEO.WithOusted();
            log = log.WithEntry(LogEntry.Info($"TERMINATED. Golden Parachute: ${review.Parachute?.TotalPayout ?? 0}M"));
        }
        else
        {
            newCEO = newCEO.WithQuarterComplete();
            log = log.WithEntry(LogEntry.Info($"Survived Q{loopState.QuarterNumber}. Proceeding to Q{loopState.QuarterNumber + 1}"));

            // Refill hand for next quarter
            var cardsToDraw = CardHand.MaxHandSize - newHand.Count;
            if (cardsToDraw > 0)
            {
                var (deckAfterDraw, drawnCards) = newDeck.DrawMultiple(cardsToDraw, rng);
                newHand = newHand.WithCardsAdded(drawnCards);
                newDeck = deckAfterDraw;
            }
        }

        var newLoopState = loopState.WithBoardReview(review);
        if (!gameOver)
        {
            newLoopState = newLoopState.NextPhase(); // Start next quarter
        }

        return QuarterLoopResult.Create(
            newLoopState: newLoopState,
            newOrg: newOrg,
            newCEO: newCEO,
            newResources: newResources,
            newCrises: crises,
            newHand: newHand,
            newCardDeck: newDeck,
            newInbox: newInbox,
            log: log,
            gameOver: gameOver,
            pendingSituations: pendingSituations,
            deferredSituations: deferredSituations);
    }

    /// <summary>
    /// Calculates profit contribution from a project outcome.
    /// </summary>
    private static int CalculateProjectProfit(OutcomeTier outcome, IReadOnlyList<(Meter, int)> meterDeltas)
    {
        int baseProfit = outcome switch
        {
            OutcomeTier.Good => 5,
            OutcomeTier.Expected => 2,
            OutcomeTier.Bad => -2,
            _ => 0
        };

        // Bonus from delivery improvements
        int deliveryBonus = meterDeltas
            .Where(d => d.Item1 == Meter.Delivery && d.Item2 > 0)
            .Sum(d => d.Item2 / 5);

        // Penalty from runway hits
        int runwayPenalty = meterDeltas
            .Where(d => d.Item1 == Meter.Runway && d.Item2 < 0)
            .Sum(d => d.Item2 / 3);

        return baseProfit + deliveryBonus + runwayPenalty;
    }

    /// <summary>
    /// Generates the board meeting email thread.
    /// </summary>
    private static EmailThread GenerateBoardMeetingEmail(
        EmailGenerator emailGen,
        BoardReviewResult review,
        int quarterNumber,
        int alignment,
        int grossProfit = 0,
        int fines = 0)
    {
        var tone = review.Rating switch
        {
            BoardRating.A => EmailTone.Enthusiastic,
            BoardRating.B => EmailTone.Professional,
            BoardRating.C => EmailTone.Aloof,
            BoardRating.D => EmailTone.Blunt,
            BoardRating.F => EmailTone.Panicked,
            _ => EmailTone.Professional
        };

        var bodyLines = new List<string>
        {
            $"Quarterly Review: Q{quarterNumber}",
            "",
            "FINANCIALS:",
            $"  Gross Profit:    ${grossProfit,4}M"
        };

        if (fines > 0)
        {
            bodyLines.Add($"  Fines/Settlements: -${fines,4}M");
            bodyLines.Add($"  Net Profit:      ${grossProfit - fines,4}M");
        }

        bodyLines.Add("");
        bodyLines.Add($"Rating: {review.RatingLabel} ({review.Rating})");
        bodyLines.Add($"Score: {review.ModifiedScore}/100");
        bodyLines.Add("");
        bodyLines.Add("Key Factors:");

        foreach (var fact in review.JustificationFacts.Take(5))
        {
            bodyLines.Add($"  - {fact}");
        }

        bodyLines.Add("");

        if (review.Decision == EmploymentDecision.Terminate)
        {
            bodyLines.Add("DECISION: The board has voted to terminate your employment.");
            if (review.Parachute is not null)
            {
                bodyLines.Add($"Your golden parachute: ${review.Parachute.TotalPayout}M");
                bodyLines.Add($"  Base: ${review.Parachute.BasePayout}M");
                bodyLines.Add($"  Tenure: ${review.Parachute.TenureBonus}M");
                bodyLines.Add($"  PC Conversion: ${review.Parachute.PCConversion}M");
                if (review.Parachute.EthicsPenalty > 0)
                {
                    bodyLines.Add($"  Ethics Penalty: -${review.Parachute.EthicsPenalty}M");
                }
            }
        }
        else
        {
            bodyLines.Add("DECISION: The board has voted to retain you as CEO.");
            if (review.Rating == BoardRating.D)
            {
                bodyLines.Add("Note: You are on probation. Improvement is expected next quarter.");
            }
        }

        return emailGen.CreateNotificationThread(
            $"board_review_q{quarterNumber}",
            $"{QuarterState.FormatQuarter(quarterNumber)} Board Review: {review.RatingLabel}",
            string.Join("\n", bodyLines),
            SenderArchetype.BoardMember,
            quarterNumber,
            tone);
    }
}
