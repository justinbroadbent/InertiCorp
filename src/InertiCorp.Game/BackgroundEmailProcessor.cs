using System.Collections.Concurrent;
using System.Threading;
using InertiCorp.Core;
using InertiCorp.Core.Cards;
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
    /// Timeout for AI generation before using fallback content.
    /// </summary>
    public const float GenerationTimeoutSeconds = 15.0f;

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
        // Update active project timers
        foreach (var kvp in _activeProjects)
        {
            kvp.Value.ElapsedTime += (float)delta;
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
    /// The card is NOT played immediately - outcome is determined in background.
    /// </summary>
    public void QueueProject(PlayableCard card)
    {
        if (IsFull)
        {
            GD.PrintErr($"[BackgroundProcessor] Project queue full, cannot add {card.Title}");
            return;
        }

        var task = new PendingProjectTask(card);
        _projectQueue.Enqueue(task);

        // Store the full card for later use
        _queuedCards.TryAdd(card.CardId, card);

        // Add to active projects for UI display
        var activeProject = new ActiveProject(card.CardId, card.Title, card.Description);
        _activeProjects.TryAdd(card.CardId, activeProject);

        GD.Print($"[BackgroundProcessor] Queued project: {card.Title}");

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
            GD.Print($"[BackgroundProcessor] Processing project: {card.Title}");

            // Determine outcome (this would normally happen in QuarterEngine)
            // For now, we'll calculate it here and store it
            var state = _gameManager?.CurrentState;
            if (state == null)
            {
                MarkProjectFailed(card.CardId, "No game state");
                return;
            }

            // Generate AI content
            string? aiBody = null;
            var emailService = LlmServiceManager.GetEmailService();

            if (emailService != null && LlmServiceManager.IsReady)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(GenerationTimeoutSeconds));

                    // We need outcome info - for now use Expected as placeholder
                    // The actual outcome will be determined when we play the card
                    aiBody = await emailService.GenerateCardEmailAsync(
                        card.Title,
                        card.Description,
                        "Expected", // Will be updated when card is actually played
                        null,
                        null,
                        cts.Token);
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

            // Update active project with AI content
            if (_activeProjects.TryGetValue(card.CardId, out var activeProject))
            {
                activeProject.AiContent = aiBody;
                activeProject.ContentReady = true;
                GD.Print($"[BackgroundProcessor] Content ready for {card.Title}");
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
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(GenerationTimeoutSeconds));

                    aiBody = await emailService.GenerateCrisisEmailAsync(
                        card.Title,
                        card.Description,
                        isResolution: false,
                        choiceLabel: null,
                        outcomeTier: null,
                        effects: null,
                        cts.Token);
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

        // Tell GameManager to play the card and create the email
        _gameManager?.FinalizeQueuedProject(project.CardId, project.AiContent);

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

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(GenerationTimeoutSeconds));

            switch (request.PromptType)
            {
                case AiPromptType.ProjectCard:
                    var profit = request.AdditionalContext?.GetValueOrDefault("profit");
                    var effects = request.AdditionalContext?.GetValueOrDefault("effects");
                    GD.Print($"[BackgroundProcessor] Generating project email for {request.Title}");
                    aiBody = await emailService?.GenerateCardEmailAsync(
                        request.Title,
                        request.Description,
                        request.OutcomeText,
                        profit,
                        effects,
                        cts.Token) ?? null;
                    break;

                case AiPromptType.CrisisInitial:
                    GD.Print($"[BackgroundProcessor] Generating crisis email for {request.Title}");
                    aiBody = await emailService?.GenerateCrisisEmailAsync(
                        request.Title,
                        request.Description,
                        isResolution: false,
                        choiceLabel: null,
                        outcomeTier: null,
                        effects: null,
                        cts.Token) ?? null;
                    break;

                case AiPromptType.CrisisResponse:
                    var choiceLabel = request.AdditionalContext?.GetValueOrDefault("choiceLabel");
                    var outcomeTier = request.AdditionalContext?.GetValueOrDefault("outcomeTier");
                    var crisisEffects = request.AdditionalContext?.GetValueOrDefault("effects");
                    GD.Print($"[BackgroundProcessor] Generating crisis response for {request.Title} (effects: {crisisEffects ?? "none"})");
                    aiBody = await emailService?.GenerateCrisisEmailAsync(
                        request.Title,
                        request.Description,
                        isResolution: true,
                        choiceLabel,
                        outcomeTier,
                        crisisEffects,
                        cts.Token) ?? null;
                    break;

                case AiPromptType.FreeformEmail:
                    var recipient = request.AdditionalContext?.GetValueOrDefault("recipient") ?? "All Staff";
                    GD.Print($"[BackgroundProcessor] Generating freeform response for {request.Title}");
                    aiBody = await emailService?.GenerateFreeformResponseAsync(
                        request.Title,
                        request.Description,
                        recipient,
                        cts.Token) ?? null;
                    break;
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
    public string CardId { get; }
    public string Title { get; }
    public string Description { get; }
    public float ElapsedTime { get; set; }
    public bool ContentReady { get; set; }
    public string? AiContent { get; set; }

    public ActiveProject(string cardId, string title, string description)
    {
        CardId = cardId;
        Title = title;
        Description = description;
    }

    /// <summary>
    /// Progress percentage (0-100) for UI display.
    /// </summary>
    public int ProgressPercent => Math.Min(100, (int)(ElapsedTime / BackgroundEmailProcessor.MinProjectDisplayTime * 100));

    /// <summary>
    /// Whether the project is ready to complete (content ready + min display time passed).
    /// </summary>
    public bool IsComplete => ContentReady && ElapsedTime >= BackgroundEmailProcessor.MinProjectDisplayTime;

    /// <summary>
    /// Status message for UI.
    /// </summary>
    public string StatusMessage => ContentReady
        ? "Preparing report..."
        : "Processing...";
}
