using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Content;

namespace InertiCorp.Game;

/// <summary>
/// UI phase for display state management.
/// </summary>
public enum UIPhase
{
    ShowingBoardDemand,
    PlayingCards,
    ShowingCrisis,
    ShowingResolution,
    GameOver
}

/// <summary>
/// Central game manager that bridges Core simulation with Godot UI.
/// Uses the 4-phase card game engine: BoardDemand → PlayCards → Crisis → Resolution.
/// Emails are generated immediately during PlayCards and Crisis phases.
/// </summary>
public partial class GameManager : Node
{
    /// <summary>
    /// Current game state (read-only for UI).
    /// </summary>
    public QuarterGameState? CurrentState { get; private set; }

    /// <summary>
    /// The RNG instance used for this game session.
    /// </summary>
    private SeededRng? _rng;

    /// <summary>
    /// The current crisis event card being displayed (null if not in crisis phase).
    /// </summary>
    public EventCard? CurrentCrisis => CurrentState?.CurrentCrisis;

    /// <summary>
    /// The last phase's log entries (for display).
    /// </summary>
    public QuarterLog? LastLog { get; private set; }

    /// <summary>
    /// Current UI phase.
    /// </summary>
    public UIPhase Phase { get; private set; } = UIPhase.ShowingBoardDemand;

    /// <summary>
    /// Signal emitted when the game state changes.
    /// </summary>
    [Signal]
    public delegate void StateChangedEventHandler();

    /// <summary>
    /// Signal emitted when phase changes.
    /// </summary>
    [Signal]
    public delegate void PhaseChangedEventHandler();

    /// <summary>
    /// Signal emitted when the game ends (ousted).
    /// </summary>
    [Signal]
    public delegate void GameEndedEventHandler(bool isWon);

    public override void _Ready()
    {
        StartNewGame();
    }

    /// <summary>
    /// Starts a new game with a random seed.
    /// </summary>
    public void StartNewGame()
    {
        var seed = (int)System.DateTime.Now.Ticks;
        StartNewGame(seed);
    }

    /// <summary>
    /// Starts a new game with the specified seed.
    /// </summary>
    public void StartNewGame(int seed)
    {
        _rng = new SeededRng(seed);

        // Create deck set from game content (Crisis, Board, Project decks)
        var deckSet = GameContent.CreateDeckSet();

        // Get playable cards
        var playableCards = GameContent.PlayableCardDeck;

        // Initialize game state with playable cards
        CurrentState = QuarterGameState.NewGame(seed, deckSet, playableCards);
        LastLog = null;
        Phase = UIPhase.ShowingBoardDemand;

        GD.Print($"[GameManager] New CEO survival game started with seed {seed}");
        GD.Print($"[GameManager] Quarter {CurrentState.Quarter.QuarterNumber}, Phase: {CurrentState.Quarter.Phase}");
        GD.Print($"[GameManager] Starting hand: {CurrentState.Hand.Count} cards");
        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Advances past the Board Demand phase.
    /// </summary>
    public void AdvanceBoardDemand()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.BoardDemand) return;

        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
        CurrentState = newState;
        LastLog = log;
        Phase = UIPhase.PlayingCards; // Now goes to PlayCards first

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Makes a choice for the crisis card.
    /// </summary>
    public void MakeCrisisChoice(string choiceId)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Crisis) return;

        var input = QuarterInput.ForChoice(choiceId);

        try
        {
            var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
            CurrentState = newState;
            LastLog = log;
            Phase = UIPhase.ShowingResolution; // Crisis now goes directly to Resolution

            LogPhase(log);
            EmitSignal(SignalName.StateChanged);
            EmitSignal(SignalName.PhaseChanged);
        }
        catch (System.ArgumentException ex)
        {
            GD.PrintErr($"[GameManager] Invalid choice: {ex.Message}");
        }
    }

    /// <summary>
    /// Makes a choice for an email reply chain.
    /// </summary>
    public void MakeReplyChainChoice(string choiceId)
    {
        if (CurrentState is null || _rng is null) return;
        if (!CurrentState.HasActiveReplyChain) return;

        var input = QuarterInput.ForReplyChain(choiceId);

        try
        {
            var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
            CurrentState = newState;
            LastLog = log;

            LogPhase(log);
            GD.Print($"[GameManager] Reply chain choice made: {choiceId}");
            EmitSignal(SignalName.StateChanged);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[GameManager] Reply chain error: {ex.Message}");
        }
    }

    /// <summary>
    /// Plays a card from hand.
    /// </summary>
    public void PlayCard(string cardId, bool endPhaseAfter = false)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        var input = QuarterInput.ForPlayCard(cardId, endPhaseAfter);

        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        // Check if we moved to next phase (now Crisis comes after PlayCards)
        if (CurrentState.Quarter.Phase == GamePhase.Crisis)
        {
            // Generate the crisis email immediately (if there's a crisis)
            var (crisisState, crisisLog) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
            CurrentState = crisisState;
            LogPhase(crisisLog);

            // Check if crisis was skipped (no crisis drawn this quarter)
            if (CurrentState.Quarter.Phase == GamePhase.Resolution)
            {
                Phase = UIPhase.ShowingResolution;
            }
            else
            {
                Phase = UIPhase.ShowingCrisis;
            }
        }

        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Ends the play cards phase without playing more cards.
    /// </summary>
    public void EndPlayCardsPhase()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.EndCardPlay, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        // Generate the crisis email immediately (if there's a crisis)
        if (CurrentState.Quarter.Phase == GamePhase.Crisis)
        {
            var (crisisState, crisisLog) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
            CurrentState = crisisState;
            LogPhase(crisisLog);

            // Check if crisis was skipped (no crisis drawn this quarter)
            if (CurrentState.Quarter.Phase == GamePhase.Resolution)
            {
                Phase = UIPhase.ShowingResolution;
            }
            else
            {
                Phase = UIPhase.ShowingCrisis;
            }
        }
        else
        {
            Phase = UIPhase.ShowingCrisis;
        }

        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Forces the crisis phase to advance to resolution.
    /// Used as a fallback when the crisis email is missing or the game is stuck.
    /// </summary>
    public void ForceAdvanceCrisis()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Crisis) return;

        // Clear the current crisis and advance to resolution
        var clearedState = CurrentState.WithCurrentCrisis(null);
        var (newState, log) = QuarterEngine.Advance(clearedState, QuarterInput.Empty, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        Phase = UIPhase.ShowingResolution;

        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Advances through Resolution to next quarter.
    /// </summary>
    public void AdvanceResolution()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Resolution) return;

        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        if (CurrentState.CEO.IsOusted)
        {
            Phase = UIPhase.GameOver;
            EmitSignal(SignalName.GameEnded, false); // false = ousted
        }
        else
        {
            Phase = UIPhase.ShowingBoardDemand;
        }

        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// CEO chooses to retire (victory condition).
    /// </summary>
    public void ChooseRetirement()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Resolution) return;
        if (!CurrentState.CEO.CanRetire) return;

        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.ForRetirement, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        Phase = UIPhase.GameOver;
        EmitSignal(SignalName.GameEnded, true); // true = retired (victory)

        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Legacy method for compatibility - routes to appropriate phase handler.
    /// </summary>
    public void MakeChoice(string choiceId)
    {
        if (CurrentState?.Quarter.Phase == GamePhase.Crisis)
        {
            MakeCrisisChoice(choiceId);
        }
    }

    /// <summary>
    /// Legacy method for compatibility.
    /// </summary>
    public void ContinueToNextTurn()
    {
        if (CurrentState is null) return;

        switch (CurrentState.Quarter.Phase)
        {
            case GamePhase.BoardDemand:
                AdvanceBoardDemand();
                break;
            case GamePhase.PlayCards:
                EndPlayCardsPhase();
                break;
            case GamePhase.Resolution:
                AdvanceResolution();
                break;
        }
    }

    /// <summary>
    /// Marks an email thread as read.
    /// </summary>
    public void MarkThreadRead(string threadId)
    {
        if (CurrentState is null) return;

        var newInbox = CurrentState.Inbox.WithThreadRead(threadId);
        CurrentState = CurrentState.WithInbox(newInbox);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Exchanges organizational meter value for Political Capital.
    /// </summary>
    public void ExchangeMeterForPC(Meter meter, int amount = 1)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        var input = QuarterInput.ForMeterExchange(meter, amount);
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Spends 1 PC to boost a meter by 5 points.
    /// </summary>
    public void SpendPCToBoostMeter(Meter meter)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;
        if (!CanAffordMeterBoost) return;  // Check balance before spending

        var input = QuarterInput.ForMeterBoost(meter);
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Spends 2 PC to schmooze the board for a chance at improved favorability.
    /// Has a small chance of backfiring.
    /// </summary>
    public void SpendPCToSchmoozeBoard()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;
        if (!CanAffordSchmooze) return;  // Check balance before spending

        var input = QuarterInput.ForBoardSchmooze;
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Whether the player can afford to boost a meter (costs 1 PC).
    /// </summary>
    public bool CanAffordMeterBoost => CurrentState?.Resources.CanAfford(1) ?? false;

    /// <summary>
    /// Whether the player can afford to schmooze the board (costs 2 PC).
    /// </summary>
    public bool CanAffordSchmooze => CurrentState?.Resources.CanAfford(2) ?? false;

    /// <summary>
    /// Whether the player can afford to re-org their hand (costs 3 PC).
    /// </summary>
    public bool CanAffordReorg => CurrentState?.Resources.CanAfford(3) ?? false;

    /// <summary>
    /// Spends 3 PC to discard current hand and draw 5 new cards.
    /// </summary>
    public void SpendPCToReorg()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;
        if (!CanAffordReorg) return;  // Check balance before spending

        var input = QuarterInput.ForReorg;
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Whether the player can afford to rehabilitate their image (costs 2 PC).
    /// </summary>
    public bool CanAffordEvilRedemption => CurrentState?.Resources.CanAfford(2) ?? false;

    /// <summary>
    /// Whether the player has any evil to redeem.
    /// </summary>
    public bool HasEvilToRedeem => (CurrentState?.CEO.EvilScore ?? 0) > 0;

    /// <summary>
    /// Spends 2 PC to reduce evil score by 1.
    /// </summary>
    public void SpendPCToRedeemEvil()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;
        if (!CanAffordEvilRedemption) return;
        if (!HasEvilToRedeem) return;

        var input = QuarterInput.ForEvilRedemption;
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Triggers the crisis phase to generate the crisis email (if not already present).
    /// Call this when entering crisis phase to show the email before requiring a choice.
    /// </summary>
    public void InitializeCrisisEmail()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Crisis) return;

        // Generate crisis email without making a choice
        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Whether the player can afford to play another card.
    /// </summary>
    public bool CanAffordNextCard => CurrentState?.CanAffordNextCard ?? false;

    /// <summary>
    /// Gets the PC cost for playing the next card.
    /// </summary>
    public int NextCardPCCost => CurrentState?.GetNextCardPCCost() ?? 0;

    /// <summary>
    /// Gets the risk modifier (%) for playing the next card.
    /// </summary>
    public int NextCardRiskModifier => CurrentState?.GetNextCardRiskModifier() ?? 0;

    /// <summary>
    /// Current Political Capital.
    /// </summary>
    public int PoliticalCapital => CurrentState?.Resources.PoliticalCapital ?? 0;

    /// <summary>
    /// Triggers a random crisis interrupt (from the background timer).
    /// This adds a crisis to the inbox without changing the current phase.
    /// Player must respond before they can end the phase.
    /// </summary>
    public void TriggerRandomCrisis()
    {
        if (CurrentState is null || _rng is null) return;

        // Don't trigger if already have a pending crisis
        if (CurrentState.CurrentCrisis is not null) return;
        if (CurrentState.Quarter.Phase == GamePhase.Resolution) return;
        if (CurrentState.CEO.IsOusted || CurrentState.CEO.HasRetired) return;

        // Draw a crisis card
        var (newEventDecks, crisisCard) = CurrentState.EventDecks.DrawCrisis(_rng);
        if (crisisCard is null)
        {
            GD.Print("[GameManager] No crisis cards available to draw");
            return;
        }

        GD.Print($"[GameManager] INTERRUPT CRISIS: {crisisCard.Title}");

        // Add the crisis without changing the phase - player must respond before ending phase
        CurrentState = CurrentState
            .WithEventDecks(newEventDecks)
            .WithCurrentCrisis(crisisCard);

        // Generate the crisis email
        var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
        var crisisThread = emailGen.CreateCrisisThread(
            crisisCard,
            CurrentState.Quarter.QuarterNumber,
            CurrentState.Org.Alignment,
            CurrentState.CEO.BoardPressureLevel,
            CurrentState.CEO.EvilScore);

        var newInbox = CurrentState.Inbox.WithThreadAdded(crisisThread);
        CurrentState = CurrentState.WithInbox(newInbox);

        // Don't change UI phase - just signal state changed so inbox updates
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Whether there's an unresolved crisis that must be handled before ending the phase.
    /// Only returns true if we're in Crisis phase (where the crisis email has been generated).
    /// </summary>
    public bool HasPendingCrisis =>
        CurrentState?.CurrentCrisis is not null &&
        CurrentState?.Quarter.Phase == GamePhase.Crisis;

    /// <summary>
    /// Responds to a pending crisis (from any phase).
    /// This allows handling interrupt crises without changing the game phase.
    /// </summary>
    public void RespondToPendingCrisis(string choiceId)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.CurrentCrisis is null) return;

        var crisis = CurrentState.CurrentCrisis;
        var choice = crisis.GetChoice(choiceId);

        GD.Print($"[GameManager] Responding to crisis: {crisis.Title} with choice: {choice.Label}");

        // Apply choice effects (simplified - full logic is in QuarterEngine.AdvanceCrisis)
        var newState = CurrentState;

        // Handle PC cost
        if (choice.HasPCCost)
        {
            if (!newState.Resources.CanAfford(choice.PCCost))
            {
                GD.PrintErr($"[GameManager] Cannot afford crisis choice (need {choice.PCCost} PC)");
                return;
            }
            newState = newState.WithResources(newState.Resources.WithSpend(choice.PCCost));
        }

        // Determine outcome and apply effects
        var outcomeTier = OutcomeTier.Expected;
        IReadOnlyList<IEffect> effectsToApply;
        var meterDeltas = new List<(Meter Meter, int Delta)>();

        if (choice.HasTieredOutcomes && choice.OutcomeProfile is not null)
        {
            outcomeTier = OutcomeRoller.RollCrisisChoice(choice.OutcomeProfile, choice, _rng);
            effectsToApply = choice.OutcomeProfile.GetEffectsForTier(outcomeTier);
        }
        else
        {
            effectsToApply = choice.Effects;
        }

        // Apply effects
        foreach (var effect in effectsToApply)
        {
            if (effect is MeterEffect meterEffect)
            {
                meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
            }

            var adapted = GameState.NewGame(newState.Seed).WithOrg(newState.Org);
            var (updatedAdapted, _) = effect.Apply(adapted, _rng);
            newState = newState.WithOrg(updatedAdapted.Org);
        }

        // Handle corporate choice
        if (choice.IsCorporateChoice)
        {
            var newCEO = newState.CEO.WithEvilScoreChange(choice.CorporateIntensityDelta);
            int favBump = 1 + (choice.CorporateIntensityDelta - 1);
            newCEO = newCEO.WithFavorabilityChange(favBump);
            newState = newState.WithCEO(newCEO);
        }

        // Generate resolution email
        var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
        var resolutionThread = emailGen.CreateCrisisResolutionThread(
            crisis.Title,
            choice.Label,
            outcomeTier,
            meterDeltas,
            CurrentState.Quarter.QuarterNumber,
            choice.IsCorporateChoice);
        newState = newState.WithInbox(newState.Inbox.WithThreadAdded(resolutionThread));

        // Clear the crisis but stay in current phase
        newState = newState.WithCurrentCrisis(null);
        CurrentState = newState;

        GD.Print($"[GameManager] Crisis resolved: {crisis.Title} - Outcome: {outcomeTier}");
        EmitSignal(SignalName.StateChanged);
    }

    private void LogPhase(QuarterLog log)
    {
        foreach (var entry in log.Entries)
        {
            GD.Print($"[Q{log.QuarterNumber} {log.Phase}] {entry.Message}");
        }
    }
}
