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

    // Varied opening phrases for Expected outcomes (corporate spin)
    // Small LLMs copy examples verbatim, so we rotate through different opener styles
    private static readonly string[] ExpectedOpeners =
    [
        "Start with: 'While we encountered [obstacle]...' then pivot to something positive",
        "Start with: 'The team navigated [challenge] and...' focusing on resilience",
        "Start with: 'Given the circumstances, we...' implying external factors",
        "Start with: 'After thorough analysis, we...' sounding methodical",
        "Start with: 'In partnership with [stakeholders], we...' spreading credit/blame",
        "Start with: 'Building on previous efforts, we...' implying continuity",
        "Start with: 'Through careful calibration, we...' sounding scientific",
        "Start with: 'Following stakeholder input, we...' diffusing responsibility",
        "Start with: 'With strategic adjustments, we...' implying adaptability",
        "Start with: 'Per the revised timeline, we...' normalizing delays",
        "Start with: 'Leveraging cross-functional support, we...' corporate buzzwords",
        "Start with: 'As anticipated, the project...' pretending this was planned",
        "Start with: 'Within scope constraints, we...' managing expectations",
        "Start with: 'The initiative has reached...' vague progress language",
        "Start with: 'Our measured approach yielded...' sounding deliberate"
    ];

    // Varied opening phrases for Good outcomes (triumphant/self-congratulatory)
    private static readonly string[] GoodOpeners =
    [
        "Start with: 'Thanks to my leadership...' and take full credit",
        "Start with: 'As I predicted...' claiming foresight",
        "Start with: 'I'm proud to report that MY initiative...' ownership language",
        "Start with: 'This validates my strategic vision...' self-validation",
        "Start with: 'My team (under my direction)...' claiming the team's work",
        "Start with: 'Exceeding all expectations, I...' exceeding modesty too",
        "Start with: 'The results speak for themselves...' then speak for them anyway",
        "Start with: 'Against the odds, my approach...' hero narrative",
        "Start with: 'I knew from the start that...' hindsight as foresight",
        "Start with: 'My decisive action resulted in...' decisive self-praise"
    ];

    // Varied opening phrases for Bad outcomes (CYA/blame-shifting)
    private static readonly string[] BadOpeners =
    [
        "Start with: 'As I cautioned in my earlier memo...' claiming you warned them",
        "Start with: 'Despite my reservations...' distancing yourself",
        "Start with: 'The vendor failed to deliver...' external blame",
        "Start with: 'Market forces beyond our control...' blaming the economy",
        "Start with: 'The team assigned to this...' blaming others",
        "Start with: 'Per my documented concerns...' paper trail defense",
        "Start with: 'I inherited this situation...' blaming predecessors",
        "Start with: 'The original scope (which I opposed)...' retroactive opposition",
        "Start with: 'Regulatory changes blindsided us...' blaming government",
        "Start with: 'This is exactly why I've been pushing for...' pivot to your agenda"
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
        // Debug: Log the outcome tier being used for prompt generation
        System.Diagnostics.Debug.WriteLine($"[LlmEmailService] BuildCardPrompt: cardTitle={cardTitle}, outcomeTier={outcomeTier}");

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
                $"""
                TRIUMPHANT & SELF-CONGRATULATORY tone. You are taking credit for everything.
                {GoodOpeners[_flavorRng.Next(GoodOpeners.Length)]}
                Be smug. Be self-aggrandizing. Imply others just followed your genius plan.
                NEVER add notes, commentary, or parenthetical explanations. Stay in character.
                """,
                "You write TRIUMPHANT corporate emails. You take full credit for successes. Body only. No meta-commentary. No notes. Stay in character."
            ),
            "Bad" => (
                $"""
                YOU ARE AVOIDING RESPONSIBILITY FOR A DISASTER. This is a CYA (cover your ass) email.
                {BadOpeners[_flavorRng.Next(BadOpeners.Length)]}
                NEVER admit fault. NEVER say "we failed" or "I should have". You saw this coming. You tried to stop it. You're the victim here.
                NEVER add notes, commentary, or parenthetical explanations about what you're doing. Stay in character.
                """,
                "You write CYA emails after a disaster. You NEVER accept blame. Body only. No meta-commentary. No notes. No explanations. Stay in character."
            ),
            _ => (
                $"""
                CORPORATE SPIN tone. Bury the bad news in positive framing.
                {ExpectedOpeners[_flavorRng.Next(ExpectedOpeners.Length)]}
                Minimize negatives, exaggerate positives. Classic corporate doublespeak.
                NEVER add notes, commentary, or parenthetical explanations. Stay in character.
                """,
                "You write SPIN-DOCTOR corporate emails. Hide bad news in positive framing. Body only. No meta-commentary. No notes. Stay in character."
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
            Body only. No greeting/signature. No notes or commentary. Just the email text.
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
            Body only. No notes or commentary. Just the email text.
            """;

        var systemPrompt = "You're a middle manager at InertiCorp replying to the CEO. Office Space humor. Body only. No meta-commentary. No notes. Stay in character.";

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
                    $"""
                    TRIUMPHANT & SELF-CONGRATULATORY. You saved the day and everyone should know it.
                    {GoodOpeners[_flavorRng.Next(GoodOpeners.Length)]}
                    Take ALL the credit. Be smug about your crisis management skills.
                    NEVER add notes, commentary, or explanations. Stay in character.
                    """,
                    "You write TRIUMPHANT crisis emails. You're the hero. Body only. No meta-commentary. Stay in character."
                ),
                "Bad" => (
                    $"""
                    BLAME-SHIFTING & DEFENSIVE. This was NOT your fault and you need to make that clear.
                    {BadOpeners[_flavorRng.Next(BadOpeners.Length)]}
                    NEVER accept any blame. Be passive-aggressive. Imply others caused this mess.
                    NEVER add notes, commentary, or explanations. Stay in character.
                    """,
                    "You write DEFENSIVE blame-shifting emails. NEVER accept fault. Body only. No meta-commentary. Stay in character."
                ),
                _ => (
                    $"""
                    CORPORATE SPIN. The crisis is 'contained' and there are 'learnings'.
                    {ExpectedOpeners[_flavorRng.Next(ExpectedOpeners.Length)]}
                    Make it sound like a growth opportunity, not a disaster.
                    NEVER add notes, commentary, or explanations. Stay in character.
                    """,
                    "You write SPIN-DOCTOR crisis emails. Frame disasters as learning opportunities. Body only. No meta-commentary. Stay in character."
                )
            };

            userPrompt = $"""
                Write a SHORT satirical email (2-3 sentences).

                {senderContext}
                Crisis "{crisisTitle}" resolved via "{choiceLabel ?? "action"}".{effectsLine}

                {toneGuidance}
                Humor style: {flavor}
                Body only. No notes or commentary. Just the email text.
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
                Body only. No notes or commentary. Just the email text.
                """;
        }

        var systemPrompt = "You write Dilbert-style crisis emails for InertiCorp. Panicked but corporate. Body only. No meta-commentary. No notes. Stay in character.";

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

        // Strip meta-commentary that LLMs sometimes add despite instructions
        // Pattern: "(Note: ...)" or "(This response ...)" at the end
        cleaned = StripMetaCommentary(cleaned);

        return cleaned;
    }

    private static string StripMetaCommentary(string text)
    {
        // Remove trailing parenthetical notes like "(Note: this avoids...)"
        // Look for opening paren in last 40% of text that contains meta-words
        var metaPatterns = new[] { "(note", "(this response", "(the response", "(i've", "(i have", "(this email", "(the email", "(this avoids", "(avoiding" };

        foreach (var pattern in metaPatterns)
        {
            var idx = text.LastIndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx > 0 && idx > text.Length * 0.5)
            {
                // Found a meta-comment pattern - strip from there to end
                text = text[..idx].TrimEnd();
                break;
            }
        }

        // Also strip standalone "Note:" lines
        var lines = text.Split('\n');
        var cleanLines = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            // Skip lines that start with meta-commentary indicators
            if (trimmed.StartsWith("Note:", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("(Note", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("---", StringComparison.Ordinal) ||
                trimmed.StartsWith("*Note", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip this line and all remaining (they're usually trailing)
            }
            cleanLines.Add(line);
        }

        return string.Join('\n', cleanLines).TrimEnd();
    }

    public void Dispose()
    {
        _sharedContext?.Dispose();
        _sharedContext = null;
        _model?.Dispose();
    }
}
