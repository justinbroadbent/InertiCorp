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

    private LLamaWeights? _model;
    private ModelParams? _modelParams;
    private ModelInfo? _activeModelInfo;
    private LLamaContext? _sharedContext;  // Reusable context for faster inference

    private const int MaxTokens = 190;  // 2-3 sentence emails, with buffer for completion

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

            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = gpuLayers,
                BatchSize = 128,  // Smaller batches reduce CPU spikes (was 512)
                Threads = 2,      // Leave cores for audio subsystem (was ProcessorCount / 2)
            };

            _model = await LLamaWeights.LoadFromFileAsync(_modelParams, ct);
            _activeModelInfo = modelInfo;

            // Create a shared context for faster inference (avoids recreating each time)
            _sharedContext = _model.CreateContext(_modelParams);

            Ready?.Invoke();
        }
        catch (Exception ex)
        {
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
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildCardPrompt(cardTitle, cardDescription, outcomeTier, profitImpact, meterEffects);
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

    private string BuildCardPrompt(string cardTitle, string cardDescription, string outcomeTier, string? profitImpact = null, string? meterEffects = null)
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

        // Concise prompt for faster generation - 1 paragraph
        var userPrompt = $"""
            Write a SHORT satirical corporate email (2-3 sentences, one paragraph).

            Project: {cardTitle} - {cardDescription}
            Outcome: {outcomeTier}
            {(resultsContext.Length > 0 ? $"Results: {resultsContext}" : "")}

            Be passive-aggressive, use buzzwords. Body only, no greeting/signature.
            """;

        var systemPrompt = "You write brief, sarcastic corporate emails. 2-3 sentences max. Passive-aggressive humor. Body only.";

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
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildCrisisPrompt(crisisTitle, crisisDescription, isResolution, choiceLabel, outcomeTier, effects);
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
        CancellationToken ct = default)
    {
        if (!IsReady || _model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildFreeformPrompt(subject, ceoMessage, recipient);
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

    private string BuildFreeformPrompt(string subject, string ceoMessage, string recipient)
    {
        // Concise prompt for faster generation
        var userPrompt = $"""
            CEO emailed {recipient} about "{subject}": "{ceoMessage}"

            Write a SHORT reply (2-3 sentences). Respond directly but with corporate spin. Body only.
            """;

        var systemPrompt = "You're a passive-aggressive manager. 2-3 sentences max. Body only.";

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }

    private string BuildCrisisPrompt(string crisisTitle, string crisisDescription, bool isResolution, string? choiceLabel, string? outcomeTier, string? effects = null)
    {
        // Concise prompts for faster generation
        string userPrompt;

        if (isResolution)
        {
            var effectsLine = !string.IsNullOrEmpty(effects) ? $" Result: {effects}" : "";
            var outcomeDesc = outcomeTier switch
            {
                "Good" => "success",
                "Bad" => "problems",
                _ => "okay"
            };

            userPrompt = $"""
                Write a SHORT satirical email (2-3 sentences).

                Crisis "{crisisTitle} - {crisisDescription}" resolved via "{choiceLabel ?? "action"}". Outcome: {outcomeDesc}.{effectsLine}

                Sound relieved but corporate. Body only.
                """;
        }
        else
        {
            userPrompt = $"""
                Write a SHORT URGENT satirical email (2-3 sentences).

                CRISIS: {crisisTitle} - {crisisDescription}

                Sound panicked, use buzzwords. Body only.
                """;
        }

        var systemPrompt = "You write brief panicked corporate emails. 2-3 sentences. Body only.";

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
