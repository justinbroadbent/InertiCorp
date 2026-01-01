using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Email;
using InertiCorp.Core.Llm;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core;

/// <summary>
/// Phase-based simulation engine for the CEO survival card game.
/// Flow: BoardDemand → PlayCards → Crisis → Resolution
/// Emails are generated immediately during PlayCards and Crisis phases.
/// </summary>
public static class QuarterEngine
{
    /// <summary>
    /// Advances the game state by one phase.
    /// </summary>
    public static (QuarterGameState NewState, QuarterLog Log) Advance(
        QuarterGameState state,
        QuarterInput input,
        IRng rng)
    {
        if (state.CEO.IsOusted)
        {
            throw new InvalidOperationException("Cannot advance: CEO has been ousted");
        }

        if (state.CEO.HasRetired)
        {
            throw new InvalidOperationException("Cannot advance: CEO has retired");
        }

        return state.Quarter.Phase switch
        {
            GamePhase.BoardDemand => AdvanceBoardDemand(state, rng),
            GamePhase.PlayCards => AdvancePlayCards(state, input, rng),
            GamePhase.Crisis => AdvanceCrisis(state, input, rng),
            GamePhase.Resolution => AdvanceResolution(state, input, rng),
            _ => throw new InvalidOperationException($"Unknown phase: {state.Quarter.Phase}")
        };
    }

    /// <summary>
    /// Board Demand phase - sets the directive for this quarter and sends directive email.
    /// </summary>
    private static (QuarterGameState NewState, QuarterLog Log) AdvanceBoardDemand(
        QuarterGameState state,
        IRng rng)
    {
        var log = QuarterLog.Create(state.Quarter.QuarterNumber, state.Quarter.Phase);
        var newState = state;

        // Directive was already set at game start or quarter transition
        if (state.CurrentDirective is not null)
        {
            log = log.WithEntry(LogEntry.Info(
                $"Board Directive: {state.CurrentDirective.GetDescription(state.CEO.BoardPressureLevel)}"));

            // Generate board directive email
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var directiveThread = emailGen.CreateBoardDirectiveThread(
                state.CurrentDirective,
                state.CEO.BoardPressureLevel,
                state.Quarter.QuarterNumber);
            var newInbox = newState.Inbox.WithThreadAdded(directiveThread);
            newState = newState.WithInbox(newInbox);
        }

        // 33% chance to draw a crisis card for the upcoming phase
        EventCard? crisisCard = null;
        var newEventDecks = state.EventDecks;
        var crisisRoll = rng.NextInt(1, 101);
        if (crisisRoll <= 33)
        {
            (newEventDecks, crisisCard) = state.EventDecks.DrawCrisis(rng);
            log = log.WithEntry(LogEntry.Info("A situation is brewing..."));

            // Pre-generate LLM content for the crisis (if LLM is ready)
            if (crisisCard != null)
            {
                LlmServiceManager.PreGenerateCrisis(crisisCard);
            }
        }

        newState = newState
            .WithEventDecks(newEventDecks)
            .WithCurrentCrisis(crisisCard)
            .WithQuarter(state.Quarter.NextPhase());

        return (newState, log);
    }

    /// <summary>
    /// Crisis phase - player responds to an event card via email reply.
    /// If no crisis was drawn, skip to Resolution phase.
    /// </summary>
    private static (QuarterGameState NewState, QuarterLog Log) AdvanceCrisis(
        QuarterGameState state,
        QuarterInput input,
        IRng rng)
    {
        var log = QuarterLog.Create(state.Quarter.QuarterNumber, state.Quarter.Phase);
        var newState = state;

        // Clean up expired follow-ups (projects older than 3 quarters)
        newState = newState.WithExpiredFollowUpsRemoved();

        // Check all pending follow-ups for triggered events
        var followUpRng = new SeededRng(HashCode.Combine(state.Seed, state.Quarter.QuarterNumber, "followups"));
        var followUpResults = FollowUpResolver.CheckAllFollowUps(
            newState.PendingFollowUps,
            state.Quarter.QuarterNumber,
            followUpRng);

        // Process triggered follow-ups
        var followUpEmailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
        foreach (var result in followUpResults)
        {
            var thread = newState.Inbox.GetThread(result.FollowUp.ThreadId);
            if (thread is null) continue;

            switch (result.Type)
            {
                case FollowUpType.Good:
                    // Good follow-up: send positive email and apply positive effects
                    var goodEffects = result.Effects?.Select(e => (e.Meter, e.Delta)).ToList()
                        ?? new List<(Meter, int)>();
                    var goodReply = followUpEmailGen.CreateGoodFollowUpReply(
                        thread.ThreadId,
                        thread.Subject,
                        result.FollowUp.CardTitle,
                        goodEffects,
                        state.Quarter.QuarterNumber);
                    newState = newState.WithInbox(newState.Inbox.WithFollowUpAdded(thread.ThreadId, goodReply));
                    newState = ApplyMeterEffects(newState, result.Effects);
                    log = log.WithEntry(LogEntry.Info($"📧 Good news on {result.FollowUp.CardTitle}"));
                    break;

                case FollowUpType.Meh:
                    // Meh follow-up: send neutral email and apply mild effects
                    var mehEffects = result.Effects?.Select(e => (e.Meter, e.Delta)).ToList()
                        ?? new List<(Meter, int)>();
                    var mehReply = followUpEmailGen.CreateMehFollowUpReply(
                        thread.ThreadId,
                        thread.Subject,
                        result.FollowUp.CardTitle,
                        mehEffects,
                        state.Quarter.QuarterNumber);
                    newState = newState.WithInbox(newState.Inbox.WithFollowUpAdded(thread.ThreadId, mehReply));
                    newState = ApplyMeterEffects(newState, result.Effects);
                    log = log.WithEntry(LogEntry.Info($"📧 Update on {result.FollowUp.CardTitle}"));
                    break;

                case FollowUpType.Crisis:
                    // Crisis follow-up: queue as pending situation
                    if (result.SituationId is not null)
                    {
                        var pendingSituation = PendingSituation.Create(
                            result.SituationId,
                            result.FollowUp.CardId,
                            state.Quarter.QuarterNumber,
                            0, // Immediate
                            result.FollowUp.ThreadId);
                        newState = newState.WithSituationQueued(pendingSituation);
                        log = log.WithEntry(LogEntry.Info($"⚠ Crisis brewing from {result.FollowUp.CardTitle}"));
                    }
                    break;
            }

            // Remove the processed follow-up from the queue
            newState = newState.WithFollowUpRemoved(result.FollowUp.CardId);
        }

        // Check for pending situations that are due this quarter
        string? situationOriginatingThreadId = null;
        var dueSituation = state.PendingSituations.FirstOrDefault(s => s.IsDueAt(state.Quarter.QuarterNumber));
        if (dueSituation is not null && state.CurrentCrisis is null)
        {
            var situationDef = SituationContent.Get(dueSituation.SituationId);
            if (situationDef is not null)
            {
                // Convert situation to crisis event card
                var crisisCard = situationDef.ToEventCard();
                situationOriginatingThreadId = dueSituation.OriginatingThreadId;
                newState = newState
                    .WithCurrentCrisis(crisisCard)
                    .WithSituationResolved(dueSituation.SituationId);

                // Pre-generate LLM content for the erupting situation
                LlmServiceManager.PreGenerateCrisis(crisisCard);

                log = log.WithEntry(LogEntry.Info($"⚠ Situation erupts: {situationDef.Title}"));
            }
            else
            {
                // Unknown situation, just remove it
                newState = newState.WithSituationResolved(dueSituation.SituationId);
            }
        }

        // No crisis this quarter - generate suck-up email and skip to Resolution
        if (newState.CurrentCrisis is null)
        {
            log = log.WithEntry(LogEntry.Info("No situations requiring attention this quarter."));

            // Generate suck-up email from management
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var randomKpiIndex = rng.NextInt(0, Content.SillyKPIs.KPIDefinitions.Length);
            var sillyKpiName = Content.SillyKPIs.KPIDefinitions[randomKpiIndex].Name;
            var suckUpThread = emailGen.CreateSuckUpThread(newState.Quarter.QuarterNumber, sillyKpiName, rng);
            var newInbox = newState.Inbox.WithThreadAdded(suckUpThread);

            var skipState = newState
                .WithInbox(newInbox)
                .WithQuarter(newState.Quarter.NextPhase());
            return (skipState, log);
        }

        var card = newState.CurrentCrisis;

        // If no choice provided, generate crisis email and wait for player response
        if (!input.HasChoice)
        {
            // Check if crisis email already exists in inbox
            var existingCrisisThread = newState.Inbox.Threads.FirstOrDefault(t => t.IsCrisis && t.OriginatingCardId == card!.EventId);

            if (existingCrisisThread is null)
            {
                var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
                var newInbox = newState.Inbox;

                // Check if this situation came from a project card - if so, add as follow-up
                if (situationOriginatingThreadId is not null &&
                    newInbox.GetThread(situationOriginatingThreadId) is not null)
                {
                    // Add situation as follow-up to the originating project email
                    var originThread = newInbox.GetThread(situationOriginatingThreadId)!;
                    var followUp = emailGen.CreateFollowUpReply(
                        situationOriginatingThreadId,
                        originThread.Subject,
                        $"⚠️ SITUATION: {card!.Title}\n\n{card!.Description}",
                        Array.Empty<(Meter, int)>(),
                        newState.Quarter.QuarterNumber,
                        newState.Org.Alignment,
                        SenderArchetype.HR);
                    newInbox = newInbox.WithFollowUpAdded(situationOriginatingThreadId, followUp);
                    // Upgrade the thread to Crisis type so it gets highlighted
                    newInbox = newInbox.WithThreadUpgradedToCrisis(situationOriginatingThreadId, card!.EventId);

                    log = log.WithEntry(LogEntry.Info($"Situation follow-up: {card.Title}"));
                }
                else
                {
                    // Standalone crisis - create new thread
                    var crisisThread = emailGen.CreateCrisisThread(
                        card!,
                        newState.Quarter.QuarterNumber,
                        newState.Org.Alignment,
                        newState.CEO.BoardPressureLevel,
                        newState.CEO.EvilScore);
                    newInbox = newInbox.WithThreadAdded(crisisThread);

                    log = log.WithEntry(LogEntry.Info($"Crisis: {card.Title}"));
                }

                newState = newState.WithInbox(newInbox);
                log = log.WithEntry(LogEntry.Info("Awaiting response via email..."));
            }

            return (newState, log);
        }

        var choice = card.GetChoice(input.ChoiceId);
        log = log.WithEntry(LogEntry.Info($"[{card.Title}] Response: {choice.Label}"));

        // Handle PC cost for choices that require political capital
        if (choice.HasPCCost)
        {
            if (!newState.Resources.CanAfford(choice.PCCost))
            {
                throw new InvalidOperationException(
                    $"Insufficient PC to select this choice (need {choice.PCCost} PC, have {newState.Resources.PoliticalCapital})");
            }

            newState = newState.WithResources(newState.Resources.WithSpend(choice.PCCost));
            log = log.WithEntry(LogEntry.Info($"Spent {choice.PCCost} PC to handle the situation"));
        }

        // Determine which effects to apply
        IReadOnlyList<IEffect> effectsToApply;
        OutcomeTier outcomeTier = OutcomeTier.Expected;

        if (choice.HasTieredOutcomes && choice.OutcomeProfile is not null)
        {
            // Use crisis-specific probability profiles:
            // - PC-cost choice: 66% good, 24% expected, 10% bad (reliable investment)
            // - Standard choice: 15% good, 75% expected, 10% bad (predictable)
            // - Corporate choice: 40% good, 10% expected, 50% bad (high risk/reward)
            outcomeTier = OutcomeRoller.RollCrisisChoice(
                choice.OutcomeProfile,
                choice,
                rng);

            effectsToApply = choice.OutcomeProfile.GetEffectsForTier(outcomeTier);
            log = log.WithEntry(LogEntry.Info($"Outcome: {outcomeTier}"));
        }
        else
        {
            effectsToApply = choice.Effects;
        }

        // Track meter deltas for email generation
        var meterDeltas = new List<(Meter Meter, int Delta)>();

        // Apply effects
        foreach (var effect in effectsToApply)
        {
            // Track meter effects for email
            if (effect is MeterEffect meterEffect)
            {
                meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
            }

            var (updatedState, entries) = effect.Apply(
                CreateAdaptedGameState(newState),
                rng);

            newState = ApplyAdaptedState(newState, updatedState);
            log = log.WithEntries(entries);
        }

        // Handle corporate choice
        if (choice.IsCorporateChoice)
        {
            var newCEO = newState.CEO.WithEvilScoreChange(choice.CorporateIntensityDelta);
            int favBump = 1 + (choice.CorporateIntensityDelta - 1);
            newCEO = newCEO.WithFavorabilityChange(favBump);

            newState = newState.WithCEO(newCEO);
            log = log.WithEntry(LogEntry.Info($"Corporate choice: EvilScore +{choice.CorporateIntensityDelta}, Favorability +{favBump}"));
        }

        // Generate crisis resolution emails as replies to existing thread
        var resolutionEmailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));

        // Find the existing crisis thread (either by OriginatingCardId or by IsCrisis flag)
        var targetThread = newState.Inbox.Threads.FirstOrDefault(t => t.OriginatingCardId == card.EventId) ??
                           newState.Inbox.Threads.FirstOrDefault(t => t.IsCrisis);

        if (targetThread is not null)
        {
            // First, add CEO's response/decision email
            var ceoResponse = resolutionEmailGen.CreateCEOResponseEmail(
                targetThread.ThreadId,
                targetThread.Subject,
                choice.Label,
                state.Quarter.QuarterNumber,
                choice.IsCorporateChoice);
            newState = newState.WithInbox(newState.Inbox.WithFollowUpAdded(targetThread.ThreadId, ceoResponse));

            // Then add the resolution/outcome email from the team
            var resolutionReply = resolutionEmailGen.CreateCrisisResolutionReply(
                targetThread.ThreadId,
                targetThread.Subject,
                card.Title,
                choice.Label,
                outcomeTier,
                meterDeltas,
                state.Quarter.QuarterNumber,
                choice.IsCorporateChoice);
            newState = newState.WithInbox(newState.Inbox.WithFollowUpAdded(targetThread.ThreadId, resolutionReply));
        }
        else
        {
            // Fallback: create standalone thread if crisis thread not found
            var resolutionThread = resolutionEmailGen.CreateCrisisResolutionThread(
                card.Title,
                choice.Label,
                outcomeTier,
                meterDeltas,
                state.Quarter.QuarterNumber,
                choice.IsCorporateChoice);
            newState = newState.WithInbox(newState.Inbox.WithThreadAdded(resolutionThread));
        }

        // Clear crisis and advance to Resolution
        newState = newState
            .WithCurrentCrisis(null)
            .WithQuarter(state.Quarter.NextPhase());

        return (newState, log);
    }

    /// <summary>
    /// Play Cards phase - player can play 0-3 cards from hand.
    /// 2nd and 3rd cards cost PC and have increased risk.
    /// </summary>
    private static (QuarterGameState NewState, QuarterLog Log) AdvancePlayCards(
        QuarterGameState state,
        QuarterInput input,
        IRng rng)
    {
        var log = QuarterLog.Create(state.Quarter.QuarterNumber, state.Quarter.Phase);
        var newState = state;

        // Handle meter exchange if requested
        if (input.HasMeterExchange && input.ExchangeMeter.HasValue)
        {
            var meter = input.ExchangeMeter.Value;
            var exchangeCount = input.ExchangeAmount;
            var (meterCost, pcGain) = ResourceState.GetMeterExchangeRate(meter);

            for (int i = 0; i < exchangeCount; i++)
            {
                if (!ResourceState.CanExchangeMeterForPC(newState.Org, meter))
                {
                    log = log.WithEntry(LogEntry.Info($"Insufficient {meter} for exchange"));
                    break;
                }

                newState = newState
                    .WithOrg(newState.Org.WithMeterChange(meter, -meterCost))
                    .WithResources(newState.Resources.WithPoliticalCapitalChange(pcGain));

                log = log.WithEntry(LogEntry.Info($"Exchanged {meterCost} {meter} for {pcGain} PC"));
            }

            return (newState, log);
        }

        // Handle spending PC to boost a meter (+5 for 1 PC)
        if (input.HasMeterBoost && input.BoostMeter.HasValue)
        {
            const int MeterBoostCost = 1;
            const int MeterBoostAmount = 5;

            var meter = input.BoostMeter.Value;

            if (!newState.Resources.CanAfford(MeterBoostCost))
            {
                log = log.WithEntry(LogEntry.Info($"Insufficient PC for meter boost (need {MeterBoostCost})"));
                return (newState, log);
            }

            var oldValue = newState.Org.GetMeter(meter);
            newState = newState
                .WithOrg(newState.Org.WithMeterChange(meter, MeterBoostAmount))
                .WithResources(newState.Resources.WithSpend(MeterBoostCost));
            var newValue = newState.Org.GetMeter(meter);

            // Generate email about the meter boost
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var boostThread = emailGen.CreateMeterBoostThread(
                meter, oldValue, newValue, state.Quarter.QuarterNumber);
            newState = newState.WithInbox(newState.Inbox.WithThreadAdded(boostThread));

            log = log.WithEntry(LogEntry.Info($"Spent {MeterBoostCost} PC to boost {meter}: {oldValue} → {newValue}"));

            return (newState, log);
        }

        // Handle spending PC to schmooze the board (2 PC for 1-5% favorability, with failure chance)
        if (input.HasBoardSchmooze)
        {
            const int SchmoozeCost = 2;
            const int FailureChance = 15; // 15% chance of backfire

            if (!newState.Resources.CanAfford(SchmoozeCost))
            {
                log = log.WithEntry(LogEntry.Info($"Insufficient PC for board schmoozing (need {SchmoozeCost})"));
                return (newState, log);
            }

            // Spend the PC
            newState = newState.WithResources(newState.Resources.WithSpend(SchmoozeCost));

            // Roll for success/failure
            var roll = rng.NextInt(0, 100);
            bool success = roll >= FailureChance;

            int favorabilityChange;
            if (success)
            {
                // Success: gain 1-5% favorability
                favorabilityChange = rng.NextInt(1, 6);
            }
            else
            {
                // Failure: lose 1-3% favorability
                favorabilityChange = -rng.NextInt(1, 4);
            }

            var newCEO = newState.CEO.WithFavorabilityChange(favorabilityChange);
            newState = newState.WithCEO(newCEO);

            // Generate humorous email
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var schmoozeThread = emailGen.CreateSchmoozingThread(
                success, favorabilityChange, newCEO.BoardFavorability, state.Quarter.QuarterNumber);
            newState = newState.WithInbox(newState.Inbox.WithThreadAdded(schmoozeThread));

            if (success)
            {
                log = log.WithEntry(LogEntry.Info($"Board schmoozing successful! Favorability +{favorabilityChange}% (now {newCEO.BoardFavorability}%)"));
            }
            else
            {
                log = log.WithEntry(LogEntry.Info($"Board schmoozing backfired! Favorability {favorabilityChange}% (now {newCEO.BoardFavorability}%)"));
            }

            return (newState, log);
        }

        // Handle spending PC to re-org hand (3 PC to discard hand and draw 5 new cards)
        if (input.HasReorg)
        {
            const int ReorgCost = 3;

            if (!newState.Resources.CanAfford(ReorgCost))
            {
                log = log.WithEntry(LogEntry.Info($"Insufficient PC for re-org (need {ReorgCost})"));
                return (newState, log);
            }

            // Spend the PC
            newState = newState.WithResources(newState.Resources.WithSpend(ReorgCost));

            // Discard current hand back to deck
            var currentHand = newState.Hand;
            var newCardDeck = newState.CardDeck;
            foreach (var card in currentHand.Cards)
            {
                newCardDeck = newCardDeck.Discard(card);
            }

            // Draw new cards up to max hand size
            var (deckAfterDraw, drawnCards) = newCardDeck.DrawMultiple(Cards.CardHand.MaxHandSize, rng);
            var newHand = Cards.CardHand.Empty.WithCardsAdded(drawnCards);

            newState = newState
                .WithCardDeck(deckAfterDraw)
                .WithHand(newHand);

            // Generate humorous email
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var reorgThread = emailGen.CreateReorgThread(
                currentHand.Cards.Count, drawnCards.Count, state.Quarter.QuarterNumber);
            newState = newState.WithInbox(newState.Inbox.WithThreadAdded(reorgThread));

            log = log.WithEntry(LogEntry.Info($"Re-org complete! Spent {ReorgCost} PC, drew {drawnCards.Count} new project cards"));

            return (newState, log);
        }

        // Handle spending PC to redeem evil (2 PC = -1 evil)
        if (input.HasEvilRedemption)
        {
            const int RedemptionCost = 2;

            if (!newState.Resources.CanAfford(RedemptionCost))
            {
                log = log.WithEntry(LogEntry.Info($"Insufficient PC for image rehabilitation (need {RedemptionCost})"));
                return (newState, log);
            }

            if (newState.CEO.EvilScore <= 0)
            {
                log = log.WithEntry(LogEntry.Info("Your reputation is already spotless!"));
                return (newState, log);
            }

            // Spend PC and reduce evil
            var oldEvilScore = newState.CEO.EvilScore;
            newState = newState.WithResources(newState.Resources.WithSpend(RedemptionCost));
            newState = newState.WithCEO(newState.CEO.WithEvilScoreChange(-1));

            // Generate humorous email
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var rehabThread = emailGen.CreateImageRehabThread(
                oldEvilScore, newState.CEO.EvilScore, state.Quarter.QuarterNumber);
            newState = newState.WithInbox(newState.Inbox.WithThreadAdded(rehabThread));

            log = log.WithEntry(LogEntry.Info($"Image rehabilitated! Spent {RedemptionCost} PC, Evil -{1} (now {newState.CEO.EvilScore})"));

            return (newState, log);
        }

        // Process played card if any
        if (input.HasPlayedCard && !string.IsNullOrEmpty(input.PlayedCardId))
        {
            var cardId = input.PlayedCardId;

            if (!state.Hand.Contains(cardId))
            {
                throw new ArgumentException($"Card {cardId} not in hand", nameof(input));
            }

            if (!state.CanPlayCard)
            {
                throw new InvalidOperationException("Cannot play more cards this quarter");
            }

            // Check PC cost for this card position
            var cardPosition = state.CardsPlayedThisQuarter.Count;
            var pcCost = QuarterGameState.GetCardPCCost(cardPosition);

            if (pcCost > 0)
            {
                if (!state.Resources.CanAfford(pcCost))
                {
                    throw new InvalidOperationException(
                        $"Insufficient PC to play card #{cardPosition + 1} (need {pcCost} PC, have {state.Resources.PoliticalCapital})");
                }

                // Deduct PC cost
                newState = newState.WithResources(newState.Resources.WithSpend(pcCost));
                log = log.WithEntry(LogEntry.Info($"Card #{cardPosition + 1} cost: {pcCost} PC"));
            }

            // Get risk modifier for this card position
            var positionRiskModifier = QuarterGameState.GetCardRiskModifier(cardPosition);

            // Find and play the card
            var card = state.Hand.Cards.First(c => c.CardId == cardId);
            log = log.WithEntry(LogEntry.Info($"Played: {card.Title}"));

            // Calculate meter affinity modifier (positive = better, negative = worse)
            var affinityModifier = card.GetAffinityModifier(state.Org);
            // Affinity affects risk: positive affinity reduces bad outcome chance
            // negative affinity increases bad outcome chance
            var totalRiskModifier = positionRiskModifier - affinityModifier;

            if (positionRiskModifier > 0)
            {
                log = log.WithEntry(LogEntry.Info($"Position risk: +{positionRiskModifier}% bad outcome chance"));
            }

            if (card.HasMeterAffinity)
            {
                var affinityMeter = card.MeterAffinity!.Value;
                if (affinityModifier > 0)
                {
                    log = log.WithEntry(LogEntry.Info($"Strong {affinityMeter}: -{affinityModifier}% risk (affinity bonus)"));
                }
                else if (affinityModifier < 0)
                {
                    log = log.WithEntry(LogEntry.Info($"Weak {affinityMeter}: +{-affinityModifier}% risk (affinity penalty)"));
                }
            }

            // Track meter changes and profit for email generation
            var meterDeltas = new List<(Meter Meter, int Delta)>();
            var outcomeTier = OutcomeTier.Expected;
            var profitDelta = 0;

            // Apply card effects with combined risk modifier
            if (card.Outcomes.Expected.Count > 0)
            {
                // Calculate affinity synergy bonus (matching affinities from cards played this quarter)
                int affinitySynergyBonus = state.GetAffinitySynergyBonus(card);

                outcomeTier = OutcomeRoller.Roll(
                    card.Outcomes,
                    state.Org.Alignment,
                    state.CEO.BoardPressureLevel,
                    rng,
                    state.CEO.EvilScore,
                    totalRiskModifier,  // Apply combined risk modifier
                    state.Quarter.QuarterNumber,  // Honeymoon period bonus
                    state.CEO.MomentumBonus,  // Consecutive success bonus
                    affinitySynergyBonus,  // Same-affinity cards bonus
                    card.IsCorporate);  // Evil path bonus for corporate cards

                // Log bonuses if they're active
                if (state.CEO.MomentumBonus > 0)
                {
                    log = log.WithEntry(LogEntry.Info($"Momentum: +{state.CEO.MomentumBonus}% good chance ({state.CEO.ConsecutiveSuccesses} streak)"));
                }
                if (affinitySynergyBonus > 0)
                {
                    log = log.WithEntry(LogEntry.Info($"Affinity synergy: +{affinitySynergyBonus}% good chance"));
                }
                if (card.IsCorporate && state.CEO.EvilScore >= 10)
                {
                    int evilPathBonus = state.CEO.EvilScore >= 20 ? 10 : 5;
                    log = log.WithEntry(LogEntry.Info($"Evil path: +{evilPathBonus}% good chance (Evil {state.CEO.EvilScore})"));
                }

                var effects = card.Outcomes.GetEffectsForTier(outcomeTier);
                log = log.WithEntry(LogEntry.Outcome(outcomeTier, card.Title, "played"));

                foreach (var effect in effects)
                {
                    // Track meter effects for email
                    if (effect is MeterEffect meterEffect)
                    {
                        meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
                    }

                    // Track profit effects - only Revenue cards affect profit
                    // Other project types only affect organizational meters
                    if (effect is ProfitEffect profitEffect && card.Category == Cards.CardCategory.Revenue)
                    {
                        // Apply target-relative scaling, delivery bonus, and diminishing returns
                        // Diminishing returns: playing 3 Revenue cards in a quarter yields less than 3x profit
                        var targetAmount = BoardDirective.ProfitIncrease.GetRequiredAmount(state.CEO.BoardPressureLevel);
                        var revenueCardsPlayedBefore = state.CardsPlayedThisQuarter
                            .Count(c => c.Category == Cards.CardCategory.Revenue);
                        var scaledProfit = ProfitCalculator.ScaleRevenueProfit(
                            profitEffect.Delta,
                            targetAmount,
                            newState.Org.Delivery,
                            revenueCardsPlayedBefore);
                        profitDelta += scaledProfit;
                    }

                    var (updatedState, entries) = effect.Apply(
                        CreateAdaptedGameState(newState),
                        rng);

                    newState = ApplyAdaptedState(newState, updatedState);
                    log = log.WithEntries(entries);
                }
            }

            // Apply profit effect to CEO state
            if (profitDelta != 0)
            {
                var newCEO = newState.CEO with
                {
                    CurrentQuarterProfit = newState.CEO.CurrentQuarterProfit + profitDelta
                };
                newState = newState.WithCEO(newCEO);
                var sign = profitDelta >= 0 ? "+" : "";
                log = log.WithEntry(LogEntry.Info($"Profit impact: {sign}${profitDelta}M"));
            }

            // Update momentum tracking based on outcome
            // Good outcomes continue/build streak, Bad outcomes reset it, Expected maintains current
            if (outcomeTier == OutcomeTier.Good)
            {
                var newCEO = newState.CEO.WithSuccessResult(true);
                newState = newState.WithCEO(newCEO);
                if (newCEO.ConsecutiveSuccesses >= 2)
                {
                    log = log.WithEntry(LogEntry.Info($"Momentum building: {newCEO.ConsecutiveSuccesses} consecutive successes!"));
                }
            }
            else if (outcomeTier == OutcomeTier.Bad)
            {
                if (newState.CEO.ConsecutiveSuccesses >= 2)
                {
                    log = log.WithEntry(LogEntry.Info("Momentum lost!"));
                }
                var newCEO = newState.CEO.WithSuccessResult(false);
                newState = newState.WithCEO(newCEO);
            }
            // Expected outcome: streak maintains (no change)

            // Handle corporate card
            if (card.IsCorporate)
            {
                var newCEO = newState.CEO.WithEvilScoreChange(card.CorporateIntensity);
                int favBump = 1 + (card.CorporateIntensity - 1);
                newCEO = newCEO.WithFavorabilityChange(favBump);

                newState = newState.WithCEO(newCEO);
                log = log.WithEntry(LogEntry.Info($"Corporate card: EvilScore +{card.CorporateIntensity}"));
            }

            // Generate email thread for this card play
            var evilScoreDelta = card.IsCorporate ? card.CorporateIntensity : 0;
            var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
            var cardThread = emailGen.CreateCardThread(
                card,
                outcomeTier,
                meterDeltas,
                state.Quarter.QuarterNumber,
                state.Org.Alignment,
                profitDelta,      // Include profit for Revenue card emails
                evilScoreDelta);  // Include evil score for Corporate cards
            var newInbox = newState.Inbox.WithThreadAdded(cardThread);

            // 15% chance to receive random fluff email (corporate noise)
            if (rng.NextInt(1, 101) <= 15)
            {
                var fluffGen = new FluffEmailGenerator(rng.NextInt(0, int.MaxValue));
                var fluffThread = fluffGen.GenerateFluffEmail(state.Quarter.QuarterNumber);
                newInbox = newInbox.WithThreadAdded(fluffThread);
            }

            newState = newState.WithInbox(newInbox);

            // Check for situation trigger from this card
            var seededRng = new SeededRng(HashCode.Combine(state.Seed, state.Quarter.QuarterNumber, card.CardId));
            PendingSituation? triggeredSituation = null;

            // First check card-specific situation mappings
            var cardSituations = CardSituationMappings.GetMappings(card.CardId);
            if (cardSituations is not null)
            {
                triggeredSituation = SituationResolver.CheckForTrigger(
                    cardSituations, outcomeTier, state.Quarter.QuarterNumber, seededRng,
                    originatingThreadId: cardThread.ThreadId);
            }

            // If no specific trigger, check generic situation pool
            // Chance increases with quarters played (5% base + 2% per quarter, max 25%)
            if (triggeredSituation is null)
            {
                triggeredSituation = SituationResolver.CheckGenericTrigger(
                    card.CardId, outcomeTier, state.Quarter.QuarterNumber, seededRng,
                    originatingThreadId: cardThread.ThreadId);
            }

            if (triggeredSituation is not null)
            {
                newState = newState.WithSituationQueued(triggeredSituation);
                var situationDef = SituationContent.Get(triggeredSituation.SituationId);
                if (situationDef is not null)
                {
                    var delay = triggeredSituation.ScheduledQuarter - state.Quarter.QuarterNumber;
                    if (delay == 0)
                    {
                        log = log.WithEntry(LogEntry.Info($"⚠ Situation triggered: {situationDef.Title} (immediate)"));
                    }
                    else
                    {
                        log = log.WithEntry(LogEntry.Info($"⚠ Situation brewing: {situationDef.Title} (Q+{delay})"));
                    }
                }
            }

            // Update hand and played cards
            var newHand = newState.Hand.WithCardRemoved(cardId);
            var newCardDeck = newState.CardDeck.Discard(card);

            newState = newState
                .WithHand(newHand)
                .WithCardDeck(newCardDeck)
                .WithCardPlayed(card);

            // Add card to pending follow-ups queue (for potential future events)
            var pendingFollowUp = PendingFollowUp.Create(
                card.CardId,
                card.Title,
                cardThread.ThreadId,
                state.Quarter.QuarterNumber,
                outcomeTier);
            newState = newState.WithFollowUpQueued(pendingFollowUp);

            // If player can still play another card and has enough PC, stay in PlayCards phase
            if (newState.CanPlayCard && newState.CanAffordNextCard && !input.EndPlayPhase)
            {
                return (newState, log);
            }
        }

        // Done playing cards - calculate restraint bonus and advance to Crisis phase
        var cardsPlayed = newState.CardsPlayedThisQuarter.Count;
        var restraintBonus = ResourceState.CalculateRestraintBonus(cardsPlayed);

        if (restraintBonus > 0)
        {
            newState = newState.WithResources(newState.Resources.WithPoliticalCapitalChange(restraintBonus));
            log = log.WithEntry(LogEntry.Info($"Restraint bonus: +{restraintBonus} PC (played {cardsPlayed} card{(cardsPlayed != 1 ? "s" : "")})"));
        }

        // Organizational neglect penalty: playing 3+ Revenue cards with no diversity
        // causes organizational decay and board skepticism (represents short-term profit over sustainable growth)
        var revenueCardsPlayed = newState.CardsPlayedThisQuarter
            .Count(c => c.Category == Cards.CardCategory.Revenue);
        if (revenueCardsPlayed >= 3)
        {
            // Revenue-only focus causes organizational decay AND board skepticism
            const int MeterPenalty = -8;
            const int FavorabilityPenalty = -15;
            var newOrg = newState.Org
                .WithMeterChange(Meter.Morale, MeterPenalty)
                .WithMeterChange(Meter.Governance, MeterPenalty)
                .WithMeterChange(Meter.Alignment, MeterPenalty);
            var newCEO = newState.CEO.WithFavorabilityChange(FavorabilityPenalty);
            newState = newState.WithOrg(newOrg).WithCEO(newCEO);
            log = log.WithEntry(LogEntry.Event($"Organizational neglect: Revenue-only focus hurts meters (-{-MeterPenalty}) and board favor (-{-FavorabilityPenalty})"));
        }

        // If no cards were played, replace 3 random cards from hand
        if (cardsPlayed == 0 && newState.Hand.Count >= 3)
        {
            var currentHand = newState.Hand.Cards.ToList();
            var cardsToReplace = currentHand.OrderBy(_ => rng.NextInt(0, 1000)).Take(3).ToList();

            // Discard selected cards back to deck
            var newCardDeck = newState.CardDeck;
            foreach (var card in cardsToReplace)
            {
                newCardDeck = newCardDeck.Discard(card);
            }

            // Remove from hand
            var remainingHand = currentHand.Where(c => !cardsToReplace.Contains(c)).ToList();

            // Draw 3 new cards
            var (deckAfterDraw, drawnCards) = newCardDeck.DrawMultiple(3, rng);
            var newHand = Cards.CardHand.Empty.WithCardsAdded(remainingHand).WithCardsAdded(drawnCards);

            newState = newState.WithCardDeck(deckAfterDraw).WithHand(newHand);
            log = log.WithEntry(LogEntry.Info("No projects executed - refreshed 3 cards from hand"));
        }

        log = log.WithEntry(LogEntry.Info("Ending card play phase"));
        newState = newState.WithQuarter(state.Quarter.NextPhase());

        return (newState, log);
    }

    /// <summary>
    /// Resolution phase - calculate profit, board reaction, ouster vote, and bonus award.
    /// Also processes crisis deadlines and Political Capital earning.
    /// Handles CEO retirement if player chooses to retire.
    /// </summary>
    private static (QuarterGameState NewState, QuarterLog Log) AdvanceResolution(
        QuarterGameState state,
        QuarterInput input,
        IRng rng)
    {
        var log = QuarterLog.Create(state.Quarter.QuarterNumber, state.Quarter.Phase);

        // Handle retirement choice (victory condition)
        if (input.HasRetirement && state.CEO.CanRetire)
        {
            log = log.WithEntry(LogEntry.Info("CEO RETIRES IN GLORY!"));
            var retiredCEO = state.CEO.WithRetirement();
            var score = ScoreCalculator.CalculateFinalScore(retiredCEO, state.Resources);
            log = log.WithEntry(LogEntry.Info($"Final Score: {score}"));
            return (state.WithCEO(retiredCEO), log);
        }

        log = log.WithEntry(LogEntry.Info($"Quarter {state.Quarter.QuarterNumber} Resolution"));

        var ceo = state.CEO;
        var org = state.Org;

        // 0. Apply ongoing crisis impacts
        var ongoingImpact = state.Crises.GetTotalOngoingImpact();
        foreach (var (meter, delta) in ongoingImpact)
        {
            org = org.WithMeterChange(meter, delta);
            if (delta != 0)
            {
                log = log.WithEntry(LogEntry.Info($"Crisis ongoing impact: {meter} {delta:+#;-#;0}"));
            }
        }

        // 0b. Process crisis deadlines - expire overdue crises
        var (newCrises, expiredCrises) = state.Crises.ProcessDeadlines(state.Quarter.QuarterNumber);
        foreach (var expired in expiredCrises)
        {
            log = log.WithEntry(LogEntry.Info($"Crisis expired: {expired.Title}"));
            // Apply base impact for expired crises
            foreach (var (meter, delta) in expired.BaseImpact)
            {
                org = org.WithMeterChange(meter, delta);
                log = log.WithEntry(LogEntry.Info($"  {meter}: {delta:+#;-#;0}"));
            }
        }

        // 0c. Passive meter recovery - organization stabilizes over time
        // Boost the two lowest meters slightly each quarter
        (org, log) = ApplyPassiveMeterRecovery(org, log);

        // 1. Calculate profit (base operations + project/crisis impacts)
        // Base operations grows organically over time (2% per quarter)
        int baseOperations = ProfitCalculator.CalculateBaseOperations(org, rng, ceo.QuartersSurvived);
        int projectImpact = ceo.CurrentQuarterProfit;
        int profit = ProfitCalculator.CalculateTotal(baseOperations, projectImpact);

        // Log project summary
        var cardsPlayedCount = state.CardsPlayedThisQuarter.Count;
        if (cardsPlayedCount > 0)
        {
            log = log.WithEntry(LogEntry.Info($"Projects Completed: {cardsPlayedCount}"));
            foreach (var card in state.CardsPlayedThisQuarter)
            {
                log = log.WithEntry(LogEntry.Info($"  • {card.Title}"));
            }
        }
        else
        {
            log = log.WithEntry(LogEntry.Info("Projects Completed: 0 (no strategic initiatives)"));
        }

        // Log the income breakdown
        log = log.WithEntry(LogEntry.Info($"Base Operations: {ProfitCalculator.FormatWithSign(baseOperations)}"));
        if (projectImpact != 0)
        {
            log = log.WithEntry(LogEntry.Info($"Project Impact: {ProfitCalculator.FormatWithSign(projectImpact)}"));
        }
        log = log.WithEntry(LogEntry.Info($"Total Quarterly Profit: {ProfitCalculator.Format(profit)}"));

        // 1b. Apply performance-based secondary effects (dice roll)
        var profitDelta = profit - ceo.LastQuarterProfit;
        (org, log) = ApplyPerformanceEffects(org, profit, profitDelta, cardsPlayedCount, rng, log);

        // 2. Check board directive
        var directive = state.CurrentDirective ?? BoardDirective.ProfitIncrease;
        bool directiveMet = directive.IsMet(ceo.LastQuarterProfit, profit, ceo.BoardPressureLevel);

        // Auto-fail directive if sustained inaction (2+ consecutive quarters with no/weak projects)
        // The board sees through a CEO who coasts on base operations without taking strategic initiative
        bool isWeakProjectQuarter = cardsPlayedCount == 0 || ceo.CurrentQuarterProfit <= 0;
        if (directiveMet && isWeakProjectQuarter && ceo.ConsecutiveWeakProjectQuarters >= 1)
        {
            directiveMet = false;
            log = log.WithEntry(LogEntry.Event("Board Override: Sustained lack of strategic initiative"));
        }

        log = log.WithEntry(LogEntry.Info(
            directiveMet
                ? $"Directive Met: {directive.GetDescription(ceo.BoardPressureLevel)}"
                : $"Directive Failed: {directive.GetDescription(ceo.BoardPressureLevel)}"));

        // 3. Update favorability (evil score, weak project streak, and tenure affect board)
        var projectRevenue = ceo.CurrentQuarterProfit; // Revenue from Revenue cards this quarter
        int favChange = FavorabilityCalculator.Calculate(
            ceo.LastQuarterProfit, profit, directiveMet, ceo.BoardPressureLevel, ceo.EvilScore,
            weakProjectStreak: ceo.ConsecutiveWeakProjectQuarters,
            quartersSurvived: ceo.QuartersSurvived);

        // Apply tenure decay - board expectations rise over time
        int tenureDecay = FavorabilityCalculator.GetTenureDecay(ceo.QuartersSurvived);
        favChange += tenureDecay;

        // No projects = no positive favorability. The board expects active leadership.
        // Base operations running profitably doesn't impress them - that's the bare minimum.
        if (cardsPlayedCount == 0 && favChange > 0)
        {
            favChange = 0;
            log = log.WithEntry(LogEntry.Info("Board unimpressed: No strategic initiatives executed"));
        }

        log = log.WithEntry(LogEntry.Info($"Board Favorability: {favChange:+#;-#;0}"));
        if (tenureDecay < 0)
        {
            log = log.WithEntry(LogEntry.Info($"Board expectations risen ({tenureDecay} tenure adjustment)"));
        }
        if (ceo.EvilScore >= 10 && !directiveMet)
        {
            log = log.WithEntry(LogEntry.Info($"Evil scrutiny applied (Evil: {ceo.EvilScore})"));
        }
        if (ceo.ConsecutiveWeakProjectQuarters >= 2)
        {
            log = log.WithEntry(LogEntry.Info($"Weak project streak penalty (Q{ceo.ConsecutiveWeakProjectQuarters + 1} of weak initiatives)"));
        }

        // 3a. Add favor bonus for executing strategic initiatives (+1 if any cards played)
        int initiativeFavorBonus = cardsPlayedCount > 0 ? 1 : 0;
        if (initiativeFavorBonus > 0)
        {
            favChange += initiativeFavorBonus;
            log = log.WithEntry(LogEntry.Info($"Initiative Bonus: +{initiativeFavorBonus} Favor (active leadership)"));
        }

        // 3b. Apply low meter penalty - critically low meters cap positive gains and add penalties
        var (maxPositiveGain, lowMeterPenalty, lowMeterReason) = FavorabilityCalculator.GetLowMeterAdjustment(org);
        if (lowMeterPenalty != 0)
        {
            favChange += lowMeterPenalty;
            log = log.WithEntry(LogEntry.Info($"Board Concern: {lowMeterReason} ({lowMeterPenalty:+#;-#;0})"));
        }
        if (maxPositiveGain < int.MaxValue && favChange > maxPositiveGain)
        {
            var originalChange = favChange;
            favChange = maxPositiveGain;
            log = log.WithEntry(LogEntry.Info($"Favor capped: {lowMeterReason ?? "Critical metrics"} (was {originalChange:+#;-#;0}, now {favChange:+#;-#;0})"));
        }

        // 3b2. Apply low activity penalty - board expects more projects as tenure increases
        var (activityMaxGain, activityPenalty, activityReason) = FavorabilityCalculator.GetLowActivityAdjustment(
            cardsPlayedCount, ceo.QuartersSurvived);
        if (activityPenalty != 0)
        {
            favChange += activityPenalty;
            log = log.WithEntry(LogEntry.Info($"Activity Concern: {activityReason} ({activityPenalty:+#;-#;0})"));
        }
        if (activityMaxGain < int.MaxValue && favChange > activityMaxGain)
        {
            var originalChange = favChange;
            favChange = Math.Min(favChange, activityMaxGain);
            if (favChange != originalChange)
            {
                log = log.WithEntry(LogEntry.Info($"Favor capped: {activityReason ?? "Low activity"} (was {originalChange:+#;-#;0}, now {favChange:+#;-#;0})"));
            }
        }

        // 3c. Calculate quarterly board bonus
        var (calculatedBonus, calculatedReasons) = ScoreCalculator.CalculateQuarterlyBonus(
            ceo, org, directiveMet, profitDelta);

        // No bonus for quarters with no strategic initiative - board rewards active leadership
        int bonusAmount;
        IReadOnlyList<string> bonusReasons;
        if (cardsPlayedCount == 0)
        {
            bonusAmount = 0;
            bonusReasons = new[] { "No strategic initiatives executed - the board expects active leadership" };
            log = log.WithEntry(LogEntry.Info("No Bonus: No strategic initiatives executed this quarter"));
        }
        else
        {
            bonusAmount = calculatedBonus;
            bonusReasons = calculatedReasons;
            log = log.WithEntry(LogEntry.Info($"Quarterly Bonus: ${bonusAmount}M"));
            foreach (var reason in bonusReasons)
            {
                log = log.WithEntry(LogEntry.Info($"  {reason}"));
            }
        }

        // 4. Update CEO state (includes bonus award, evil snapshot, profit tracking, and project performance)
        var newCEO = ceo
            .WithProfitAdded(profit)
            .WithProfitRecorded(profit) // Track for trajectory/smoothing
            .WithProjectPerformanceRecorded(projectRevenue) // Track weak project streak
            .WithCardsPlayedRecorded(cardsPlayedCount) // Track lifetime cards for parachute
            .WithFavorabilityChange(favChange)
            .WithQuarterComplete()
            .WithBonusAwarded(bonusAmount)
            .WithEvilSnapshotForQuarter() with
        {
            LastQuarterProfit = profit,
            CurrentQuarterProfit = 0
        };

        // 4b. Calculate exceptional quarter metric rewards
        var metricRewards = CalculateExceptionalRewards(directiveMet, profitDelta, bonusAmount, org, rng);
        foreach (var (meter, delta) in metricRewards)
        {
            org = org.WithMeterChange(meter, delta);
            log = log.WithEntry(LogEntry.Info($"Board Award: +{delta} {meter}"));
        }

        // 5. Check for ouster (now uses trajectory tracking for fairer evaluation)
        bool ousted = OusterCalculator.RollForOuster(
            newCEO.BoardFavorability,
            newCEO.BoardPressureLevel,
            rng,
            newCEO.QuartersSurvived,
            newCEO.EvilScore,
            directiveMet,
            profitPositive: profit >= 0,
            profitImproving: newCEO.IsProfitImproving,
            consecutiveNegativeQuarters: newCEO.ConsecutiveNegativeQuarters,
            consecutiveWeakProjectQuarters: newCEO.ConsecutiveWeakProjectQuarters,
            cardsPlayedThisQuarter: cardsPlayedCount);

        // 5b. Generate board decision email with income statement and bonus
        var emailGen = new EmailGenerator(rng.NextInt(0, int.MaxValue));
        var requiredIncrease = directive.GetRequiredAmount(ceo.BoardPressureLevel);
        var boardDecisionThread = emailGen.CreateBoardDecisionThread(
            state.Quarter.QuarterNumber,
            baseOperations,
            projectImpact,
            profit,
            ceo.LastQuarterProfit,
            requiredIncrease,
            directiveMet,
            favChange,
            newCEO.BoardFavorability,
            newCEO.BoardPressureLevel,
            org,
            survived: !ousted,
            bonusAmount: bonusAmount,
            bonusReasons: bonusReasons,
            accumulatedBonus: newCEO.AccumulatedBonus,
            canRetire: newCEO.CanRetire,
            metricRewards: metricRewards.Count > 0 ? metricRewards : null);
        var newInbox = state.Inbox.WithThreadAdded(boardDecisionThread);

        if (ousted)
        {
            newCEO = newCEO.WithOusted();
            log = log.WithEntry(LogEntry.Info($"CEO OUSTED! Golden Parachute: {newCEO.ParachuteDescription}"));

            return (state.WithCEO(newCEO).WithOrg(org).WithInbox(newInbox).WithCurrentCrisis(null), log);
        }

        log = log.WithEntry(LogEntry.Info($"Survived Quarter {state.Quarter.QuarterNumber}. " +
            $"Favorability: {newCEO.BoardFavorability}, Pressure: {newCEO.BoardPressureLevel}"));

        // 5c. Check and log retirement eligibility
        if (newCEO.CanRetire)
        {
            log = log.WithEntry(LogEntry.Info($"RETIREMENT AVAILABLE! Accumulated Bonus: ${newCEO.AccumulatedBonus}M"));
        }
        else
        {
            log = log.WithEntry(LogEntry.Info($"Accumulated Bonus: ${newCEO.AccumulatedBonus}M (${CEOState.RetirementThreshold}M to retire)"));
        }

        // 6. Update Political Capital based on org metrics
        var oldPC = state.Resources.PoliticalCapital;
        var newResources = state.Resources.WithTurnEndAdjustments(org);
        var pcDelta = newResources.PoliticalCapital - oldPC;
        if (pcDelta != 0)
        {
            log = log.WithEntry(LogEntry.Info($"Political Capital: {pcDelta:+#;-#;0} (now {newResources.PoliticalCapital})"));
        }

        // 7. Setup next quarter
        var nextQuarter = state.Quarter.NextPhase();

        // Generate new board directive for next quarter
        var nextDirective = BoardDirective.Generate(newCEO.BoardPressureLevel, rng);

        // Draw cards to refill hand
        var (newCardDeck, drawnCards) = state.CardDeck.DrawMultiple(
            CardHand.MaxHandSize - state.Hand.Count, rng);
        var newHand = state.Hand.WithCardsAdded(drawnCards);

        var newState = state
            .WithOrg(org)
            .WithCEO(newCEO)
            .WithQuarter(nextQuarter)
            .WithCardDeck(newCardDeck)
            .WithHand(newHand)
            .WithInbox(newInbox)
            .WithPlayedCardsCleared()
            .WithCurrentDirective(nextDirective)
            .WithResources(newResources)
            .WithCrises(newCrises);

        return (newState, log);
    }

    // Helper to adapt QuarterGameState for existing IEffect interface
    private static GameState CreateAdaptedGameState(QuarterGameState state)
    {
        return GameState.NewGame(state.Seed).WithOrg(state.Org);
    }

    // Helper to apply effect results back to QuarterGameState
    private static QuarterGameState ApplyAdaptedState(QuarterGameState original, GameState adapted)
    {
        return original.WithOrg(adapted.Org);
    }

    /// <summary>
    /// Apply secondary performance effects at quarter end based on financial results.
    /// Good results boost morale/alignment/runway; bad results hurt them.
    /// Project execution (playing cards with positive profit) boosts delivery.
    /// All effects are dice-roll based for variability.
    /// </summary>
    private static (OrgState Org, QuarterLog Log) ApplyPerformanceEffects(
        OrgState org,
        int quarterProfit,
        int profitDelta,
        int cardsPlayed,
        IRng rng,
        QuarterLog log)
    {
        var effectsApplied = new List<string>();

        // Financial results affect morale, alignment, and runway
        // Excellent results (profit increased by $15M+)
        if (profitDelta >= 15)
        {
            var moraleBoost = rng.NextInt(2, 7);    // +2 to +6
            var alignmentBoost = rng.NextInt(1, 5); // +1 to +4
            var runwayBoost = rng.NextInt(2, 6);    // +2 to +5

            org = org.WithMeterChange(Meter.Morale, moraleBoost);
            org = org.WithMeterChange(Meter.Alignment, alignmentBoost);
            org = org.WithMeterChange(Meter.Runway, runwayBoost);

            effectsApplied.Add($"Excellent results! Morale +{moraleBoost}, Alignment +{alignmentBoost}, Runway +{runwayBoost}");
        }
        // Good results (profit increased by $5M+)
        else if (profitDelta >= 5)
        {
            var moraleBoost = rng.NextInt(1, 4);    // +1 to +3
            var alignmentBoost = rng.NextInt(0, 3); // +0 to +2
            var runwayBoost = rng.NextInt(1, 4);    // +1 to +3

            if (moraleBoost > 0) org = org.WithMeterChange(Meter.Morale, moraleBoost);
            if (alignmentBoost > 0) org = org.WithMeterChange(Meter.Alignment, alignmentBoost);
            if (runwayBoost > 0) org = org.WithMeterChange(Meter.Runway, runwayBoost);

            var parts = new List<string>();
            if (moraleBoost > 0) parts.Add($"Morale +{moraleBoost}");
            if (alignmentBoost > 0) parts.Add($"Alignment +{alignmentBoost}");
            if (runwayBoost > 0) parts.Add($"Runway +{runwayBoost}");
            if (parts.Count > 0)
                effectsApplied.Add($"Good results! {string.Join(", ", parts)}");
        }
        // Poor results (profit decreased by $15M+)
        else if (profitDelta <= -15)
        {
            var moralePenalty = rng.NextInt(2, 7);    // -2 to -6
            var alignmentPenalty = rng.NextInt(1, 5); // -1 to -4
            var runwayPenalty = rng.NextInt(2, 6);    // -2 to -5

            org = org.WithMeterChange(Meter.Morale, -moralePenalty);
            org = org.WithMeterChange(Meter.Alignment, -alignmentPenalty);
            org = org.WithMeterChange(Meter.Runway, -runwayPenalty);

            effectsApplied.Add($"Poor results! Morale -{moralePenalty}, Alignment -{alignmentPenalty}, Runway -{runwayPenalty}");
        }
        // Bad results (profit decreased by $5M+)
        else if (profitDelta <= -5)
        {
            var moralePenalty = rng.NextInt(1, 4);    // -1 to -3
            var alignmentPenalty = rng.NextInt(0, 3); // -0 to -2
            var runwayPenalty = rng.NextInt(1, 4);    // -1 to -3

            if (moralePenalty > 0) org = org.WithMeterChange(Meter.Morale, -moralePenalty);
            if (alignmentPenalty > 0) org = org.WithMeterChange(Meter.Alignment, -alignmentPenalty);
            if (runwayPenalty > 0) org = org.WithMeterChange(Meter.Runway, -runwayPenalty);

            var parts = new List<string>();
            if (moralePenalty > 0) parts.Add($"Morale -{moralePenalty}");
            if (alignmentPenalty > 0) parts.Add($"Alignment -{alignmentPenalty}");
            if (runwayPenalty > 0) parts.Add($"Runway -{runwayPenalty}");
            if (parts.Count > 0)
                effectsApplied.Add($"Disappointing results: {string.Join(", ", parts)}");
        }

        // Project execution affects delivery
        // Playing cards and generating profit = good execution
        if (cardsPlayed > 0)
        {
            // Strong delivery: played projects with good profit
            if (quarterProfit >= 20)
            {
                var deliveryBoost = rng.NextInt(2, 7); // +2 to +6
                org = org.WithMeterChange(Meter.Delivery, deliveryBoost);
                effectsApplied.Add($"Strong project execution! Delivery +{deliveryBoost}");
            }
            // Decent delivery: played projects with modest profit
            else if (quarterProfit >= 10)
            {
                var deliveryBoost = rng.NextInt(1, 4); // +1 to +3
                org = org.WithMeterChange(Meter.Delivery, deliveryBoost);
                effectsApplied.Add($"Solid project execution. Delivery +{deliveryBoost}");
            }
            // Poor execution: played projects but lost significant money
            else if (quarterProfit <= -10)
            {
                var deliveryPenalty = rng.NextInt(1, 4); // -1 to -3
                org = org.WithMeterChange(Meter.Delivery, -deliveryPenalty);
                effectsApplied.Add($"Project execution issues. Delivery -{deliveryPenalty}");
            }
            // Terrible execution: major losses despite projects
            else if (quarterProfit <= -20)
            {
                var deliveryPenalty = rng.NextInt(2, 7); // -2 to -6
                org = org.WithMeterChange(Meter.Delivery, -deliveryPenalty);
                effectsApplied.Add($"Project failures impacting operations. Delivery -{deliveryPenalty}");
            }
        }

        // Log all effects
        foreach (var effect in effectsApplied)
        {
            log = log.WithEntry(LogEntry.Info(effect));
        }

        return (org, log);
    }

    /// <summary>
    /// Calculates exceptional quarter rewards (metric bonuses) for outstanding performance.
    /// Board may award small metric boosts for truly exceptional quarters.
    /// </summary>
    private static List<(Meter Meter, int Delta)> CalculateExceptionalRewards(
        bool directiveMet,
        int profitDelta,
        int bonusAmount,
        OrgState org,
        IRng rng)
    {
        var rewards = new List<(Meter Meter, int Delta)>();

        // Exceptional bonus threshold (earned max or near-max bonus)
        var isExceptionalQuarter = bonusAmount >= 10 && directiveMet;

        // Outstanding profit growth
        var isOutstandingGrowth = profitDelta >= 30;

        if (!isExceptionalQuarter && !isOutstandingGrowth)
        {
            return rewards;
        }

        // 40% chance of a board discretionary award for exceptional quarters
        if (rng.NextInt(0, 100) >= 40)
        {
            return rewards;
        }

        // Select what to award based on what needs help (but still small amounts)
        if (isOutstandingGrowth)
        {
            // Strong growth = more budget
            var runwayReward = rng.NextInt(3, 8); // +3 to +7
            rewards.Add((Meter.Runway, runwayReward));
        }

        if (isExceptionalQuarter)
        {
            // Find the lowest non-critical meter to give a small boost
            var lowestMeter = GetLowestMeter(org);

            if (lowestMeter.HasValue && org.GetMeter(lowestMeter.Value) < 70)
            {
                var boost = rng.NextInt(2, 6); // +2 to +5
                rewards.Add((lowestMeter.Value, boost));
            }
        }

        return rewards;
    }

    private static Meter? GetLowestMeter(OrgState org)
    {
        var meters = new[]
        {
            (Meter.Delivery, org.Delivery),
            (Meter.Morale, org.Morale),
            (Meter.Governance, org.Governance),
            (Meter.Alignment, org.Alignment),
            (Meter.Runway, org.Runway)
        };

        var lowest = meters.OrderBy(m => m.Item2).First();
        return lowest.Item2 < 70 ? lowest.Item1 : null;
    }

    /// <summary>
    /// Applies passive meter recovery each quarter.
    /// The organization naturally stabilizes - lowest meters get boosts.
    /// This prevents death spirals and extends game length.
    /// </summary>
    private static (OrgState Org, QuarterLog Log) ApplyPassiveMeterRecovery(OrgState org, QuarterLog log)
    {
        var meters = new[]
        {
            (Meter.Delivery, org.Delivery),
            (Meter.Morale, org.Morale),
            (Meter.Governance, org.Governance),
            (Meter.Alignment, org.Alignment),
            (Meter.Runway, org.Runway)
        };

        // Sort by value to find the lowest meters
        var sorted = meters.OrderBy(m => m.Item2).ToList();

        var recoveries = new List<(Meter, int)>();

        // Lowest meter gets +5 if below 50, +3 if below 60
        if (sorted[0].Item2 < 50)
        {
            var boost = Math.Min(5, 50 - sorted[0].Item2);
            if (boost > 0)
            {
                org = org.WithMeterChange(sorted[0].Item1, boost);
                recoveries.Add((sorted[0].Item1, boost));
            }
        }
        else if (sorted[0].Item2 < 60)
        {
            org = org.WithMeterChange(sorted[0].Item1, 3);
            recoveries.Add((sorted[0].Item1, 3));
        }

        // Second lowest gets +3 if below 45
        if (sorted[1].Item2 < 45)
        {
            var boost = Math.Min(3, 45 - sorted[1].Item2);
            if (boost > 0)
            {
                org = org.WithMeterChange(sorted[1].Item1, boost);
                recoveries.Add((sorted[1].Item1, boost));
            }
        }

        // Third lowest gets +2 if below 35
        if (sorted[2].Item2 < 35)
        {
            var boost = Math.Min(2, 35 - sorted[2].Item2);
            if (boost > 0)
            {
                org = org.WithMeterChange(sorted[2].Item1, boost);
                recoveries.Add((sorted[2].Item1, boost));
            }
        }

        // Log recoveries if any occurred
        if (recoveries.Count > 0)
        {
            var recoveryDesc = string.Join(", ", recoveries.Select(r => $"{r.Item1} +{r.Item2}"));
            log = log.WithEntry(LogEntry.Info($"Org stabilization: {recoveryDesc}"));
        }

        return (org, log);
    }

    /// <summary>
    /// Applies meter effects from follow-up events.
    /// </summary>
    private static QuarterGameState ApplyMeterEffects(
        QuarterGameState state,
        IReadOnlyList<Situation.MeterEffect>? effects)
    {
        if (effects is null || effects.Count == 0)
            return state;

        var org = state.Org;
        foreach (var effect in effects)
        {
            org = org.WithMeterChange(effect.Meter, effect.Delta);
        }
        return state.WithOrg(org);
    }
}
