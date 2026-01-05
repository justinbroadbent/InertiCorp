using System.Collections.Immutable;
using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Email;
using InertiCorp.Core.Llm;
using InertiCorp.Game.Audio;
using InertiCorp.Game.Dashboard;

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
    /// Stores deferred meter deltas for queued cards - effects are applied when project completes.
    /// Key is cardId, value is the meter changes and other deltas.
    /// </summary>
    private readonly Dictionary<string, DeferredCardEffects> _deferredEffects = new();

    /// <summary>
    /// Deferred effects for a card that will be applied when project completes.
    /// </summary>
    private record DeferredCardEffects(
        int DeliveryDelta,
        int MoraleDelta,
        int GovernanceDelta,
        int AlignmentDelta,
        int RunwayDelta,
        int ProfitDelta,
        int EvilScoreDelta,
        int FavorabilityDelta,
        OutcomeTier? OutcomeTier = null,
        string? ProfitImpact = null,
        string? MeterEffects = null);

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

    private LoadingScreen? _loadingScreen;
    private bool _gameStarted;

    public override void _Ready()
    {
        InitializeMusicManager();
        InitializeBackgroundProcessor();

        // Skip loading screen for now - start game directly
        // TODO: Fix LoadingScreen CanvasLayer _Ready not being called
        _gameStarted = true;
        StartNewGame();

        // Load LLM in background (won't block game start)
        InitializeLlmService();
    }

    private void ShowLoadingScreen()
    {
        GD.Print("[GameManager] ShowLoadingScreen called");

        // Hide dashboard initially
        var dashboard = GetNode<Control>("../CEODashboard");
        if (dashboard != null)
        {
            dashboard.Visible = false;
            GD.Print("[GameManager] Dashboard hidden");
        }
        else
        {
            GD.PrintErr("[GameManager] Could not find CEODashboard!");
        }

        // Create and show loading screen
        _loadingScreen = new LoadingScreen();
        _loadingScreen.LoadingComplete += OnLoadingScreenComplete;
        GetTree().Root.AddChild(_loadingScreen);
        GD.Print("[GameManager] LoadingScreen added to tree");
    }

    private void OnLoadingScreenComplete()
    {
        GD.Print("[GameManager] OnLoadingScreenComplete called");

        // Show dashboard
        var dashboard = GetNode<Control>("../CEODashboard");
        if (dashboard != null)
        {
            dashboard.Visible = true;
            GD.Print("[GameManager] Dashboard shown");
        }
        else
        {
            GD.PrintErr("[GameManager] Could not find CEODashboard to show!");
        }

        // Start game if not already started
        if (!_gameStarted)
        {
            _gameStarted = true;
            GD.Print("[GameManager] Starting new game");
            StartNewGame();
        }
    }

    private void InitializeBackgroundProcessor()
    {
        var processor = new BackgroundEmailProcessor();
        AddChild(processor);
        processor.Initialize(this);
    }

    private void InitializeMusicManager()
    {
        // Create and add the music manager as a child so it persists
        var musicManager = new MusicManager();
        AddChild(musicManager);
    }

    private async void InitializeLlmService()
    {
        GD.Print("[GameManager] InitializeLlmService starting");

        // Initialize the LLM service manager
        LlmServiceManager.Initialize();
        LlmServiceManager.Ready += OnLlmReady;
        LlmServiceManager.LoadFailed += OnLlmLoadFailed;
        GD.Print("[GameManager] LlmServiceManager initialized");

        // Check if a model is configured
        var modelManager = LlmServiceManager.GetModelManager();
        if (modelManager?.ActiveModelId == null)
        {
            GD.Print("[GameManager] No AI model configured - using email templates");
            _loadingScreen?.OnLoadingFailed("No AI model installed");
            return;
        }

        GD.Print($"[GameManager] Loading model: {modelManager.ActiveModelId}");

        // Try to load the active model in the background
        try
        {
            await LlmServiceManager.LoadActiveModelAsync();
            GD.Print("[GameManager] LoadActiveModelAsync completed");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[GameManager] Failed to load LLM model: {ex.Message}");
            _loadingScreen?.OnLoadingFailed(ex.Message);
        }
    }

    private void OnLlmReady()
    {
        GD.Print($"[GameManager] LLM ready: {LlmServiceManager.LoadedModelName}");
        _loadingScreen?.OnLoadingComplete();
    }

    private void OnLlmLoadFailed(System.Exception ex)
    {
        GD.PrintErr($"[GameManager] LLM load failed: {ex.Message}");
        _loadingScreen?.OnLoadingFailed(ex.Message);
    }

    public override void _ExitTree()
    {
        // Clean up LLM service
        LlmServiceManager.Shutdown();
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

        // If a crisis was drawn during BoardDemand, create the inbox thread for it
        if (CurrentState.CurrentCrisis is not null)
        {
            var crisisCard = CurrentState.CurrentCrisis;
            GD.Print($"[GameManager] Crisis drawn during BoardDemand: {crisisCard.Title}");

            // Generate the crisis email
            var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
            var crisisThread = emailGen.CreateCrisisThread(
                crisisCard,
                CurrentState.Quarter.QuarterNumber,
                CurrentState.Org.Alignment,
                CurrentState.CEO.BoardPressureLevel,
                CurrentState.CEO.EvilScore);

            // Set OriginatingCardId so NavigateToUnresolvedCrisis can find this thread
            crisisThread = crisisThread with { OriginatingCardId = crisisCard.EventId };

            var newInbox = CurrentState.Inbox.WithThreadAdded(crisisThread);
            CurrentState = CurrentState.WithInbox(newInbox);

            // Trigger async AI generation to update the email content
            TriggerCrisisAiGeneration(crisisThread.ThreadId, crisisCard.Title, crisisCard.Description, isResolution: false);
        }

        LogPhase(log);
        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);
    }

    /// <summary>
    /// Makes a choice for the crisis card.
    /// Effects are deferred until the player clicks "Accept Result" on the resolution email.
    /// </summary>
    public void MakeCrisisChoice(string choiceId)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.Crisis) return;
        if (CurrentState.CurrentCrisis is null) return;

        var crisis = CurrentState.CurrentCrisis;
        var choice = crisis.GetChoice(choiceId);

        GD.Print($"[GameManager] Making crisis choice: {crisis.Title} with choice: {choice.Label}");

        var newState = CurrentState;

        // Handle PC cost immediately (not deferred)
        if (choice.HasPCCost)
        {
            if (!newState.Resources.CanAfford(choice.PCCost))
            {
                GD.PrintErr($"[GameManager] Cannot afford crisis choice (need {choice.PCCost} PC)");
                return;
            }
            newState = newState.WithResources(newState.Resources.WithSpend(choice.PCCost));
        }

        // Determine outcome and calculate effects (but don't apply yet)
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

        // Calculate effects without applying
        foreach (var effect in effectsToApply)
        {
            if (effect is MeterEffect meterEffect)
            {
                meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
            }
        }

        // Calculate corporate choice effects
        var evilScoreDelta = choice.IsCorporateChoice ? choice.CorporateIntensityDelta : 0;
        var favorabilityDelta = choice.IsCorporateChoice ? 1 + (choice.CorporateIntensityDelta - 1) : 0;

        // Store deferred effects using a crisis-specific key
        var crisisKey = $"crisis_{crisis.EventId}_{CurrentState.Quarter.QuarterNumber}";
        _deferredEffects[crisisKey] = new DeferredCardEffects(
            DeliveryDelta: meterDeltas.Where(m => m.Meter == Meter.Delivery).Sum(m => m.Delta),
            MoraleDelta: meterDeltas.Where(m => m.Meter == Meter.Morale).Sum(m => m.Delta),
            GovernanceDelta: meterDeltas.Where(m => m.Meter == Meter.Governance).Sum(m => m.Delta),
            AlignmentDelta: meterDeltas.Where(m => m.Meter == Meter.Alignment).Sum(m => m.Delta),
            RunwayDelta: meterDeltas.Where(m => m.Meter == Meter.Runway).Sum(m => m.Delta),
            ProfitDelta: 0,  // Crises don't affect profit directly
            EvilScoreDelta: evilScoreDelta,
            FavorabilityDelta: favorabilityDelta);

        // Generate a thread ID for the resolution email (created when AI completes)
        var threadId = $"crisis_res_{Guid.NewGuid():N}";

        // Store pending resolution info - email will be created when AI completes
        var pendingResolution = new PendingCrisisResolution(
            CrisisTitle: crisis.Title,
            ChoiceLabel: choice.Label,
            Outcome: outcomeTier,
            MeterDeltas: meterDeltas,
            QuarterNumber: CurrentState.Quarter.QuarterNumber,
            WasCorporateChoice: choice.IsCorporateChoice,
            EvilScoreDelta: evilScoreDelta,
            CrisisKey: crisisKey);

        // Clear the crisis and advance to Resolution phase
        // Email will be added when AI completes
        newState = newState.WithCurrentCrisis(null);
        newState = newState.WithQuarter(newState.Quarter with { Phase = GamePhase.Resolution });
        CurrentState = newState;
        Phase = UIPhase.ShowingResolution;

        GD.Print($"[GameManager] Crisis queued for AI: {crisis.Title} - Outcome: {outcomeTier} (awaiting AI)");
        EmitSignal(SignalName.StateChanged);
        EmitSignal(SignalName.PhaseChanged);

        // Trigger async AI generation - email created when AI completes
        TriggerCrisisAiGeneration(
            threadId,
            crisis.Title,
            crisis.Description,
            isResolution: true,
            choiceLabel: choice.Label,
            outcomeTier: outcomeTier.ToString(),
            pendingResolution: pendingResolution);
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
    /// Plays a card that was queued in the project queue.
    /// Effects are deferred until the project completes (after AI generation).
    /// </summary>
    /// <param name="cardId">The card ID to play</param>
    /// <param name="card">Optional card object if the card was already removed from hand</param>
    public void PlayQueuedCard(string cardId, PlayableCard? card = null)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        // If card isn't in hand but we have the card object, add it back temporarily
        if (!CurrentState.Hand.Contains(cardId))
        {
            if (card != null)
            {
                // Card was removed from hand by AddToProjectQueue, add it back temporarily
                var tempHand = CurrentState.Hand.WithCardAdded(card);
                CurrentState = CurrentState.WithHand(tempHand);
                GD.Print($"[GameManager] Temporarily added card {cardId} back to hand for playing");
            }
            else
            {
                GD.PrintErr($"[GameManager] Queued card {cardId} not found in hand");
                return;
            }
        }

        // Store the pre-play state (before effects)
        var preOrg = CurrentState.Org;
        var preCEO = CurrentState.CEO;

        // Play the card - this applies effects and creates email
        // Use ForQueuedCard to suppress fluff email generation
        var input = QuarterInput.ForQueuedCard(cardId);
        var (newState, log) = QuarterEngine.Advance(CurrentState, input, _rng);

        // Capture outcome from log immediately (before LastLog can be overwritten by phase changes)
        var outcome = log.Entries.FirstOrDefault(e => e.OutcomeTier != null)?.OutcomeTier;
        var profitEntry = log.Entries.FirstOrDefault(e => e.Message.StartsWith("Profit impact:"));
        var profitImpact = profitEntry?.Message.Replace("Profit impact: ", "");
        var meterChanges = log.Entries
            .Where(e => e.Category == LogCategory.MeterChange)
            .Select(e => $"{e.Meter}: {(e.Delta >= 0 ? "+" : "")}{e.Delta}")
            .ToList();
        var meterEffects = meterChanges.Count > 0 ? string.Join(", ", meterChanges) : null;

        GD.Print($"[GameManager] Captured outcome for {cardId}: {outcome?.ToString() ?? "null"} (stored before phase changes)");

        // Calculate the deltas (what changed)
        var deltas = new DeferredCardEffects(
            DeliveryDelta: newState.Org.Delivery - preOrg.Delivery,
            MoraleDelta: newState.Org.Morale - preOrg.Morale,
            GovernanceDelta: newState.Org.Governance - preOrg.Governance,
            AlignmentDelta: newState.Org.Alignment - preOrg.Alignment,
            RunwayDelta: newState.Org.Runway - preOrg.Runway,
            ProfitDelta: newState.CEO.CurrentQuarterProfit - preCEO.CurrentQuarterProfit,
            EvilScoreDelta: newState.CEO.EvilScore - preCEO.EvilScore,
            FavorabilityDelta: newState.CEO.BoardFavorability - preCEO.BoardFavorability,
            OutcomeTier: outcome,
            ProfitImpact: profitImpact,
            MeterEffects: meterEffects);

        _deferredEffects[cardId] = deltas;

        // Restore the pre-play org/CEO state - effects will be applied when project completes
        // Keep the rest of the state (inbox with email, hand without card, etc.)
        var deferredState = newState
            .WithOrg(preOrg)
            .WithCEO(preCEO);

        CurrentState = deferredState;
        LastLog = log;

        // Hide the email thread until AI content is ready
        var thread = CurrentState.Inbox.GetThreadForCard(cardId);
        if (thread != null)
        {
            var hiddenThread = thread with { IsVisible = false };
            var hiddenInbox = CurrentState.Inbox.WithThreadReplaced(thread.ThreadId, hiddenThread);
            CurrentState = CurrentState.WithInbox(hiddenInbox);
            GD.Print($"[GameManager] Thread {thread.ThreadId} hidden until AI content ready");
        }

        LogPhase(log);
        GD.Print($"[GameManager] Queued card played (effects deferred): {cardId}");

        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Applies the deferred effects for a completed project.
    /// Called when project finishes in the queue (after AI generation or timeout).
    /// </summary>
    public void ApplyDeferredEffects(string cardId)
    {
        if (CurrentState is null) return;

        if (!_deferredEffects.TryGetValue(cardId, out var effects))
        {
            GD.Print($"[GameManager] No deferred effects for card {cardId}");
            return;
        }

        // Apply the deferred deltas to current state
        var newOrg = CurrentState.Org
            .WithMeterChange(Meter.Delivery, effects.DeliveryDelta)
            .WithMeterChange(Meter.Morale, effects.MoraleDelta)
            .WithMeterChange(Meter.Governance, effects.GovernanceDelta)
            .WithMeterChange(Meter.Alignment, effects.AlignmentDelta)
            .WithMeterChange(Meter.Runway, effects.RunwayDelta);

        var newCEO = CurrentState.CEO with
        {
            CurrentQuarterProfit = CurrentState.CEO.CurrentQuarterProfit + effects.ProfitDelta,
            EvilScore = CurrentState.CEO.EvilScore + effects.EvilScoreDelta,
            BoardFavorability = Math.Clamp(CurrentState.CEO.BoardFavorability + effects.FavorabilityDelta, 0, 100)
        };

        CurrentState = CurrentState
            .WithOrg(newOrg)
            .WithCEO(newCEO);

        // Clean up
        _deferredEffects.Remove(cardId);

        GD.Print($"[GameManager] Applied deferred effects for card {cardId}");
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Gets the stored outcome info for a card (captured when card was played, before phase changes).
    /// This avoids race conditions where LastLog is overwritten by subsequent phase transitions.
    /// </summary>
    public (string Outcome, string? ProfitImpact, string? MeterEffects)? GetStoredOutcome(string cardId)
    {
        if (!_deferredEffects.TryGetValue(cardId, out var effects))
        {
            return null;
        }

        var outcome = effects.OutcomeTier?.ToString() ?? "Expected";
        return (outcome, effects.ProfitImpact, effects.MeterEffects);
    }

    /// <summary>
    /// Queues a project card for background processing.
    /// The card is removed from hand immediately, but effects are deferred until processing completes.
    /// </summary>
    public void QueueProjectCard(string cardId)
    {
        if (CurrentState is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        // Check that the card is actually in the hand
        if (!CurrentState.Hand.Contains(cardId))
        {
            GD.PrintErr($"[GameManager] Card {cardId} not found in hand");
            return;
        }

        // Get the card before removing from hand
        var card = CurrentState.Hand.Cards.FirstOrDefault(c => c.CardId == cardId);
        if (card == null) return;

        // Remove card from hand but don't play it yet
        var newHand = CurrentState.Hand.WithCardRemoved(cardId);
        CurrentState = CurrentState.WithHand(newHand);

        // Queue for background processing
        BackgroundEmailProcessor.Instance?.QueueProject(card);

        GD.Print($"[GameManager] Queued project for background processing: {card.Title}");
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Finalizes a project that was processed in the background.
    /// Called by BackgroundEmailProcessor when AI content is ready.
    /// The card was already played in QueueProject - this just updates the email and applies effects.
    /// </summary>
    public void FinalizeQueuedProject(string cardId, string? aiContent)
    {
        if (CurrentState is null || _rng is null) return;

        GD.Print($"[GameManager] Finalizing queued project: {cardId}");

        // The card was already played in BackgroundEmailProcessor.QueueProject via PlayQueuedCard.
        // Effects were calculated and stored in _deferredEffects.
        // The email thread exists but is hidden. We need to:
        // 1. Update the email body with AI content (or keep template)
        // 2. Make the thread visible
        // 3. Apply the deferred effects

        var thread = CurrentState.Inbox.GetThreadForCard(cardId);
        if (thread == null)
        {
            GD.PrintErr($"[GameManager] No thread found for card {cardId}");
            ApplyDeferredEffects(cardId);
            return;
        }

        // Update body with AI content if available and substantial (min 50 chars)
        const int MinContentLength = 50;
        var updatedThread = thread;

        if (!string.IsNullOrEmpty(aiContent) && aiContent.Length >= MinContentLength)
        {
            // Update the last message body with AI content
            var updatedMessages = thread.Messages.Select((m, i) =>
                i == thread.Messages.Count - 1
                    ? m with { Body = aiContent }
                    : m).ToList();
            updatedThread = thread with { Messages = updatedMessages };
            GD.Print($"[GameManager] Updated thread with AI content ({aiContent.Length} chars)");
        }
        else if (!string.IsNullOrEmpty(aiContent))
        {
            GD.Print($"[GameManager] AI content too short ({aiContent.Length} chars), keeping template");
        }
        else
        {
            GD.Print($"[GameManager] No AI content, keeping template");
        }

        // Make the thread visible now that content is ready
        updatedThread = updatedThread with { IsVisible = true };
        var updatedInbox = CurrentState.Inbox.WithThreadReplaced(thread.ThreadId, updatedThread);
        CurrentState = CurrentState.WithInbox(updatedInbox);
        GD.Print($"[GameManager] Thread {thread.ThreadId} now visible");

        // Apply deferred effects immediately (since we're in background completion)
        ApplyDeferredEffects(cardId);

        EmitSignal(SignalName.StateChanged);
        GD.Print($"[GameManager] Project finalized with effects: {cardId}");
    }

    /// <summary>
    /// Activates a crisis that was processed in the background.
    /// Called by BackgroundEmailProcessor when AI content is ready.
    /// </summary>
    public void ActivateCrisis(string eventId, string title, string description,
        int quarterNumber, string? originatingThreadId, string? aiContent)
    {
        if (CurrentState is null || _rng is null) return;

        GD.Print($"[GameManager] Activating crisis: {title}");

        // Get crisis cards from the event deck to find a matching one or create one
        var (newDecks, crisisCard) = CurrentState.EventDecks.DrawCrisis(_rng);

        // If the drawn card doesn't match, create one from the provided data
        // This handles cases where we pre-generated content for a specific crisis
        if (crisisCard.EventId != eventId)
        {
            // Use the drawn card but it won't match the pre-generated content
            // In practice, this shouldn't happen often
            GD.Print($"[GameManager] Crisis mismatch: expected {eventId}, got {crisisCard.EventId}");
        }

        CurrentState = CurrentState.WithEventDecks(newDecks);

        // Create the crisis email with AI content or fallback
        var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
        var inbox = CurrentState.Inbox;

        if (!string.IsNullOrEmpty(originatingThreadId) && inbox.GetThread(originatingThreadId) is not null)
        {
            // Add as follow-up to originating thread
            var originThread = inbox.GetThread(originatingThreadId)!;
            var followUp = emailGen.CreateFollowUpReply(
                originatingThreadId,
                originThread.Subject,
                !string.IsNullOrEmpty(aiContent) ? aiContent : $"URGENT: {crisisCard.Title}\n\n{crisisCard.Description}",
                Array.Empty<(Meter, int)>(),
                quarterNumber,
                CurrentState.Org.Alignment,
                Core.Email.SenderArchetype.HR);
            inbox = inbox.WithFollowUpAdded(originatingThreadId, followUp);
            inbox = inbox.WithThreadUpgradedToCrisis(originatingThreadId, crisisCard.EventId);
        }
        else
        {
            // Create standalone crisis thread
            var crisisThread = emailGen.CreateCrisisThread(
                crisisCard,
                quarterNumber,
                CurrentState.Org.Alignment,
                CurrentState.CEO.BoardPressureLevel,
                CurrentState.CEO.EvilScore);

            // Set OriginatingCardId so NavigateToUnresolvedCrisis can find this thread
            crisisThread = crisisThread with { OriginatingCardId = crisisCard.EventId };

            // If we have AI content, update the thread body
            if (!string.IsNullOrEmpty(aiContent))
            {
                var updatedThread = crisisThread with
                {
                    Messages = crisisThread.Messages.Select((m, i) => i == 0
                        ? m with { Body = aiContent }
                        : m).ToImmutableArray()
                };
                inbox = inbox.WithThreadAdded(updatedThread);
            }
            else
            {
                inbox = inbox.WithThreadAdded(crisisThread);
            }
        }

        // Set the crisis as active
        CurrentState = CurrentState
            .WithInbox(inbox)
            .WithCurrentCrisis(crisisCard);

        GD.Print($"[GameManager] Crisis activated: {crisisCard.Title}");
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Ends the play cards phase without playing more cards.
    /// </summary>
    public void EndPlayCardsPhase()
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.Quarter.Phase != GamePhase.PlayCards) return;

        // Block if there are still projects being processed
        var processor = BackgroundEmailProcessor.Instance;
        var hasActive = processor?.HasActiveProjects ?? false;
        var activeCount = processor?.ActiveProjectCount ?? 0;
        GD.Print($"[EndPlayCardsPhase] Checking: processor={processor != null}, hasActive={hasActive}, count={activeCount}");

        if (hasActive)
        {
            GD.Print("[GameManager] Cannot end PlayCards phase - projects still processing");
            EmitSignal(SignalName.StateChanged); // Refresh UI to show blocked state
            return;
        }

        // Block if there's an unresolved crisis
        if (HasPendingCrisis)
        {
            GD.Print("[GameManager] Cannot end PlayCards phase - pending crisis must be resolved");
            EmitSignal(SignalName.StateChanged); // Refresh UI
            return;
        }

        var (newState, log) = QuarterEngine.Advance(CurrentState, QuarterInput.EndCardPlay, _rng);
        CurrentState = newState;
        LastLog = log;

        LogPhase(log);

        // Process crisis phase (follow-ups, draw crisis) but don't stop for separate UI
        if (CurrentState.Quarter.Phase == GamePhase.Crisis)
        {
            var (crisisState, crisisLog) = QuarterEngine.Advance(CurrentState, QuarterInput.Empty, _rng);
            CurrentState = crisisState;
            LogPhase(crisisLog);

            // If a crisis was triggered (from follow-up or random draw), create the inbox thread
            if (CurrentState.CurrentCrisis is not null)
            {
                var crisisCard = CurrentState.CurrentCrisis;
                GD.Print($"[GameManager] Crisis triggered during Crisis phase: {crisisCard.Title}");

                // Generate the crisis email
                var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
                var crisisThread = emailGen.CreateCrisisThread(
                    crisisCard,
                    CurrentState.Quarter.QuarterNumber,
                    CurrentState.Org.Alignment,
                    CurrentState.CEO.BoardPressureLevel,
                    CurrentState.CEO.EvilScore);

                // Set OriginatingCardId so NavigateToUnresolvedCrisis can find this thread
                crisisThread = crisisThread with { OriginatingCardId = crisisCard.EventId };

                var newInbox = CurrentState.Inbox.WithThreadAdded(crisisThread);
                CurrentState = CurrentState.WithInbox(newInbox);

                // Trigger async AI generation to update the email content
                TriggerCrisisAiGeneration(crisisThread.ThreadId, crisisCard.Title, crisisCard.Description, isResolution: false);
            }
        }

        // Always go directly to Resolution - crises handled as inbox items there
        Phase = UIPhase.ShowingResolution;

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
    /// Moves an email thread to the trash.
    /// </summary>
    public void TrashThread(string threadId)
    {
        if (CurrentState is null) return;

        var newInbox = CurrentState.Inbox.WithThreadTrashed(threadId);
        CurrentState = CurrentState.WithInbox(newInbox);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Restores an email thread from trash.
    /// </summary>
    public void RestoreThread(string threadId)
    {
        if (CurrentState is null) return;

        var newInbox = CurrentState.Inbox.WithThreadRestored(threadId);
        CurrentState = CurrentState.WithInbox(newInbox);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Empties the trash bin.
    /// </summary>
    public void EmptyTrash()
    {
        if (CurrentState is null) return;

        var newInbox = CurrentState.Inbox.WithTrashEmptied();
        CurrentState = CurrentState.WithInbox(newInbox);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Accepts the pending effects for a project or crisis email thread.
    /// For queued cards, effects were already applied when card was played.
    /// For crisis resolutions, effects are deferred and applied here.
    /// </summary>
    public void AcceptProjectEffects(string threadId)
    {
        if (CurrentState is null) return;

        var thread = CurrentState.Inbox.Threads.FirstOrDefault(t => t.ThreadId == threadId);
        if (thread is null || !thread.HasPendingEffects) return;

        // Check if this thread has deferred effects to apply (crisis resolutions)
        var effectKey = thread.OriginatingCardId;
        if (effectKey is not null && _deferredEffects.TryGetValue(effectKey, out var effects))
        {
            // Apply the deferred effects
            var newOrg = CurrentState.Org
                .WithMeterChange(Meter.Delivery, effects.DeliveryDelta)
                .WithMeterChange(Meter.Morale, effects.MoraleDelta)
                .WithMeterChange(Meter.Governance, effects.GovernanceDelta)
                .WithMeterChange(Meter.Alignment, effects.AlignmentDelta)
                .WithMeterChange(Meter.Runway, effects.RunwayDelta);

            var newCEO = CurrentState.CEO with
            {
                CurrentQuarterProfit = CurrentState.CEO.CurrentQuarterProfit + effects.ProfitDelta,
                EvilScore = CurrentState.CEO.EvilScore + effects.EvilScoreDelta,
                BoardFavorability = Math.Clamp(CurrentState.CEO.BoardFavorability + effects.FavorabilityDelta, 0, 100)
            };

            CurrentState = CurrentState
                .WithOrg(newOrg)
                .WithCEO(newCEO);

            // Clean up deferred effects
            _deferredEffects.Remove(effectKey);
            GD.Print($"[GameManager] Applied deferred effects for: {effectKey}");
        }

        // Mark effects as accepted
        var updatedThread = thread.WithEffectsAccepted();
        var newInbox = CurrentState.Inbox.WithThreadReplaced(threadId, updatedThread);
        CurrentState = CurrentState.WithInbox(newInbox);
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Updates an email thread with a replacement (e.g., with AI-generated content).
    /// Does not emit StateChanged - caller should emit if UI update is needed.
    /// </summary>
    public void UpdateThread(string threadId, EmailThread updatedThread)
    {
        if (CurrentState is null) return;

        var newInbox = CurrentState.Inbox.WithThreadReplaced(threadId, updatedThread);
        CurrentState = CurrentState.WithInbox(newInbox);
    }

    /// <summary>
    /// Updates the body of the latest message in a thread with AI-generated content.
    /// </summary>
    public void UpdateThreadWithAiContent(string threadId, string aiContent)
    {
        if (CurrentState is null) return;

        var thread = CurrentState.Inbox.GetThread(threadId);
        if (thread == null) return;

        // Update the last message body with AI content
        var updatedMessages = thread.Messages.Select((m, i) =>
            i == thread.Messages.Count - 1
                ? m with { Body = aiContent }
                : m).ToList();

        var updatedThread = thread with { Messages = updatedMessages };
        UpdateThread(threadId, updatedThread);
    }

    /// <summary>
    /// Replaces the entire inbox (for adding new threads, etc.).
    /// </summary>
    public void UpdateInbox(Inbox newInbox)
    {
        if (CurrentState is null) return;

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

        // Set OriginatingCardId so NavigateToUnresolvedCrisis can find this thread
        crisisThread = crisisThread with { OriginatingCardId = crisisCard.EventId };

        var newInbox = CurrentState.Inbox.WithThreadAdded(crisisThread);
        CurrentState = CurrentState.WithInbox(newInbox);

        // Don't change UI phase - just signal state changed so inbox updates
        EmitSignal(SignalName.StateChanged);

        // Trigger async AI generation to update the email content
        TriggerCrisisAiGeneration(crisisThread.ThreadId, crisisCard.Title, crisisCard.Description, isResolution: false);
    }

    // Stores pending crisis resolution info until AI completes
    private readonly Dictionary<string, PendingCrisisResolution> _pendingCrisisResolutions = new();

    private record PendingCrisisResolution(
        string CrisisTitle,
        string ChoiceLabel,
        OutcomeTier Outcome,
        IReadOnlyList<(Meter Meter, int Delta)> MeterDeltas,
        int QuarterNumber,
        bool WasCorporateChoice,
        int EvilScoreDelta,
        string CrisisKey);

    /// <summary>
    /// Triggers async AI content generation for a crisis email via the shared queue.
    /// The email is NOT added until AI completes - no instant canned fallback.
    /// </summary>
    private void TriggerCrisisAiGeneration(
        string threadId,
        string crisisTitle,
        string crisisDescription,
        bool isResolution,
        string? choiceLabel = null,
        string? outcomeTier = null,
        PendingCrisisResolution? pendingResolution = null)
    {
        var promptType = isResolution ? AiPromptType.CrisisResponse : AiPromptType.CrisisInitial;
        var priority = isResolution ? AiPriority.Low : AiPriority.Normal;

        Dictionary<string, string>? context = null;
        if (isResolution)
        {
            context = new Dictionary<string, string>();
            if (choiceLabel is not null) context["choiceLabel"] = choiceLabel;
            if (outcomeTier is not null) context["outcomeTier"] = outcomeTier;

            // Add effects description for better AI context
            if (pendingResolution is not null)
            {
                var effectsList = new List<string>();
                foreach (var (meter, delta) in pendingResolution.MeterDeltas)
                {
                    if (delta != 0)
                    {
                        var sign = delta > 0 ? "+" : "";
                        effectsList.Add($"{meter}: {sign}{delta}");
                    }
                }
                if (pendingResolution.EvilScoreDelta > 0)
                {
                    effectsList.Add($"Evil Score: +{pendingResolution.EvilScoreDelta}");
                }
                if (effectsList.Count > 0)
                {
                    context["effects"] = string.Join(", ", effectsList);
                }
            }
        }

        // Store pending resolution info so we can create the email when AI completes
        if (pendingResolution is not null)
        {
            _pendingCrisisResolutions[threadId] = pendingResolution;
        }

        GD.Print($"[GameManager] Queueing crisis AI generation: {crisisTitle} (priority: {priority})");

        // Use shared AI queue - email created only when AI completes
        BackgroundEmailProcessor.QueueExternalAiRequest(
            threadId,
            crisisTitle,
            crisisDescription,
            promptType,
            priority,
            aiBody => CreateCrisisEmailWithAiContent(threadId, aiBody),
            context);
    }

    private void CreateCrisisEmailWithAiContent(string threadId, string aiBody)
    {
        if (CurrentState is null || _rng is null) return;

        // Check if we have pending resolution info for this thread
        if (!_pendingCrisisResolutions.TryGetValue(threadId, out var pending))
        {
            GD.PrintErr($"[GameManager] No pending crisis resolution for {threadId}");
            return;
        }

        _pendingCrisisResolutions.Remove(threadId);

        // Create the email with AI content (or fallback if AI genuinely failed after retries)
        var emailGen = new Core.Email.EmailGenerator(_rng.NextInt(0, int.MaxValue));
        var resolutionThread = emailGen.CreateCrisisResolutionThread(
            pending.CrisisTitle,
            pending.ChoiceLabel,
            pending.Outcome,
            pending.MeterDeltas,
            pending.QuarterNumber,
            pending.WasCorporateChoice,
            pending.EvilScoreDelta);

        // Override body with AI content if available
        if (!string.IsNullOrWhiteSpace(aiBody))
        {
            var updatedMessages = resolutionThread.Messages.Select(m =>
                m.IsFromPlayer ? m : m with { Body = aiBody }).ToList();
            resolutionThread = resolutionThread with { Messages = updatedMessages };
            GD.Print($"[GameManager] Crisis email created with AI content: {aiBody[..Math.Min(50, aiBody.Length)]}...");
        }
        else
        {
            GD.Print($"[GameManager] Crisis email created with fallback content (AI unavailable)");
        }

        // Set the crisis key for deferred effects
        resolutionThread = resolutionThread with { OriginatingCardId = pending.CrisisKey };

        // NOW add the thread to inbox
        var newInbox = CurrentState.Inbox.WithThreadAdded(resolutionThread);
        CurrentState = CurrentState.WithInbox(newInbox);

        GD.Print($"[GameManager] Crisis resolution email added to inbox: {threadId}");
        EmitSignal(SignalName.StateChanged);
    }

    /// <summary>
    /// Whether there's an unresolved crisis that must be handled before ending the phase.
    /// Returns true if there's an active crisis that needs a response AND a crisis thread exists.
    /// The thread may not exist yet if we're in PlayCards and haven't entered Crisis phase.
    /// </summary>
    public bool HasPendingCrisis
    {
        get
        {
            if (CurrentState?.CurrentCrisis is null)
            {
                return false;
            }

            var crisisEventId = CurrentState.CurrentCrisis.EventId;

            // Check if the crisis thread actually exists in the inbox AND is visible
            // Hidden threads (waiting for AI content) shouldn't block the UI
            var crisisThread = CurrentState.Inbox.Threads.FirstOrDefault(t =>
                t.ThreadType == EmailThreadType.Crisis &&
                t.OriginatingCardId == crisisEventId);

            if (crisisThread != null && crisisThread.IsVisible)
            {
                GD.Print($"[HasPendingCrisis] Found visible crisis thread: {crisisThread.ThreadId}, EventId={crisisEventId}");
                return true;
            }

            if (crisisThread != null && !crisisThread.IsVisible)
            {
                GD.Print($"[HasPendingCrisis] Crisis thread exists but hidden (AI pending): {crisisThread.ThreadId}");
                return false;
            }

            // Log why we're returning false
            var allCrisisThreads = CurrentState.Inbox.Threads.Where(t => t.ThreadType == EmailThreadType.Crisis).ToList();
            if (allCrisisThreads.Count > 0)
            {
                GD.Print($"[HasPendingCrisis] CurrentCrisis.EventId={crisisEventId}, but crisis threads have IDs: {string.Join(", ", allCrisisThreads.Select(t => t.OriginatingCardId ?? "null"))}");
            }

            return false;
        }
    }

    /// <summary>
    /// Responds to a pending crisis (from any phase).
    /// Effects are deferred until the player clicks "Accept Result" on the resolution email.
    /// This allows handling interrupt crises without changing the game phase.
    /// </summary>
    public void RespondToPendingCrisis(string choiceId)
    {
        if (CurrentState is null || _rng is null) return;
        if (CurrentState.CurrentCrisis is null) return;

        var crisis = CurrentState.CurrentCrisis;
        var choice = crisis.GetChoice(choiceId);

        GD.Print($"[GameManager] Responding to crisis: {crisis.Title} with choice: {choice.Label}");

        var newState = CurrentState;

        // Handle PC cost immediately (not deferred)
        if (choice.HasPCCost)
        {
            if (!newState.Resources.CanAfford(choice.PCCost))
            {
                GD.PrintErr($"[GameManager] Cannot afford crisis choice (need {choice.PCCost} PC)");
                return;
            }
            newState = newState.WithResources(newState.Resources.WithSpend(choice.PCCost));
        }

        // Determine outcome and calculate effects (but don't apply yet)
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

        // Calculate effects without applying
        foreach (var effect in effectsToApply)
        {
            if (effect is MeterEffect meterEffect)
            {
                meterDeltas.Add((meterEffect.Meter, meterEffect.Delta));
            }
        }

        // Calculate corporate choice effects
        var evilScoreDelta = choice.IsCorporateChoice ? choice.CorporateIntensityDelta : 0;
        var favorabilityDelta = choice.IsCorporateChoice ? 1 + (choice.CorporateIntensityDelta - 1) : 0;

        // Store deferred effects using a crisis-specific key
        var crisisKey = $"crisis_{crisis.EventId}_{CurrentState.Quarter.QuarterNumber}";
        _deferredEffects[crisisKey] = new DeferredCardEffects(
            DeliveryDelta: meterDeltas.Where(m => m.Meter == Meter.Delivery).Sum(m => m.Delta),
            MoraleDelta: meterDeltas.Where(m => m.Meter == Meter.Morale).Sum(m => m.Delta),
            GovernanceDelta: meterDeltas.Where(m => m.Meter == Meter.Governance).Sum(m => m.Delta),
            AlignmentDelta: meterDeltas.Where(m => m.Meter == Meter.Alignment).Sum(m => m.Delta),
            RunwayDelta: meterDeltas.Where(m => m.Meter == Meter.Runway).Sum(m => m.Delta),
            ProfitDelta: 0,  // Crises don't affect profit directly
            EvilScoreDelta: evilScoreDelta,
            FavorabilityDelta: favorabilityDelta);

        // Generate a thread ID for the resolution email (created when AI completes)
        var threadId = $"crisis_res_{Guid.NewGuid():N}";

        // Store pending resolution info - email will be created when AI completes
        var pendingResolution = new PendingCrisisResolution(
            CrisisTitle: crisis.Title,
            ChoiceLabel: choice.Label,
            Outcome: outcomeTier,
            MeterDeltas: meterDeltas,
            QuarterNumber: CurrentState.Quarter.QuarterNumber,
            WasCorporateChoice: choice.IsCorporateChoice,
            EvilScoreDelta: evilScoreDelta,
            CrisisKey: crisisKey);

        // Clear the crisis but stay in current phase
        // Email will be added when AI completes
        newState = newState.WithCurrentCrisis(null);
        CurrentState = newState;

        GD.Print($"[GameManager] Crisis queued for AI: {crisis.Title} - Outcome: {outcomeTier} (awaiting AI)");
        EmitSignal(SignalName.StateChanged);

        // Trigger async AI generation - email created when AI completes
        TriggerCrisisAiGeneration(
            threadId,
            crisis.Title,
            crisis.Description,
            isResolution: true,
            choiceLabel: choice.Label,
            outcomeTier: outcomeTier.ToString(),
            pendingResolution: pendingResolution);
    }

    private void LogPhase(QuarterLog log)
    {
        foreach (var entry in log.Entries)
        {
            GD.Print($"[Q{log.QuarterNumber} {log.Phase}] {entry.Message}");
        }
    }
}
