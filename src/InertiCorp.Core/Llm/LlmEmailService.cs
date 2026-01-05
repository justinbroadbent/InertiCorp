using System.Text;
using LLama;
using LLama.Common;

namespace InertiCorp.Core.Llm;

/// <summary>
/// Service for generating dynamic emails using a local LLM.
/// </summary>
public sealed class LlmEmailService : IDisposable
{
    private readonly ModelManager _modelManager;
    private readonly Random _flavorRng = new();

    private LLamaWeights? _model;
    private ModelParams? _modelParams;
    private ModelInfo? _activeModelInfo;
    private LLamaContext? _sharedContext;  // Reusable context for faster inference

    private const int MaxTokens = 190;  // 2-3 sentence emails, with buffer for completion

    // Rotating humor styles for variety (Dilbert/Office Space inspired)
    private static readonly string[] ProjectFlavors =
    [
        "Take credit while deflecting blame",
        "Mention synergy or alignment unnecessarily",
        "Reference a meeting that could have been an email",
        "Imply someone else dropped the ball",
        "Use metrics that sound impressive but mean nothing",
        "Suggest forming a committee or task force",
        "Passive-aggressively CC someone's manager",
        "Celebrate mediocrity as if it were exceptional",
        "Blame the timeline while praising the team",
        "Reference 'learnings' or 'takeaways' earnestly",
        "Declare victory before results are measured",
        "Suggest we 'leverage' something abstract",
        "Mention stakeholder alignment with a straight face",
        "Reference the strategic roadmap vaguely",
        "Use 'pivot' as if it were planned all along",
        "Praise the team's resilience through self-inflicted chaos",
        "Invoke best practices without naming any",
        "Suggest this positions us well for Q-whatever",
        "Reference cross-functional collaboration unnecessarily",
        "Claim the budget was always this tight"
    ];

    private static readonly string[] CrisisFlavors =
    [
        "Blame a vendor or external factor",
        "Insist this was unforeseeable despite warnings",
        "Suggest an all-hands meeting to discuss feelings",
        "Reference the importance of 'optics'",
        "Imply legal wants to be looped in",
        "Mention this could affect bonuses",
        "Suggest someone volunteer to own this",
        "Reference previous similar disasters casually",
        "Propose a retrospective before fixing anything",
        "Wonder aloud who approved this originally",
        "Ask if we have insurance for this",
        "Suggest everyone stay calm while panicking",
        "Reference the crisis communication playbook",
        "Wonder if competitors are experiencing this too",
        "Propose forming an emergency task force",
        "Suggest this is actually an opportunity",
        "Ask who's handling the press inquiry",
        "Reference that time something similar happened",
        "Imply executive leadership should be informed carefully",
        "Suggest we control the narrative proactively"
    ];

    private static readonly string[] ResponseFlavors =
    [
        "Agree enthusiastically while changing nothing",
        "Promise to circle back and never do so",
        "Redirect to a different department entirely",
        "Suggest the CEO's idea was already in progress",
        "Misinterpret the request deliberately",
        "Provide technically correct but useless info",
        "Reference bandwidth or capacity constraints",
        "Suggest a pilot program to delay commitment",
        "Ask clarifying questions to buy time",
        "Invoke process or policy apologetically",
        "Offer to schedule a meeting to discuss further",
        "Claim alignment with the CEO's vision vaguely",
        "Mention competing priorities diplomatically",
        "Suggest the request is already in the backlog",
        "Reference resource constraints sympathetically",
        "Propose a phased approach to delay everything",
        "Thank the CEO for their leadership enthusiastically",
        "Mention needing to loop in another stakeholder",
        "Suggest we revisit this next quarter",
        "Reference ongoing transformation initiatives"
    ];

    /// <summary>
    /// Event raised when the LLM is ready for generation.
    /// </summary>
    public event Action? Ready;

    /// <summary>
    /// Event raised when loading fails.
    /// </summary>
    public event Action<Exception>? LoadFailed;

    /// <summary>
    /// Whether the service is ready to generate emails.
    /// </summary>
    public bool IsReady => _model != null;

    /// <summary>
    /// Whether the service is currently loading a model.
    /// </summary>
    public bool IsLoading { get; private set; }

    /// <summary>
    /// Whether the service is currently generating content.
    /// Note: This always returns false as generation is now handled synchronously via BackgroundEmailProcessor.
    /// </summary>
    public bool IsGenerating => false;

    /// <summary>
    /// The name of the currently loaded model.
    /// </summary>
    public string? LoadedModelName => _activeModelInfo?.Name;

    public LlmEmailService(ModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    /// <summary>
    /// Loads the active model. Call this during game initialization.
    /// </summary>
    public async Task LoadModelAsync(CancellationToken ct = default)
    {
        var modelPath = _modelManager.GetActiveModelPath();
        if (modelPath == null)
        {
            throw new InvalidOperationException("No active model configured");
        }

        var modelId = _modelManager.ActiveModelId!;
        var modelInfo = ModelCatalog.GetById(modelId)
            ?? throw new InvalidOperationException($"Unknown model: {modelId}");

        await LoadModelFromPathAsync(modelPath, modelInfo, ct);
    }

    /// <summary>
    /// Loads a model from a specific path.
    /// </summary>
    private async Task LoadModelFromPathAsync(string modelPath, ModelInfo modelInfo, CancellationToken ct)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            // Unload existing model
            UnloadModel();

            // Optimize parameters based on model tier
            var (contextSize, gpuLayers) = GetOptimizedParams(modelInfo);

            const int cpuThreads = 4;  // Lower thread count leaves CPU headroom for audio subsystem

            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = gpuLayers,
                BatchSize = 128,  // Smaller batches reduce CPU spikes
                Threads = cpuThreads,
            };

            _model = await LLamaWeights.LoadFromFileAsync(_modelParams, ct);
            _activeModelInfo = modelInfo;

            // Record diagnostics for status display
            LlmDiagnostics.RecordModelParams(gpuLayers, cpuThreads);

            // Create a shared context for faster inference (avoids recreating each time)
            _sharedContext = _model.CreateContext(_modelParams);

            Ready?.Invoke();
        }
        catch (Exception ex)
        {
            LlmDiagnostics.LogException("LoadModelFromPathAsync", ex);
            _model?.Dispose();
            _model = null;
            _activeModelInfo = null;
            LoadFailed?.Invoke(ex);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets optimized context size and GPU layers based on model tier.
    /// Smaller context = faster inference for short prompts.
    /// </summary>
    private static (uint contextSize, int gpuLayers) GetOptimizedParams(ModelInfo modelInfo)
    {
        return modelInfo.Tier switch
        {
            // TinyLlama, small Qwen - minimal context, full GPU offload
            ModelTier.Fast => (512u, 50),

            // Phi-3 Mini - balanced
            ModelTier.Balanced => (768u, 40),

            // Mistral 7B - larger but still optimized
            ModelTier.Quality => (1024u, 35),

            _ => (768u, 35)
        };
    }

    /// <summary>
    /// Unloads the current model.
    /// </summary>
    public void UnloadModel()
    {
        _sharedContext?.Dispose();
        _sharedContext = null;
        _model?.Dispose();
        _model = null;
        _activeModelInfo = null;
    }

    /// <summary>
    /// Gets optimized inference parameters based on model tier.
    /// Fast models use lower temperature for quicker sampling.
    /// </summary>
    private InferenceParams GetOptimizedInferenceParams(int maxTokens)
    {
        var (temp, topP) = _activeModelInfo!.Tier switch
        {
            ModelTier.Fast => (0.7f, 0.85f),      // Lower = faster, more deterministic
            ModelTier.Balanced => (0.75f, 0.88f),
            ModelTier.Quality => (0.8f, 0.9f),
            _ => (0.75f, 0.88f)
        };

        return new InferenceParams
        {
            MaxTokens = maxTokens,
            AntiPrompts = _activeModelInfo.StopTokens,
            SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
            {
                Temperature = temp,
                TopP = topP,
            },
        };
    }

    /// <summary>
    /// Generates an email body for a card result.
    /// Returns the generated text or null if not ready.
    /// </summary>
    public async Task<string?> GenerateCardEmailAsync(
        string cardTitle,
        string cardDescription,
        string outcomeTier,
        string? profitImpact = null,
        string? meterEffects = null,
        string? senderName = null,
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildCardPrompt(cardTitle, cardDescription, outcomeTier, profitImpact, meterEffects, senderName);
            var executor = new StatelessExecutor(_model, _modelParams);
            var inferenceParams = GetOptimizedInferenceParams(MaxTokens);

            var result = new StringBuilder();
            var tokenCount = 0;
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct).ConfigureAwait(false))
            {
                result.Append(token);
                // Yield frequently to let audio subsystem get CPU time
                if (++tokenCount % 2 == 0)
                    await Task.Delay(2, ct).ConfigureAwait(false);
            }

            return CleanEmailBody(result.ToString());
        }
        catch
        {
            return null;
        }
    }

    private string BuildCardPrompt(string cardTitle, string cardDescription, string outcomeTier, string? profitImpact = null, string? meterEffects = null, string? senderName = null)
    {
        // Build results context from project execution
        var resultsContext = new StringBuilder();
        if (!string.IsNullOrEmpty(profitImpact))
        {
            resultsContext.Append($"Financial impact: {profitImpact}. ");
        }
        if (!string.IsNullOrEmpty(meterEffects))
        {
            resultsContext.Append($"Organization effects: {meterEffects}. ");
        }

        // Pick a random humor flavor for variety
        var flavor = ProjectFlavors[_flavorRng.Next(ProjectFlavors.Length)];

        // Include sender context if available
        var senderContext = !string.IsNullOrEmpty(senderName)
            ? $"You are {senderName} writing to the CEO."
            : "You are a middle manager writing to the CEO.";

        // Tone guidance with example phrases - helps small LLMs understand the style
        var (toneGuidance, systemPrompt) = outcomeTier switch
        {
            "Good" => (
                """
                TRIUMPHANT & SELF-CONGRATULATORY tone. You are taking credit for everything.
                Use phrases like: "Thanks to my leadership...", "As I predicted...", "My strategy delivered...", "I'm proud to announce MY team...", "This validates my vision..."
                Be smug. Be self-aggrandizing. Imply others just followed your genius plan.
                """,
                "You write TRIUMPHANT corporate emails. You take full credit for successes. Smug, self-congratulatory, glorifying yourself. 2-3 sentences. Body only. Never break character."
            ),
            "Bad" => (
                """
                BLAME-SHIFTING & PASSIVE-AGGRESSIVE tone. You are deflecting all responsibility.
                Use phrases like: "Despite vendor failures...", "Given the unrealistic timeline OTHERS committed to...", "If certain teams had delivered as promised...", "The market conditions nobody could have predicted...", "Per my earlier warnings that went unheeded...", "I flagged this risk months ago..."
                NEVER take any blame. Point fingers at vendors, other departments, the economy, or vague 'circumstances'. Be passive-aggressive. Imply someone else screwed up.
                """,
                "You write DEFENSIVE blame-shifting emails. You NEVER accept fault. Passive-aggressive, finger-pointing, CYA language. Blame vendors, other teams, market conditions. 2-3 sentences. Body only. Never break character."
            ),
            _ => (
                """
                CORPORATE SPIN tone. Bury the bad news in positive framing.
                Use phrases like: "Despite some headwinds...", "While we faced challenges, we delivered value...", "This positions us well for...", "Key learnings emerged...", "We exceeded expectations in several areas..."
                Minimize negatives, exaggerate positives. Classic corporate doublespeak.
                """,
                "You write SPIN-DOCTOR corporate emails. Hide bad news in positive framing. Corporate doublespeak, minimize negatives, exaggerate wins. 2-3 sentences. Body only. Never break character."
            )
        };

        // Concise prompt for faster generation - 1 paragraph
        var userPrompt = $"""
            Write a SHORT satirical corporate email (2-3 sentences, one paragraph).

            {senderContext}
            Project: {cardTitle} - {cardDescription}
            Outcome: {outcomeTier}
            {(resultsContext.Length > 0 ? $"Results: {resultsContext}" : "")}

            {toneGuidance}
            Humor style: {flavor}
            Body only, no greeting/signature.
            """;

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }

    /// <summary>
    /// Generates an email body for a crisis event.
    /// Returns the generated text or null if not ready.
    /// </summary>
    public async Task<string?> GenerateCrisisEmailAsync(
        string crisisTitle,
        string crisisDescription,
        bool isResolution = false,
        string? choiceLabel = null,
        string? outcomeTier = null,
        string? effects = null,
        string? senderName = null,
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildCrisisPrompt(crisisTitle, crisisDescription, isResolution, choiceLabel, outcomeTier, effects, senderName);
            var executor = new StatelessExecutor(_model, _modelParams);
            var inferenceParams = GetOptimizedInferenceParams(MaxTokens);

            var result = new StringBuilder();
            var tokenCount = 0;
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct).ConfigureAwait(false))
            {
                result.Append(token);
                // Yield frequently to let audio subsystem get CPU time
                if (++tokenCount % 2 == 0)
                    await Task.Delay(2, ct).ConfigureAwait(false);
            }

            return CleanEmailBody(result.ToString());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a humorous corporate response to a freeform email from the CEO.
    /// Returns the generated response or null if not ready.
    /// </summary>
    public async Task<string?> GenerateFreeformResponseAsync(
        string subject,
        string ceoMessage,
        string recipient,
        string? senderName = null,
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildFreeformPrompt(subject, ceoMessage, recipient, senderName);
            var executor = new StatelessExecutor(_model, _modelParams);
            // Use slightly higher token count for freeform, but still optimized
            var inferenceParams = GetOptimizedInferenceParams(MaxTokens + 20);

            var result = new StringBuilder();
            var tokenCount = 0;
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct).ConfigureAwait(false))
            {
                result.Append(token);
                // Yield frequently to let audio subsystem get CPU time
                if (++tokenCount % 2 == 0)
                    await Task.Delay(2, ct).ConfigureAwait(false);
            }

            return CleanEmailBody(result.ToString());
        }
        catch
        {
            return null;
        }
    }

    private string BuildFreeformPrompt(string subject, string ceoMessage, string recipient, string? senderName = null)
    {
        // Pick a random humor flavor for variety
        var flavor = ResponseFlavors[_flavorRng.Next(ResponseFlavors.Length)];

        // Include sender context if available
        var senderContext = !string.IsNullOrEmpty(senderName)
            ? $"You are {senderName} replying to the CEO."
            : "You are a middle manager replying to the CEO.";

        var userPrompt = $"""
            {senderContext}
            CEO emailed {recipient} about "{subject}": "{ceoMessage}"

            Write a SHORT reply (2-3 sentences). Humor style: {flavor}
            Body only. Vary your vocabulary.
            """;

        var systemPrompt = "You're a middle manager at InertiCorp replying to the CEO. Office Space humor - agreeable on surface, subtly unhelpful. 2-3 sentences. Body only. No placeholders like [NAME] - write complete text. NEVER break character or add disclaimers.";

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }

    private string BuildCrisisPrompt(string crisisTitle, string crisisDescription, bool isResolution, string? choiceLabel, string? outcomeTier, string? effects = null, string? senderName = null)
    {
        // Pick a random humor flavor for variety
        var flavor = CrisisFlavors[_flavorRng.Next(CrisisFlavors.Length)];
        string userPrompt;

        // Include sender context if available
        var senderContext = !string.IsNullOrEmpty(senderName)
            ? $"You are {senderName} writing to the CEO."
            : "You are a panicked manager writing to the CEO.";

        if (isResolution)
        {
            var effectsLine = !string.IsNullOrEmpty(effects) ? $" Result: {effects}" : "";
            var (toneGuidance, toneSystemPrompt) = outcomeTier switch
            {
                "Good" => (
                    """
                    TRIUMPHANT & SELF-CONGRATULATORY. You saved the day and everyone should know it.
                    Use phrases like: "Thanks to my decisive action...", "My quick thinking prevented...", "As I suspected all along...", "This proves my judgment was correct..."
                    Take ALL the credit. Be smug about your crisis management skills.
                    """,
                    "You write TRIUMPHANT crisis resolution emails. You're the hero. Smug, self-congratulatory. 2-3 sentences. Body only. Never break character."
                ),
                "Bad" => (
                    """
                    BLAME-SHIFTING & DEFENSIVE. This was NOT your fault and you need to make that clear.
                    Use phrases like: "Despite being handed an impossible situation...", "Given the failures of the previous team...", "Had proper resources been allocated as I requested...", "The vendor's negligence caused...", "I inherited this problem from...", "Per my documented concerns that were ignored..."
                    NEVER accept any blame. Be passive-aggressive. Imply others caused this mess and you did your best with what you were given.
                    """,
                    "You write DEFENSIVE blame-shifting crisis emails. NEVER accept fault. Point fingers, deflect, CYA. 2-3 sentences. Body only. Never break character."
                ),
                _ => (
                    """
                    CORPORATE SPIN. The crisis is 'contained' and there are 'learnings'.
                    Use phrases like: "While the situation was challenging...", "We've emerged stronger...", "Key process improvements identified...", "This stress-tested our systems..."
                    Make it sound like a growth opportunity, not a disaster.
                    """,
                    "You write SPIN-DOCTOR crisis emails. Frame disasters as learning opportunities. Corporate doublespeak. 2-3 sentences. Body only. Never break character."
                )
            };

            userPrompt = $"""
                Write a SHORT satirical email (2-3 sentences).

                {senderContext}
                Crisis "{crisisTitle}" resolved via "{choiceLabel ?? "action"}".{effectsLine}

                {toneGuidance}
                Humor style: {flavor}
                Body only.
                """;

            // Use tone-specific system prompt for crisis resolution
            var crisisSystemPrompt = toneSystemPrompt;
            return string.Format(_activeModelInfo!.PromptFormat, userPrompt, crisisSystemPrompt);
        }
        else
        {
            userPrompt = $"""
                Write a SHORT URGENT satirical email (2-3 sentences).

                {senderContext}
                CRISIS at InertiCorp: {crisisTitle} - {crisisDescription}

                Humor style: {flavor}
                Body only. Vary your vocabulary.
                """;
        }

        var systemPrompt = "You write Dilbert-style crisis emails for InertiCorp. Panicked but corporate - concerned about optics, blame-shifting, CYA language. 2-3 sentences. Body only. No placeholders like [NAME] - write complete text. NEVER break character or add disclaimers.";

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }


    private static string CleanEmailBody(string raw)
    {
        var cleaned = raw.Trim();

        // Remove common unwanted prefixes
        var prefixes = new[] { "Subject:", "From:", "To:", "Dear CEO,", "Hi CEO," };
        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var newlineIndex = cleaned.IndexOf('\n');
                if (newlineIndex > 0)
                    cleaned = cleaned[(newlineIndex + 1)..].Trim();
            }
        }

        // Only remove signatures if they're near the end of the content (last 30%)
        // This prevents cutting off content that happens to contain "Best," etc.
        var signatureMarkers = new[] { "\nBest,", "\nRegards,", "\nSincerely,", "\n--", "\nBest regards,", "\nThank you," };
        foreach (var marker in signatureMarkers)
        {
            var idx = cleaned.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            // Only trim if the signature is in the last 30% of the content
            if (idx > 0 && idx > cleaned.Length * 0.7)
            {
                cleaned = cleaned[..idx].Trim();
                break; // Only remove one signature
            }
        }

        return cleaned;
    }

    public void Dispose()
    {
        _sharedContext?.Dispose();
        _sharedContext = null;
        _model?.Dispose();
    }
}
