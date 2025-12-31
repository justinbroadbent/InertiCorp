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
    private CancellationTokenSource? _backgroundCts;
    private Task? _backgroundTask;

    private const int MaxQueueSize = 20;
    private const int MaxCacheSize = 100;
    private const int MaxTokens = 150;

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

            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = 2048,
                GpuLayerCount = 35, // Offload to GPU if available
            };

            _model = await LLamaWeights.LoadFromFileAsync(_modelParams, ct);
            _activeModelInfo = modelInfo;

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
    /// Unloads the current model.
    /// </summary>
    public void UnloadModel()
    {
        StopBackgroundGeneration();
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
            using var context = _model.CreateContext(_modelParams);
            var executor = new StatelessExecutor(_model, _modelParams);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = MaxTokens,
                AntiPrompts = _activeModelInfo.StopTokens,
                SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
                {
                    Temperature = 0.8f,
                    TopP = 0.9f,
                },
            };

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
        var userPrompt = $"""
            You write satirical corporate emails for a dark comedy game. Write a short email (60-80 words) with dark humor and passive-aggressive corporate speak.

            Situation: {request.Situation.Title}
            Description: {request.Situation.Description}
            Outcome: {request.Outcome}
            From: {request.SenderName}, {request.SenderTitle}

            Write ONLY the email body addressed to "CEO". No subject line, no signature. Be satirical and darkly funny.
            """;

        var systemPrompt = "You write satirical corporate emails. Be darkly funny and use passive-aggressive corporate speak. Output only the email body, nothing else.";

        // Format according to model's expected format
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
