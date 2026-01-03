using System.Collections.Concurrent;
using System.Threading;
using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Email;
using InertiCorp.Core.Llm;
using InertiCorp.Core.Situation;
using Godot;

namespace InertiCorp.Game;

/// <summary>
/// Priority levels for AI generation requests.
/// </summary>
public enum AiPriority
{
    High = 0,   // Project cards - player is actively waiting
    Normal = 1, // Crisis initial emails
    Low = 2     // Crisis resolution responses - can arrive later
}

/// <summary>
/// Type of AI content to generate.
/// </summary>
public enum AiPromptType
{
    ProjectCard,      // Project completion email
    CrisisInitial,    // Initial crisis/event notification
    CrisisResponse,   // Response to player's crisis choice
    FreeformEmail     // CEO-initiated freeform email
}

/// <summary>
/// Handles background processing of email content generation.
/// Both project cards and crises are processed here - emails only appear
/// in the inbox once content is ready (or timeout occurs).
/// </summary>
public partial class BackgroundEmailProcessor : Node
{
    private static BackgroundEmailProcessor? _instance;
    private GameManager? _gameManager;

    private readonly ConcurrentQueue<PendingProjectTask> _projectQueue = new();
    private readonly ConcurrentQueue<PendingCrisisTask> _crisisQueue = new();
    private readonly ConcurrentDictionary<string, ActiveProject> _activeProjects = new();
    private readonly ConcurrentDictionary<string, PlayableCard> _queuedCards = new();

    private bool _isProcessing;
    private readonly object _processingLock = new();

    /// <summary>
    /// Default timeout for AI generation before using fallback content.
    /// </summary>
    public const float DefaultGenerationTimeoutSeconds = 70.0f;

    /// <summary>
    /// Gets the adaptive timeout based on current model tier and hardware.
    /// </summary>
    public static float GetAdaptiveTimeout()
    {
        var modelManager = LlmServiceManager.GetModelManager();
        var modelId = modelManager?.ActiveModelId;
        var modelInfo = modelId != null ? ModelCatalog.GetById(modelId) : null;
        var gpuAvailable = LlmDiagnostics.GpuDetected;

        if (modelInfo == null)
            return DefaultGenerationTimeoutSeconds;

        return (modelInfo.Tier, gpuAvailable) switch
        {
            (ModelTier.Fast, _) => 45.0f,         // TinyLlama - always fast even on CPU
            (ModelTier.Balanced, true) => 60.0f,  // Phi-3 on GPU
            (ModelTier.Balanced, false) => 120.0f,// Phi-3 on CPU (slow but allow it)
            (ModelTier.Quality, true) => 90.0f,   // Mistral 7B on GPU
            (ModelTier.Quality, false) => 180.0f, // Mistral 7B on CPU (not recommended)
            _ => DefaultGenerationTimeoutSeconds
        };
    }

    /// <summary>
    /// Minimum display time for projects in the queue UI.
    /// </summary>
    public const float MinProjectDisplayTime = 3.0f;

    /// <summary>
    /// Event raised when a project email is ready and added to inbox.
    /// </summary>
    [Signal]
    public delegate void ProjectReadyEventHandler(string cardId);

    /// <summary>
    /// Event raised when a crisis email is ready and added to inbox.
    /// </summary>
    [Signal]
    public delegate void CrisisReadyEventHandler(string eventId);

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static BackgroundEmailProcessor? Instance => _instance;

    /// <summary>
    /// Active projects being processed (for UI display).
    /// </summary>
    public IReadOnlyDictionary<string, ActiveProject> ActiveProjects => _activeProjects;

    /// <summary>
    /// Whether there are active projects being processed.
    /// </summary>
    public bool HasActiveProjects => !_activeProjects.IsEmpty;

    /// <summary>
    /// Number of active projects.
    /// </summary>
    public int ActiveProjectCount => _activeProjects.Count;

    /// <summary>
    /// Maximum concurrent projects.
    /// </summary>
    public const int MaxProjects = 3;

    /// <summary>
    /// Whether the queue is full.
    /// </summary>
    public bool IsFull => _activeProjects.Count >= MaxProjects;

    // External AI queue for crisis emails, freeform responses, etc.
    private readonly List<AiGenerationRequest> _aiQueue = new();
    private readonly Dictionary<string, Action<string>> _pendingCallbacks = new();
    private readonly object _aiQueueLock = new();

    /// <summary>
    /// Queues an external AI generation request (for crisis emails, freeform responses, etc.).
    /// </summary>
    public static void QueueExternalAiRequest(
        string requestId,
        string title,
        string description,
        AiPromptType promptType,
        AiPriority priority,
        Action<string> onComplete,
        Dictionary<string, string>? additionalContext = null)
    {
        if (_instance is null)
        {
            GD.PrintErr("[BackgroundProcessor] No instance available for external AI request");
            onComplete?.Invoke("");
            return;
        }

        _instance.QueueAiRequestInternal(requestId, title, description, promptType, priority, onComplete, additionalContext);
    }

    private void QueueAiRequestInternal(
        string requestId,
        string title,
        string description,
        AiPromptType promptType,
        AiPriority priority,
        Action<string> onComplete,
        Dictionary<string, string>? additionalContext)
    {
        lock (_aiQueueLock)
        {
            _pendingCallbacks[requestId] = onComplete;

            var request = new AiGenerationRequest(
                requestId, title, description, "",
                priority, promptType, onComplete, additionalContext);

            _aiQueue.Add(request);
            _aiQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            GD.Print($"[BackgroundProcessor] Queued external AI request: {title} (priority: {priority}, queue size: {_aiQueue.Count})");

            if (!_isProcessing)
            {
                ProcessNextInQueue();
            }
        }
    }

    public override void _Ready()
    {
        _instance = this;
    }

    public override void _ExitTree()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Initialize with a game manager reference.
    /// </summary>
    public void Initialize(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public override void _Process(double delta)
    {
        // Update active project progress animations
        foreach (var kvp in _activeProjects)
        {
            kvp.Value.UpdateProgress((float)delta);
        }

        // Check for completed projects
        var completedProjects = _activeProjects
            .Where(kvp => kvp.Value.IsComplete)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var cardId in completedProjects)
        {
            if (_activeProjects.TryRemove(cardId, out var project))
            {
                FinalizeProject(project);
            }
        }

        // Process queues if not already processing
        if (!_isProcessing)
        {
            ProcessNextInQueue();
        }
    }

    /// <summary>
    /// Queue a project card for background processing.
    /// The card IS played immediately to determine outcome, but effects are deferred
    /// and AI content is generated in the background.
    /// </summary>
    public void QueueProject(PlayableCard card)
    {
        if (IsFull)
        {
            GD.PrintErr($"[BackgroundProcessor] Project queue full, cannot add {card.Title}");
            return;
        }

        if (_gameManager is null)
        {
            GD.PrintErr($"[BackgroundProcessor] No game manager, cannot process {card.Title}");
            return;
        }

        // Store the full card for later reference
        _queuedCards.TryAdd(card.CardId, card);

        // Create active project for UI display
        var activeProject = new ActiveProject(card.CardId, card.Title, card.Description);
        _activeProjects.TryAdd(card.CardId, activeProject);

        // Advance to "Playing" phase
        activeProject.AdvancePhase(); // Queued -> Playing

        GD.Print($"[BackgroundProcessor] Queued project: {card.Title}");

        // Play the card IMMEDIATELY to determine outcome (effects are deferred)
        // Pass the card object since it was already removed from hand by AddToProjectQueue
        _gameManager.PlayQueuedCard(card.CardId, card);

        // Get the thread ID for this card (just created)
        var thread = _gameManager.CurrentState?.Inbox.GetThreadForCard(card.CardId);
        if (thread != null)
        {
            activeProject.PendingThreadId = thread.ThreadId;
            GD.Print($"[BackgroundProcessor] Card {card.CardId} played, thread {thread.ThreadId} created");
        }

        // Capture outcome from the log
        var outcome = _gameManager.LastLog?.Entries
            .FirstOrDefault(e => e.OutcomeTier != null)?.OutcomeTier;
        activeProject.OutcomeText = outcome?.ToString() ?? "Expected";

        // Extract profit impact from log entries
        var profitEntry = _gameManager.LastLog?.Entries
            .FirstOrDefault(e => e.Message.StartsWith("Profit impact:"));
        if (profitEntry != null)
        {
            activeProject.ProfitImpact = profitEntry.Message.Replace("Profit impact: ", "");
        }

        // Extract meter changes from log entries
        var meterChanges = _gameManager.LastLog?.Entries
            .Where(e => e.Category == Core.LogCategory.MeterChange)
            .Select(e => $"{e.Meter}: {(e.Delta >= 0 ? "+" : "")}{e.Delta}")
            .ToList();
        if (meterChanges?.Count > 0)
        {
            activeProject.MeterEffects = string.Join(", ", meterChanges);
        }

        GD.Print($"[BackgroundProcessor] Captured outcome: {activeProject.OutcomeText}, profit: {activeProject.ProfitImpact ?? "n/a"}");

        // Stay in "Playing" phase (shows as "Queued") until LLM actually starts processing
        // Advance to Generating happens in ProcessProjectAsync when this project is dequeued

        var task = new PendingProjectTask(card);
        _projectQueue.Enqueue(task);

        // Start processing if not already
        if (!_isProcessing)
        {
            ProcessNextInQueue();
        }
    }

    /// <summary>
    /// Gets the queued card by ID (for GameManager to use when finalizing).
    /// </summary>
    public PlayableCard? GetQueuedCard(string cardId)
    {
        _queuedCards.TryGetValue(cardId, out var card);
        return card;
    }

    /// <summary>
    /// Queue a crisis for background processing.
    /// Crisis email is NOT created until content is ready.
    /// </summary>
    public void QueueCrisis(EventCard crisisCard, int quarterNumber, string? originatingThreadId = null)
    {
        var task = new PendingCrisisTask(crisisCard, quarterNumber, originatingThreadId);
        _crisisQueue.Enqueue(task);

        GD.Print($"[BackgroundProcessor] Queued crisis: {crisisCard.Title}");

        // Start processing if not already
        if (!_isProcessing)
        {
            ProcessNextInQueue();
        }
    }

    private void ProcessNextInQueue()
    {
        lock (_processingLock)
        {
            if (_isProcessing) return;

            // Process crises first (they block game progression)
            if (_crisisQueue.TryDequeue(out var crisisTask))
            {
                _isProcessing = true;
                ProcessCrisisAsync(crisisTask);
                return;
            }

            // Then process projects
            if (_projectQueue.TryDequeue(out var projectTask))
            {
                _isProcessing = true;
                ProcessProjectAsync(projectTask);
                return;
            }

            // Then process external AI requests (freeform emails, crisis responses, etc.)
            lock (_aiQueueLock)
            {
                if (_aiQueue.Count > 0)
                {
                    var request = _aiQueue[0];
                    _aiQueue.RemoveAt(0);
                    _isProcessing = true;
                    ProcessExternalAiRequestAsync(request);
                    return;
                }
            }
        }
    }

    private async void ProcessProjectAsync(PendingProjectTask task)
    {
        try
        {
            var card = task.Card;

            // Get the active project to access captured outcome data
            if (!_activeProjects.TryGetValue(card.CardId, out var activeProject))
            {
                GD.PrintErr($"[BackgroundProcessor] No active project found for {card.CardId}");
                return;
            }

            GD.Print($"[BackgroundProcessor] Processing project: {card.Title} (outcome: {activeProject.OutcomeText})");

            // NOW advance to Generating phase - LLM is actually starting
            activeProject.AdvancePhase(); // Playing -> Generating

            // Generate AI content using the REAL outcome data captured when card was played
            string? aiBody = null;
            var emailService = LlmServiceManager.GetEmailService();

            if (emailService != null && LlmServiceManager.IsReady)
            {
                try
                {
                    var timeout = GetAdaptiveTimeout();
                    GD.Print($"[BackgroundProcessor] Using timeout: {timeout}s (GPU: {LlmDiagnostics.GpuDetected})");
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

                    // Run LLM on background thread to avoid blocking main game thread (audio, etc.)
                    var title = card.Title;
                    var desc = card.Description;
                    var outcome = activeProject.OutcomeText ?? "Expected";
                    var profit = activeProject.ProfitImpact;
                    var effects = activeProject.MeterEffects;

                    // Determine sender based on the thread that was created
                    string? senderName = null;
                    if (!string.IsNullOrEmpty(activeProject.PendingThreadId))
                    {
                        var thread = _gameManager?.CurrentState?.Inbox.GetThread(activeProject.PendingThreadId);
                        if (thread?.LatestMessage != null)
                        {
                            senderName = thread.LatestMessage.FromDisplay;
                        }
                    }

                    aiBody = await Task.Run(async () =>
                        await emailService.GenerateCardEmailAsync(title, desc, outcome, profit, effects, senderName, cts.Token).ConfigureAwait(false),
                        cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    GD.Print($"[BackgroundProcessor] AI generation timed out for {card.Title}");
                }
                catch (Exception ex)
                {
                    GD.Print($"[BackgroundProcessor] AI generation failed for {card.Title}: {ex.Message}");
                }
            }

            // Validate AI content has enough substance (at least 50 chars for a 2-3 sentence email)
            // If too short, reject it and use the template instead
            const int MinContentLength = 50;
            if (aiBody != null && aiBody.Length < MinContentLength)
            {
                GD.Print($"[BackgroundProcessor] AI content too short for {card.Title}: {aiBody.Length} chars (need {MinContentLength}+), using template");
                aiBody = null;
            }

            // Update active project with AI content and advance to Finalizing phase
            activeProject.AiContent = aiBody;
            activeProject.ContentReady = true;
            activeProject.AdvancePhase(); // Generating -> Finalizing

            if (aiBody != null)
            {
                GD.Print($"[BackgroundProcessor] AI content ready for {card.Title}: {aiBody.Length} chars");
            }
            else
            {
                GD.Print($"[BackgroundProcessor] No AI content for {card.Title}, will use template");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BackgroundProcessor] Error processing project: {ex.Message}");
            MarkProjectFailed(task.Card.CardId, ex.Message);
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
            }

            // Continue processing queue
            CallDeferred(nameof(ProcessNextInQueue));
        }
    }

    private async void ProcessCrisisAsync(PendingCrisisTask task)
    {
        try
        {
            var card = task.CrisisCard;
            GD.Print($"[BackgroundProcessor] Processing crisis: {card.Title}");

            // Generate AI content
            string? aiBody = null;
            var emailService = LlmServiceManager.GetEmailService();

            if (emailService != null && LlmServiceManager.IsReady)
            {
                try
                {
                    var timeout = GetAdaptiveTimeout();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

                    // Run LLM on background thread to avoid blocking main game thread
                    var title = card.Title;
                    var desc = card.Description;

                    // Crisis emails come from EngManager
                    var senderEmployee = CompanyDirectory.GetEmployeeForEvent(SenderArchetype.EngManager, card.EventId);
                    var senderName = $"{senderEmployee.Name}, {senderEmployee.Title}";

                    aiBody = await Task.Run(async () =>
                        await emailService.GenerateCrisisEmailAsync(title, desc, false, null, null, null, senderName, cts.Token).ConfigureAwait(false),
                        cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    GD.Print($"[BackgroundProcessor] AI generation timed out for crisis {card.Title}");
                }
                catch (Exception ex)
                {
                    GD.Print($"[BackgroundProcessor] AI generation failed for crisis: {ex.Message}");
                }
            }

            // Validate AI content has enough substance
            const int MinContentLength = 50;
            if (aiBody != null && aiBody.Length < MinContentLength)
            {
                GD.Print($"[BackgroundProcessor] AI content too short for crisis {card.Title}: {aiBody.Length} chars (need {MinContentLength}+), using template");
                aiBody = null;
            }

            // Now create the crisis email and update game state
            CallDeferred(nameof(FinalizeCrisis), card.EventId, card.Title, card.Description,
                task.QuarterNumber, task.OriginatingThreadId ?? "", aiBody ?? "");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BackgroundProcessor] Error processing crisis: {ex.Message}");
            // Still create the crisis with fallback content
            CallDeferred(nameof(FinalizeCrisis), task.CrisisCard.EventId, task.CrisisCard.Title,
                task.CrisisCard.Description, task.QuarterNumber, task.OriginatingThreadId ?? "", "");
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
            }

            // Continue processing queue
            CallDeferred(nameof(ProcessNextInQueue));
        }
    }

    private void FinalizeCrisis(string eventId, string title, string description,
        int quarterNumber, string originatingThreadId, string aiBody)
    {
        GD.Print($"[BackgroundProcessor] Finalizing crisis: {title}");

        // Tell GameManager to create the crisis email and set CurrentCrisis
        _gameManager?.ActivateCrisis(eventId, title, description, quarterNumber,
            string.IsNullOrEmpty(originatingThreadId) ? null : originatingThreadId, aiBody);

        EmitSignal(SignalName.CrisisReady, eventId);
    }

    private void FinalizeProject(ActiveProject project)
    {
        GD.Print($"[BackgroundProcessor] Finalizing project: {project.Title}");

        // Tell GameManager to update email and apply effects
        // (card was already played in QueueProject)
        _gameManager?.FinalizeQueuedProject(project.CardId, project.AiContent);

        // Clean up the queued card
        _queuedCards.TryRemove(project.CardId, out _);

        EmitSignal(SignalName.ProjectReady, project.CardId);
    }

    private void MarkProjectFailed(string cardId, string reason)
    {
        GD.Print($"[BackgroundProcessor] Project failed: {cardId} - {reason}");

        if (_activeProjects.TryGetValue(cardId, out var project))
        {
            project.ContentReady = true; // Allow completion with fallback
        }
    }

    private async void ProcessExternalAiRequestAsync(AiGenerationRequest request)
    {
        try
        {
            // Check if LLM is ready
            if (!LlmServiceManager.IsReady)
            {
                GD.Print($"[BackgroundProcessor] LLM not ready, returning empty for {request.Title}");
                InvokeCallback(request.RequestId, "");
                return;
            }

            var emailService = LlmServiceManager.GetEmailService();
            string? aiBody = null;

            var timeout = GetAdaptiveTimeout();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

            // Run LLM on background thread to avoid blocking main game thread
            aiBody = await Task.Run(async () =>
            {
                switch (request.PromptType)
                {
                    case AiPromptType.ProjectCard:
                        var profit = request.AdditionalContext?.GetValueOrDefault("profit");
                        var effects = request.AdditionalContext?.GetValueOrDefault("effects");
                        var projectSender = request.AdditionalContext?.GetValueOrDefault("senderName");
                        GD.Print($"[BackgroundProcessor] Generating project email for {request.Title}");
                        return await (emailService?.GenerateCardEmailAsync(
                            request.Title,
                            request.Description,
                            request.OutcomeText,
                            profit,
                            effects,
                            projectSender,
                            cts.Token) ?? Task.FromResult<string?>(null)).ConfigureAwait(false);

                    case AiPromptType.CrisisInitial:
                        var crisisInitialSender = request.AdditionalContext?.GetValueOrDefault("senderName");
                        if (string.IsNullOrEmpty(crisisInitialSender))
                        {
                            var emp = CompanyDirectory.GetEmployeeForEvent(SenderArchetype.EngManager, request.RequestId);
                            crisisInitialSender = $"{emp.Name}, {emp.Title}";
                        }
                        GD.Print($"[BackgroundProcessor] Generating crisis email for {request.Title}");
                        return await (emailService?.GenerateCrisisEmailAsync(
                            request.Title,
                            request.Description,
                            isResolution: false,
                            choiceLabel: null,
                            outcomeTier: null,
                            effects: null,
                            crisisInitialSender,
                            cts.Token) ?? Task.FromResult<string?>(null)).ConfigureAwait(false);

                    case AiPromptType.CrisisResponse:
                        var choiceLabel = request.AdditionalContext?.GetValueOrDefault("choiceLabel");
                        var outcomeTier = request.AdditionalContext?.GetValueOrDefault("outcomeTier");
                        var crisisEffects = request.AdditionalContext?.GetValueOrDefault("effects");
                        var crisisRespSender = request.AdditionalContext?.GetValueOrDefault("senderName");
                        if (string.IsNullOrEmpty(crisisRespSender))
                        {
                            var emp = CompanyDirectory.GetEmployeeForEvent(SenderArchetype.EngManager, request.RequestId);
                            crisisRespSender = $"{emp.Name}, {emp.Title}";
                        }
                        GD.Print($"[BackgroundProcessor] Generating crisis response for {request.Title} (effects: {crisisEffects ?? "none"})");
                        return await (emailService?.GenerateCrisisEmailAsync(
                            request.Title,
                            request.Description,
                            isResolution: true,
                            choiceLabel,
                            outcomeTier,
                            crisisEffects,
                            crisisRespSender,
                            cts.Token) ?? Task.FromResult<string?>(null)).ConfigureAwait(false);

                    case AiPromptType.FreeformEmail:
                        var recipient = request.AdditionalContext?.GetValueOrDefault("recipient") ?? "All Staff";
                        var freeformSender = request.AdditionalContext?.GetValueOrDefault("senderName");
                        if (string.IsNullOrEmpty(freeformSender))
                        {
                            var emp = CompanyDirectory.GetEmployeeForEvent(SenderArchetype.PM, request.RequestId);
                            freeformSender = $"{emp.Name}, {emp.Title}";
                        }
                        GD.Print($"[BackgroundProcessor] Generating freeform response for {request.Title}");
                        return await (emailService?.GenerateFreeformResponseAsync(
                            request.Title,
                            request.Description,
                            recipient,
                            freeformSender,
                            cts.Token) ?? Task.FromResult<string?>(null)).ConfigureAwait(false);

                    default:
                        return null;
                }
            }, cts.Token).ConfigureAwait(false);

            // Validate AI content has enough substance
            const int MinContentLength = 50;
            if (aiBody != null && aiBody.Length < MinContentLength)
            {
                GD.Print($"[BackgroundProcessor] AI content too short: {aiBody.Length} chars (need {MinContentLength}+), using fallback");
                aiBody = null;
            }

            if (!string.IsNullOrWhiteSpace(aiBody))
            {
                GD.Print($"[BackgroundProcessor] AI generated: {aiBody[..Math.Min(50, aiBody.Length)]}...");
            }

            CallDeferred(nameof(InvokeCallback), request.RequestId, aiBody ?? "");
        }
        catch (OperationCanceledException)
        {
            GD.Print($"[BackgroundProcessor] AI generation timed out for {request.Title}");
            CallDeferred(nameof(InvokeCallback), request.RequestId, "");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BackgroundProcessor] AI generation failed: {ex.Message}");
            CallDeferred(nameof(InvokeCallback), request.RequestId, "");
        }
        finally
        {
            lock (_processingLock)
            {
                _isProcessing = false;
            }
            CallDeferred(nameof(ProcessNextInQueue));
        }
    }

    private void InvokeCallback(string requestId, string aiBody)
    {
        if (_pendingCallbacks.TryGetValue(requestId, out var callback))
        {
            _pendingCallbacks.Remove(requestId);
            callback?.Invoke(aiBody);
        }
    }

    /// <summary>
    /// A project card waiting to be processed.
    /// </summary>
    private sealed record PendingProjectTask(PlayableCard Card);

    /// <summary>
    /// A crisis waiting to be processed.
    /// </summary>
    private sealed record PendingCrisisTask(EventCard CrisisCard, int QuarterNumber, string? OriginatingThreadId);

    /// <summary>
    /// An external AI generation request.
    /// </summary>
    private sealed record AiGenerationRequest(
        string RequestId,
        string Title,
        string Description,
        string OutcomeText,
        AiPriority Priority,
        AiPromptType PromptType,
        Action<string>? OnComplete,
        Dictionary<string, string>? AdditionalContext);
}

/// <summary>
/// Represents an active project being processed in the background.
/// </summary>
public sealed class ActiveProject
{
    private static readonly Random Rng = new();

    public string CardId { get; }
    public string Title { get; }
    public string Description { get; }
    public float ElapsedTime { get; set; }
    public bool ContentReady { get; set; }
    public string? AiContent { get; set; }

    // Outcome data captured after playing the card
    public string? OutcomeText { get; set; }
    public string? ProfitImpact { get; set; }
    public string? MeterEffects { get; set; }
    public string? PendingThreadId { get; set; }

    // Phase-based progress
    private ProjectPhase _phase = ProjectPhase.Queued;
    private int _messageIndex;
    private float _messageTimer;
    private float _fakeProgress;

    private enum ProjectPhase { Queued, Playing, Generating, Finalizing, Complete }

    private static readonly string[] GeneratingMessages = new[]
    {
        "Synergizing stakeholder requirements...",
        "Leveraging cross-functional deliverables...",
        "Aligning strategic initiatives...",
        "Optimizing paradigm shifts...",
        "Cascading to key stakeholders...",
        "Evaluating ROI metrics...",
        "Circling back with learnings...",
        "Actioning next steps...",
        "Driving operational excellence...",
        "Building consensus...",
        "Maximizing synergies...",
        "Pivoting to value creation..."
    };

    public ActiveProject(string cardId, string title, string description)
    {
        CardId = cardId;
        Title = title;
        Description = description;
        _messageIndex = Rng.Next(GeneratingMessages.Length);
    }

    /// <summary>
    /// Advances to the next phase.
    /// </summary>
    public void AdvancePhase()
    {
        if (_phase < ProjectPhase.Complete)
        {
            _phase++;
            _fakeProgress = 0;
        }
    }

    /// <summary>
    /// Sets the phase directly.
    /// </summary>
    public void SetPhase(int phase)
    {
        _phase = (ProjectPhase)Math.Clamp(phase, 0, 4);
        _fakeProgress = 0;
    }

    /// <summary>
    /// Update progress animation.
    /// </summary>
    public void UpdateProgress(float delta)
    {
        ElapsedTime += delta;

        if (_phase == ProjectPhase.Generating)
        {
            // Slowly increment fake progress during generation (takes ~60 seconds with CPU LLM)
            _fakeProgress = Math.Min(1.0f, _fakeProgress + delta * 0.015f);

            // Rotate messages every 2 seconds
            _messageTimer += delta;
            if (_messageTimer >= 2.0f)
            {
                _messageTimer = 0;
                _messageIndex = (_messageIndex + 1) % GeneratingMessages.Length;
            }
        }
        else if (_phase == ProjectPhase.Finalizing)
        {
            // Quick finish animation
            _fakeProgress = Math.Min(1.0f, _fakeProgress + delta * 2.0f);
            if (_fakeProgress >= 1.0f)
            {
                _phase = ProjectPhase.Complete;
            }
        }
    }

    /// <summary>
    /// Progress percentage (0-100) for UI display.
    /// </summary>
    public int ProgressPercent => _phase switch
    {
        ProjectPhase.Queued => 5,
        ProjectPhase.Playing => 15,
        ProjectPhase.Generating => 25 + (int)(_fakeProgress * 60), // 25-85%
        ProjectPhase.Finalizing => 85 + (int)(_fakeProgress * 15), // 85-100%
        ProjectPhase.Complete => 100,
        _ => 0
    };

    /// <summary>
    /// Whether the project is ready to complete.
    /// </summary>
    public bool IsComplete => _phase == ProjectPhase.Complete;

    /// <summary>
    /// Status message for UI.
    /// </summary>
    public string StatusMessage => _phase switch
    {
        ProjectPhase.Queued => "Initializing workstream...",
        ProjectPhase.Playing => "Queued - Waiting for AI...",
        ProjectPhase.Generating => GeneratingMessages[_messageIndex],
        ProjectPhase.Finalizing => "Preparing executive summary...",
        ProjectPhase.Complete => "Report ready!",
        _ => "Processing..."
    };
}
