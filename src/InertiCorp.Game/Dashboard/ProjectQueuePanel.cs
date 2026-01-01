using System.Threading;
using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Llm;

namespace InertiCorp.Game.Dashboard;

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
/// UI panel showing "active projects" - cards being processed with progress bars.
/// Cards are played immediately when added, but the email is hidden until AI content
/// is generated (or timeout). This creates a natural delay for AI generation.
/// </summary>
public partial class ProjectQueuePanel : PanelContainer
{
    private VBoxContainer? _projectsContainer;
    private Label? _headerLabel;
    private readonly List<ActiveProjectEntry> _projects = new();
    private readonly Dictionary<string, PlayableCard> _queuedCards = new();
    private readonly Dictionary<string, string> _pendingThreadIds = new(); // cardId -> threadId
    private GameManager? _gameManager;

    // Serial AI generation queue - LLamaSharp doesn't handle concurrent inference well
    // Uses priority list instead of simple queue - high priority items processed first
    private readonly List<AiGenerationRequest> _aiQueue = new();
    private bool _isGenerating;
    private readonly object _queueLock = new();

    // Static instance for external access (crisis AI generation)
    private static ProjectQueuePanel? _instance;

    /// <summary>
    /// Maximum number of concurrent projects.
    /// </summary>
    public const int MaxProjects = 3;

    /// <summary>
    /// Minimum display time for a project (seconds).
    /// </summary>
    public const float MinDisplayTime = 3.0f;

    /// <summary>
    /// Maximum display time before auto-completing (seconds).
    /// Gives AI plenty of time to generate content.
    /// </summary>
    public const float MaxDisplayTime = 30.0f;

    /// <summary>
    /// Whether there are projects in the queue.
    /// </summary>
    public bool HasActiveProjects => _projects.Count > 0;

    /// <summary>
    /// Whether the queue is at capacity.
    /// </summary>
    public bool IsFull => _projects.Count >= MaxProjects;

    /// <summary>
    /// Thread IDs that are pending (card played but waiting for AI content).
    /// These should be hidden from inbox display.
    /// </summary>
    public IReadOnlyCollection<string> PendingThreadIds => _pendingThreadIds.Values.ToHashSet();

    /// <summary>
    /// Event raised when a project completes.
    /// </summary>
    [Signal]
    public delegate void ProjectCompletedEventHandler(string cardId);

    /// <summary>
    /// Queues an external AI generation request (for crisis emails, etc.).
    /// Lower priority than project cards - will be processed after current high-priority items.
    /// </summary>
    /// <param name="requestId">Unique ID for this request (e.g., threadId)</param>
    /// <param name="title">Title/subject for the AI prompt</param>
    /// <param name="description">Description/context for the AI prompt</param>
    /// <param name="promptType">Type of content to generate</param>
    /// <param name="priority">Priority level (High, Normal, Low)</param>
    /// <param name="onComplete">Callback with generated content (or empty string on failure)</param>
    /// <param name="additionalContext">Optional additional context for generation</param>
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
            GD.PrintErr("[ProjectQueue] No instance available for external AI request");
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
        lock (_queueLock)
        {
            // Store callback for later invocation (can't pass delegates through CallDeferred)
            _pendingCallbacks[requestId] = onComplete;

            var request = new AiGenerationRequest(
                requestId, title, description, "", // outcomeText not used for external
                priority, promptType, onComplete, additionalContext);

            _aiQueue.Add(request);
            // Sort by priority (lower enum value = higher priority)
            _aiQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            GD.Print($"[ProjectQueue] Queued external AI request: {title} (priority: {priority}, queue size: {_aiQueue.Count})");

            if (!_isGenerating)
            {
                ProcessNextInQueue();
            }
        }
    }

    public override void _Ready()
    {
        _instance = this;
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style the panel
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 0.95f),
            BorderColor = new Color(0.2f, 0.25f, 0.3f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        style.BorderWidthTop = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        AddThemeStyleboxOverride("panel", style);

        // Main container
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        AddChild(vbox);

        // Header
        _headerLabel = new Label
        {
            Text = "ACTIVE PROJECTS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 11);
        _headerLabel.Modulate = new Color(0.6f, 0.65f, 0.7f);
        vbox.AddChild(_headerLabel);

        vbox.AddChild(new HSeparator());

        // Projects container
        _projectsContainer = new VBoxContainer();
        _projectsContainer.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(_projectsContainer);

        // Initially hidden when empty
        UpdateVisibility();
    }

    public override void _Process(double delta)
    {
        if (_projects.Count == 0) return;

        var completedProjects = new List<ActiveProjectEntry>();

        foreach (var project in _projects)
        {
            project.Update((float)delta);

            if (project.IsComplete)
            {
                completedProjects.Add(project);
            }
        }

        // Process completions
        foreach (var completed in completedProjects)
        {
            _projects.Remove(completed);
            _queuedCards.Remove(completed.CardId);
            _pendingThreadIds.Remove(completed.CardId); // Remove from pending - email now visible
            completed.Container?.QueueFree();

            // Apply the deferred effects now that the project is done
            _gameManager?.ApplyDeferredEffects(completed.CardId);

            EmitSignal(SignalName.ProjectCompleted, completed.CardId);
            GD.Print($"[ProjectQueue] Project completed: {completed.CardId}");
        }

        UpdateHeader();
        UpdateVisibility();
    }

    /// <summary>
    /// Adds a card to the project queue.
    /// The card is played immediately to determine outcome, but the email
    /// is hidden until AI content is generated.
    /// </summary>
    public bool TryAddProject(PlayableCard card)
    {
        if (IsFull) return false;
        if (_gameManager?.CurrentState is null) return false;

        var entry = new ActiveProjectEntry(card.CardId, card.Title, card.Description);
        _projects.Add(entry);

        // Store the card reference
        _queuedCards[card.CardId] = card;

        // Play the card IMMEDIATELY to determine outcome
        // This creates the email but we'll hide it until AI content is ready
        _gameManager.PlayQueuedCard(card.CardId);

        // Get the thread ID for this card (just created)
        var thread = _gameManager.CurrentState.Inbox.GetThreadForCard(card.CardId);
        if (thread != null)
        {
            _pendingThreadIds[card.CardId] = thread.ThreadId;
            GD.Print($"[ProjectQueue] Card {card.CardId} played, thread {thread.ThreadId} marked pending");
        }

        // Get the outcome and result details from the log
        var outcome = _gameManager.LastLog?.Entries
            .FirstOrDefault(e => e.OutcomeTier != null)?.OutcomeTier;

        // Extract profit impact from log entries (e.g., "Profit impact: +$15M")
        string? profitImpact = null;
        var profitEntry = _gameManager.LastLog?.Entries
            .FirstOrDefault(e => e.Message.StartsWith("Profit impact:"));
        if (profitEntry != null)
        {
            profitImpact = profitEntry.Message.Replace("Profit impact: ", "");
        }

        // Extract meter changes from log entries
        var meterChanges = _gameManager.LastLog?.Entries
            .Where(e => e.Category == InertiCorp.Core.LogCategory.MeterChange)
            .Select(e => $"{e.Meter}: {(e.Delta >= 0 ? "+" : "")}{e.Delta}")
            .ToList() ?? new List<string>();

        // Build additional context for LLM
        var additionalContext = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(profitImpact))
        {
            additionalContext["profit"] = profitImpact;
        }
        if (meterChanges.Count > 0)
        {
            additionalContext["effects"] = string.Join(", ", meterChanges);
        }

        // Create UI for this project
        var container = CreateProjectUI(entry);
        entry.Container = container;
        _projectsContainer?.AddChild(container);

        // Start real AI generation with known outcome and results
        TriggerAiGeneration(card, outcome?.ToString() ?? "Expected", additionalContext);

        UpdateHeader();
        UpdateVisibility();
        return true;
    }

    /// <summary>
    /// Gets a queued card by ID (for playing when complete).
    /// </summary>
    public PlayableCard? GetQueuedCard(string cardId)
    {
        return _queuedCards.TryGetValue(cardId, out var card) ? card : null;
    }

    /// <summary>
    /// Marks a project as having received an AI response (success or failure).
    /// Project will complete shortly after this is called.
    /// </summary>
    public void MarkAiResponse(string cardId, bool succeeded)
    {
        var project = _projects.FirstOrDefault(p => p.CardId == cardId);
        project?.MarkAiResponse(succeeded);
    }

    private void TriggerAiGeneration(PlayableCard card, string outcomeText, Dictionary<string, string>? additionalContext = null)
    {
        // Queue the request - AI generation runs serially to avoid GPU contention
        lock (_queueLock)
        {
            var request = new AiGenerationRequest(
                card.CardId, card.Title, card.Description, outcomeText,
                AiPriority.High, AiPromptType.ProjectCard, null, additionalContext);

            _aiQueue.Add(request);
            // Sort by priority (lower enum value = higher priority)
            _aiQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            GD.Print($"[ProjectQueue] Queued AI generation for {card.Title} (priority: High, queue size: {_aiQueue.Count})");

            // Start processing if not already running
            if (!_isGenerating)
            {
                ProcessNextInQueue();
            }
        }
    }

    private void ProcessNextInQueue()
    {
        AiGenerationRequest? request;

        lock (_queueLock)
        {
            if (_aiQueue.Count == 0)
            {
                _isGenerating = false;
                return;
            }

            _isGenerating = true;
            // Take first item (highest priority after sorting)
            request = _aiQueue[0];
            _aiQueue.RemoveAt(0);
        }

        // Check if LLM is ready - if not, re-queue and wait
        if (!LlmServiceManager.IsReady)
        {
            // Re-add to queue and try again later
            lock (_aiQueue)
            {
                _aiQueue.Add(request);
            }
            GD.Print($"[ProjectQueue] LLM not ready, will retry {request.Title} in 2s");

            // Wait 2 seconds before trying again
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                CallDeferred(nameof(ProcessNextInQueue));
            });
            return;
        }

        // Run AI generation in background
        Task.Run(async () =>
        {
            try
            {
                var emailService = LlmServiceManager.GetEmailService();
                string? aiBody = null;

                switch (request.PromptType)
                {
                    case AiPromptType.ProjectCard:
                        var profit = request.AdditionalContext?.GetValueOrDefault("profit");
                        var effects = request.AdditionalContext?.GetValueOrDefault("effects");
                        GD.Print($"[ProjectQueue] Generating project email for {request.Title} ({request.OutcomeText}, profit: {profit ?? "n/a"})");
                        aiBody = await emailService?.GenerateCardEmailAsync(
                            request.Title,
                            request.Description,
                            request.OutcomeText,
                            profit,
                            effects,
                            CancellationToken.None) ?? null;
                        break;

                    case AiPromptType.CrisisInitial:
                        GD.Print($"[ProjectQueue] Generating crisis email for {request.Title}");
                        aiBody = await emailService?.GenerateCrisisEmailAsync(
                            request.Title,
                            request.Description,
                            isResolution: false,
                            choiceLabel: null,
                            outcomeTier: null,
                            CancellationToken.None) ?? null;
                        break;

                    case AiPromptType.CrisisResponse:
                        var choiceLabel = request.AdditionalContext?.GetValueOrDefault("choiceLabel");
                        var outcomeTier = request.AdditionalContext?.GetValueOrDefault("outcomeTier");
                        GD.Print($"[ProjectQueue] Generating crisis response for {request.Title} (choice: {choiceLabel})");
                        aiBody = await emailService?.GenerateCrisisEmailAsync(
                            request.Title,
                            request.Description,
                            isResolution: true,
                            choiceLabel,
                            outcomeTier,
                            CancellationToken.None) ?? null;
                        break;

                    case AiPromptType.FreeformEmail:
                        var recipient = request.AdditionalContext?.GetValueOrDefault("recipient") ?? "All Staff";
                        GD.Print($"[ProjectQueue] Generating freeform response for: {request.Title}");
                        aiBody = await emailService?.GenerateFreeformResponseAsync(
                            request.Title,  // Subject
                            request.Description,  // CEO's message
                            recipient,
                            CancellationToken.None) ?? null;
                        break;
                }

                if (!string.IsNullOrWhiteSpace(aiBody))
                {
                    GD.Print($"[ProjectQueue] AI generated: {aiBody[..Math.Min(50, aiBody.Length)]}...");
                }
                else
                {
                    GD.PrintErr("[ProjectQueue] AI returned empty - GENUINE FAILURE");
                }

                // Handle completion based on request type
                if (request.OnComplete is not null)
                {
                    // External request - call the callback via lookup
                    CallDeferred(nameof(InvokeExternalCallback), request.RequestId, aiBody ?? "");
                }
                else
                {
                    // Internal project request - update via existing method
                    CallDeferred(nameof(OnAiGenerationComplete), request.RequestId, aiBody ?? "");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ProjectQueue] AI generation failed with exception: {ex.Message}");
                // Mark as failed - will use canned content
                if (request.OnComplete is not null)
                {
                    CallDeferred(nameof(InvokeExternalCallback), request.RequestId, "");
                }
                else
                {
                    CallDeferred(nameof(OnAiGenerationComplete), request.RequestId, "");
                }
            }
            finally
            {
                // Process next item in queue
                CallDeferred(nameof(ProcessNextInQueue));
            }
        });
    }

    // Dictionary to hold callbacks that need to be invoked on main thread
    private readonly Dictionary<string, Action<string>> _pendingCallbacks = new();

    private void InvokeExternalCallback(string requestId, string aiBody)
    {
        // Find and invoke the callback
        // Note: We can't pass Action directly through CallDeferred, so we use a lookup
        if (_pendingCallbacks.TryGetValue(requestId, out var callback))
        {
            _pendingCallbacks.Remove(requestId);
            callback?.Invoke(aiBody);
        }
    }

    /// <summary>
    /// Request for AI-generated email content.
    /// </summary>
    private sealed record AiGenerationRequest(
        string RequestId,
        string Title,
        string Description,
        string OutcomeText,
        AiPriority Priority = AiPriority.High,
        AiPromptType PromptType = AiPromptType.ProjectCard,
        Action<string>? OnComplete = null,
        Dictionary<string, string>? AdditionalContext = null);

    private void OnAiGenerationComplete(string cardId, string aiBody)
    {
        var succeeded = !string.IsNullOrWhiteSpace(aiBody);

        // Update the email if we have AI content
        if (succeeded && _pendingThreadIds.TryGetValue(cardId, out var threadId))
        {
            var thread = _gameManager?.CurrentState?.Inbox.GetThread(threadId);
            if (thread != null)
            {
                // Update the reply message (non-CEO message) with AI content
                var updatedMessages = thread.Messages.Select(m =>
                    m.IsFromPlayer ? m : m with { Body = aiBody }).ToList();

                var updatedThread = thread with { Messages = updatedMessages };
                _gameManager?.UpdateThread(threadId, updatedThread);

                GD.Print($"[ProjectQueue] Updated email {threadId} with AI content");
            }
        }

        // Mark AI response received (success or failure) - project can now complete
        MarkAiResponse(cardId, succeeded);
    }

    private Control CreateProjectUI(ActiveProjectEntry entry)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 2);

        // Title row
        var titleRow = new HBoxContainer();
        container.AddChild(titleRow);

        var titleLabel = new Label
        {
            Text = TruncateText(entry.Title, 22),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 10);
        titleRow.AddChild(titleLabel);

        var percentLabel = new Label
        {
            Text = "0%"
        };
        percentLabel.AddThemeFontSizeOverride("font_size", 9);
        percentLabel.Modulate = new Color(0.5f, 0.7f, 0.5f);
        entry.PercentLabel = percentLabel;
        titleRow.AddChild(percentLabel);

        // Progress bar
        var progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 8),
            Value = 0,
            MaxValue = 100,
            ShowPercentage = false
        };
        var bgStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.12f) };
        bgStyle.CornerRadiusTopLeft = 2;
        bgStyle.CornerRadiusTopRight = 2;
        bgStyle.CornerRadiusBottomLeft = 2;
        bgStyle.CornerRadiusBottomRight = 2;
        progressBar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.4f) };
        fillStyle.CornerRadiusTopLeft = 2;
        fillStyle.CornerRadiusTopRight = 2;
        fillStyle.CornerRadiusBottomLeft = 2;
        fillStyle.CornerRadiusBottomRight = 2;
        progressBar.AddThemeStyleboxOverride("fill", fillStyle);
        entry.ProgressBar = progressBar;
        container.AddChild(progressBar);

        // Status message
        var statusLabel = new Label
        {
            Text = entry.StatusMessage
        };
        statusLabel.AddThemeFontSizeOverride("font_size", 8);
        statusLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
        entry.StatusLabel = statusLabel;
        container.AddChild(statusLabel);

        // Hover expansion drawer - shows full details on hover
        var drawerPanel = new PanelContainer
        {
            Visible = false,
            Name = "DrawerPanel"
        };
        var drawerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f),
            BorderWidthLeft = 2,
            BorderWidthRight = 0,
            BorderWidthTop = 0,
            BorderWidthBottom = 0,
            BorderColor = new Color(0.3f, 0.5f, 0.4f, 0.8f),
            ContentMarginLeft = 8,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        drawerPanel.AddThemeStyleboxOverride("panel", drawerStyle);

        var drawerContent = new VBoxContainer();
        drawerContent.AddThemeConstantOverride("separation", 4);
        drawerPanel.AddChild(drawerContent);

        // Full title in drawer
        var fullTitle = new Label
        {
            Text = entry.Title,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        fullTitle.AddThemeFontSizeOverride("font_size", 10);
        fullTitle.Modulate = new Color(0.9f, 0.9f, 0.95f);
        drawerContent.AddChild(fullTitle);

        // Description in drawer
        var descLabel = new Label
        {
            Text = entry.Description,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        descLabel.AddThemeFontSizeOverride("font_size", 9);
        descLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        drawerContent.AddChild(descLabel);

        container.AddChild(drawerPanel);

        // Store references for hover handling
        var titleLabelRef = titleLabel;
        var drawerRef = drawerPanel;
        var fullTitleStr = entry.Title;

        // Hover event handlers
        container.MouseEntered += () =>
        {
            drawerRef.Visible = true;
            titleLabelRef.Text = fullTitleStr; // Show full title when hovered
        };
        container.MouseExited += () =>
        {
            drawerRef.Visible = false;
            titleLabelRef.Text = TruncateText(fullTitleStr, 22); // Back to truncated
        };

        return container;
    }

    private void UpdateHeader()
    {
        if (_headerLabel == null) return;
        _headerLabel.Text = _projects.Count > 0
            ? $"ACTIVE PROJECTS ({_projects.Count})"
            : "ACTIVE PROJECTS";
    }

    private void UpdateVisibility()
    {
        Visible = _projects.Count > 0;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Gets the IDs of all cards currently in the queue.
    /// </summary>
    public IReadOnlyList<string> QueuedCardIds => _projects.Select(p => p.CardId).ToList();

    /// <summary>
    /// Internal class tracking an active project.
    /// </summary>
    private sealed class ActiveProjectEntry
    {
        private static readonly Random Rng = new();

        public string CardId { get; }
        public string Title { get; }
        public string Description { get; }
        public float Progress { get; private set; }
        public string StatusMessage { get; private set; }
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// Whether AI has responded (success or genuine failure).
        /// Projects MUST wait for this before completing.
        /// </summary>
        public bool HasAiResponse { get; private set; }

        /// <summary>
        /// Whether AI generation succeeded (got actual content).
        /// </summary>
        public bool AiSucceeded { get; private set; }

        /// <summary>
        /// Project completes when: progress >= 100 AND AI has responded.
        /// </summary>
        public bool IsComplete => Progress >= 100f && HasAiResponse;

        public Control? Container { get; set; }
        public ProgressBar? ProgressBar { get; set; }
        public Label? PercentLabel { get; set; }
        public Label? StatusLabel { get; set; }

        private int _currentPhase;
        private float _phaseTime;
        private bool _waitingForAi;

        private static readonly string[][] PhaseMessages = new[]
        {
            new[] { "Spinning up synergy engines...", "Bootstrapping innovation pipeline...", "Initializing workstreams..." },
            new[] { "Aligning cross-functional teams...", "Building stakeholder consensus...", "Harmonizing objectives..." },
            new[] { "Leveraging core competencies...", "Driving operational excellence...", "Executing deliverables..." },
            new[] { "Validating impact metrics...", "Reviewing stakeholder feedback...", "Assessing trajectory..." },
            new[] { "Preparing executive summary...", "Compiling action items...", "Finalizing outcomes..." }
        };

        private static readonly string[] WaitingMessages = new[]
        {
            "Awaiting AI analysis...",
            "Processing with neural networks...",
            "Consulting the algorithm...",
            "AI is thinking...",
            "Synthesizing response..."
        };

        public ActiveProjectEntry(string cardId, string title, string description)
        {
            CardId = cardId;
            Title = title;
            Description = description;
            StatusMessage = PhaseMessages[0][Rng.Next(PhaseMessages[0].Length)];
            _phaseTime = 0;
        }

        /// <summary>
        /// Called when AI generation completes (success or genuine failure).
        /// </summary>
        public void MarkAiResponse(bool succeeded)
        {
            HasAiResponse = true;
            AiSucceeded = succeeded;
            GD.Print($"[ProjectQueue] AI response for {CardId}: {(succeeded ? "SUCCESS" : "FAILED")}");
        }

        public void Update(float delta)
        {
            ElapsedTime += delta;
            _phaseTime += delta;

            // Progress goes to 95% over ~60 seconds, then waits for AI
            float targetProgress = HasAiResponse ? 100f : 95f;

            if (Progress < targetProgress)
            {
                // Progress rate: reach 95% in about 60 seconds, fast finish after AI responds
                float progressRate = HasAiResponse ? 50f : 1.58f;
                Progress = Math.Min(targetProgress, Progress + (progressRate * delta));
            }

            // Update phase messages
            if (!_waitingForAi && Progress >= 95f && !HasAiResponse)
            {
                // Switch to waiting for AI message
                _waitingForAi = true;
                StatusMessage = WaitingMessages[Rng.Next(WaitingMessages.Length)];
                StatusLabel?.SetText(StatusMessage);
            }
            else if (!_waitingForAi)
            {
                // Normal phase progression
                int newPhase = Math.Min(4, (int)(Progress / 20f));
                if (newPhase != _currentPhase)
                {
                    _currentPhase = newPhase;
                    StatusMessage = PhaseMessages[_currentPhase][Rng.Next(PhaseMessages[_currentPhase].Length)];
                    StatusLabel?.SetText(StatusMessage);
                }
            }
            else if (_waitingForAi && HasAiResponse)
            {
                // AI responded, show final message
                StatusMessage = AiSucceeded ? "Report ready!" : "Finalizing...";
                StatusLabel?.SetText(StatusMessage);
            }

            // Update UI
            ProgressBar?.SetValue(Progress);
            PercentLabel?.SetText($"{(int)Progress}%");
        }
    }
}
