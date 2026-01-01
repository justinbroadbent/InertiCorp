using System.Collections.Concurrent;
using System.Text;
using LLama;
using LLama.Common;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Llm;

/// <summary>
/// Service for generating dynamic emails using a local LLM.
/// </summary>
public sealed class LlmEmailService : IDisposable
{
    private readonly ModelManager _modelManager;
    private readonly ConcurrentQueue<GeneratedEmail> _emailQueue = new();
    private readonly ConcurrentDictionary<string, GeneratedEmail> _cache = new();
    private readonly HashSet<string> _usedEmailHashes = new();

    private LLamaWeights? _model;
    private ModelParams? _modelParams;
    private ModelInfo? _activeModelInfo;
    private LLamaContext? _sharedContext;  // Reusable context for faster inference
    private CancellationTokenSource? _backgroundCts;
    private Task? _backgroundTask;

    private const int MaxQueueSize = 20;
    private const int MaxCacheSize = 100;
    private const int MaxTokens = 300;  // Tripled to avoid truncation

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
    /// </summary>
    public bool IsGenerating => !_generationQueue.IsEmpty || _isActivelyGenerating;
    private volatile bool _isActivelyGenerating;

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
                BatchSize = 512,  // Process more tokens at once
                Threads = Math.Max(4, Environment.ProcessorCount / 2),  // Use half of CPU cores
            };

            _model = await LLamaWeights.LoadFromFileAsync(_modelParams, ct);
            _activeModelInfo = modelInfo;

            // Create a shared context for faster inference (avoids recreating each time)
            _sharedContext = _model.CreateContext(_modelParams);

            // Start background generation
            StartBackgroundGeneration();

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
        StopBackgroundGeneration();
        _sharedContext?.Dispose();
        _sharedContext = null;
        _model?.Dispose();
        _model = null;
        _activeModelInfo = null;
        _emailQueue.Clear();
        _cache.Clear();
    }

    /// <summary>
    /// Generates an email for a situation. Returns cached/queued email if available,
    /// or null if generation is needed (use fallback template).
    /// </summary>
    public GeneratedEmail? GetSituationEmail(
        SituationDefinition situation,
        OutcomeTier outcome,
        string senderName,
        string senderTitle)
    {
        if (!IsReady) return null;

        var cacheKey = $"{situation.SituationId}_{outcome}_{senderName}";

        // Try cache first
        if (_cache.TryRemove(cacheKey, out var cached))
        {
            if (!_usedEmailHashes.Contains(cached.ContentHash))
            {
                _usedEmailHashes.Add(cached.ContentHash);
                return cached;
            }
        }

        // Try queue
        if (_emailQueue.TryDequeue(out var queued))
        {
            if (!_usedEmailHashes.Contains(queued.ContentHash))
            {
                _usedEmailHashes.Add(queued.ContentHash);
                return queued;
            }
        }

        // Nothing available - caller should use template fallback
        // Queue a background generation for next time
        QueueGeneration(situation, outcome, senderName, senderTitle);
        return null;
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
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
            {
                result.Append(token);
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

        // Enhanced prompt with project results for more contextual responses
        var userPrompt = $"""
            Write a satirical 50-70 word corporate email about project results.
            Project: {cardTitle} - {cardDescription}
            Outcome: {outcomeTier}
            {(resultsContext.Length > 0 ? $"Results: {resultsContext}" : "")}
            Style: Passive-aggressive, darkly funny. Reference the actual results if provided. Body only, no greeting/signature.
            """;

        var systemPrompt = "Satirical corporate email writer. Passive-aggressive, darkly funny. Reference specific financial/organizational impacts when available. Output body only.";

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
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
            {
                result.Append(token);
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
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
            {
                result.Append(token);
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
        // Prompt that actually responds to what the CEO said
        var userPrompt = $"""
            The CEO sent this email to {recipient}:
            Subject: "{subject}"
            Message: "{ceoMessage}"

            Write a 60-80 word reply that DIRECTLY RESPONDS to what the CEO said.
            If they asked a question, answer it (with corporate spin).
            If they made a statement, acknowledge and build on it.
            Stay in character as a passive-aggressive middle manager.
            Include one made-up metric. Use corporate buzzwords.
            Write ONLY the reply body, no greeting or signature.
            """;

        var systemPrompt = "You are a passive-aggressive middle manager replying to the CEO. Always directly address what they said. Use corporate speak and fake metrics.";

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }

    private string BuildCrisisPrompt(string crisisTitle, string crisisDescription, bool isResolution, string? choiceLabel, string? outcomeTier, string? effects = null)
    {
        // Concise prompts to reduce tokens
        string userPrompt;

        if (isResolution)
        {
            var effectsLine = !string.IsNullOrEmpty(effects)
                ? $"\nResult: {effects}"
                : "";

            var outcomeDesc = outcomeTier switch
            {
                "Good" => "went better than expected",
                "Bad" => "had complications",
                _ => "met expectations"
            };

            userPrompt = $"""
                Write a satirical 50-70 word corporate email about crisis resolution.
                Crisis: {crisisTitle} - {crisisDescription}
                Action taken: {choiceLabel ?? "handled it"}
                Outcome: {outcomeDesc}{effectsLine}
                Style: Corporate doublespeak, stressed manager relieved it's over. Reference the specific action and outcome. Body only.
                """;
        }
        else
        {
            userPrompt = $"""
                Write a satirical 50-70 word URGENT corporate email about a crisis.
                Crisis: {crisisTitle} - {crisisDescription}
                Style: Panicked manager, excessive urgency. Body only.
                """;
        }

        var systemPrompt = "Satirical corporate crisis writer. Panic through corporate speak. Body only.";

        return string.Format(_activeModelInfo!.PromptFormat, userPrompt, systemPrompt);
    }

    /// <summary>
    /// Pre-generates emails for likely upcoming situations.
    /// Call this when the game state changes (e.g., card played, quarter ended).
    /// </summary>
    public void PreGenerate(IEnumerable<(SituationDefinition Situation, OutcomeTier Outcome)> likelySituations)
    {
        // Queue generation for likely situations
        foreach (var (situation, outcome) in likelySituations)
        {
            // Use default sender for pre-generation
            QueueGeneration(situation, outcome, "Corporate Communications", "VP Communications");
        }
    }

    private void StartBackgroundGeneration()
    {
        _backgroundCts = new CancellationTokenSource();
        _backgroundTask = Task.Run(() => BackgroundGenerationLoop(_backgroundCts.Token));
    }

    private void StopBackgroundGeneration()
    {
        _backgroundCts?.Cancel();
        try
        {
            _backgroundTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        _backgroundCts?.Dispose();
        _backgroundCts = null;
        _backgroundTask = null;
    }

    private readonly ConcurrentQueue<GenerationRequest> _generationQueue = new();

    private void QueueGeneration(SituationDefinition situation, OutcomeTier outcome, string senderName, string senderTitle)
    {
        _generationQueue.Enqueue(new GenerationRequest(situation, outcome, senderName, senderTitle));
    }

    private async Task BackgroundGenerationLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Process queued requests
                if (_generationQueue.TryDequeue(out var request))
                {
                    _isActivelyGenerating = true;
                    try
                    {
                        var email = await GenerateEmailAsync(request, ct);
                        if (email != null)
                        {
                            var cacheKey = $"{request.Situation.SituationId}_{request.Outcome}_{request.SenderName}";
                            _cache.TryAdd(cacheKey, email);

                            // Limit cache size
                            while (_cache.Count > MaxCacheSize)
                            {
                                var firstKey = _cache.Keys.FirstOrDefault();
                                if (firstKey != null)
                                    _cache.TryRemove(firstKey, out _);
                            }
                        }
                    }
                    finally
                    {
                        _isActivelyGenerating = false;
                    }
                }

                // Keep queue filled with generic content
                if (_emailQueue.Count < MaxQueueSize / 2)
                {
                    // Generate a generic situation email
                    // This could be improved with actual game context
                }

                await Task.Delay(100, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Log error but continue
                await Task.Delay(1000, ct);
            }
        }
    }

    private async Task<GeneratedEmail?> GenerateEmailAsync(GenerationRequest request, CancellationToken ct)
    {
        if (_model == null || _activeModelInfo == null || _modelParams == null)
            return null;

        try
        {
            var prompt = BuildPrompt(request);
            var executor = new StatelessExecutor(_model, _modelParams);
            var inferenceParams = GetOptimizedInferenceParams(MaxTokens);

            var result = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, ct))
            {
                result.Append(token);
            }

            var body = CleanEmailBody(result.ToString());
            if (string.IsNullOrWhiteSpace(body))
                return null;

            return new GeneratedEmail(
                SituationId: request.Situation.SituationId,
                Outcome: request.Outcome,
                SenderName: request.SenderName,
                SenderTitle: request.SenderTitle,
                Body: body,
                ContentHash: ComputeHash(body));
        }
        catch
        {
            return null;
        }
    }

    private string BuildPrompt(GenerationRequest request)
    {
        // Concise prompt for faster background generation
        var userPrompt = $"""
            Write a satirical 50-70 word corporate email.
            Situation: {request.Situation.Title} - {request.Situation.Description}
            Outcome: {request.Outcome}
            Style: Passive-aggressive, darkly funny. Body only.
            """;

        var systemPrompt = "Satirical corporate email writer. Passive-aggressive, darkly funny. Body only.";

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

        // Remove signatures
        var signatureMarkers = new[] { "\nBest,", "\nRegards,", "\nSincerely,", "\n--", "\nBest regards," };
        foreach (var marker in signatureMarkers)
        {
            var idx = cleaned.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                cleaned = cleaned[..idx].Trim();
        }

        return cleaned;
    }

    private static string ComputeHash(string content)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes)[..16]; // First 16 chars is enough
    }

    public void Dispose()
    {
        StopBackgroundGeneration();
        _sharedContext?.Dispose();
        _sharedContext = null;
        _model?.Dispose();
    }

    private sealed record GenerationRequest(
        SituationDefinition Situation,
        OutcomeTier Outcome,
        string SenderName,
        string SenderTitle);
}

/// <summary>
/// A generated email ready for use.
/// </summary>
public sealed record GeneratedEmail(
    string SituationId,
    OutcomeTier Outcome,
    string SenderName,
    string SenderTitle,
    string Body,
    string ContentHash);
