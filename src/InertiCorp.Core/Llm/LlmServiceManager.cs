namespace InertiCorp.Core.Llm;

/// <summary>
/// Manages the LLM service lifecycle for the game.
/// Provides a singleton access point for LLM email generation.
/// </summary>
public static class LlmServiceManager
{
    private static ModelManager? _modelManager;
    private static LlmEmailService? _emailService;
    private static bool _initialized;

    /// <summary>
    /// Whether the LLM service is available and ready for use.
    /// </summary>
    public static bool IsReady => _emailService?.IsReady ?? false;

    /// <summary>
    /// Whether the LLM service is currently loading a model.
    /// </summary>
    public static bool IsLoading => _emailService?.IsLoading ?? false;

    /// <summary>
    /// The name of the currently loaded model, if any.
    /// </summary>
    public static string? LoadedModelName => _emailService?.LoadedModelName;

    /// <summary>
    /// Event raised when the LLM is ready for generation.
    /// </summary>
    public static event Action? Ready;

    /// <summary>
    /// Event raised when loading fails.
    /// </summary>
    public static event Action<Exception>? LoadFailed;

    /// <summary>
    /// Initializes the LLM service manager.
    /// Call this during game startup.
    /// </summary>
    public static void Initialize(string? modelsDirectory = null)
    {
        if (_initialized) return;

        // Initialize diagnostics FIRST - must be before any LLamaSharp calls
        LlmDiagnostics.Initialize();

        _modelManager = new ModelManager(modelsDirectory);
        _emailService = new LlmEmailService(_modelManager);

        _emailService.Ready += () => Ready?.Invoke();
        _emailService.LoadFailed += ex => LoadFailed?.Invoke(ex);

        _initialized = true;
    }

    /// <summary>
    /// Loads the active model if one is configured.
    /// Call this after initialization and after settings are loaded.
    /// </summary>
    public static async Task LoadActiveModelAsync(CancellationToken ct = default)
    {
        if (!_initialized || _emailService == null) return;

        if (_modelManager?.ActiveModelId == null)
        {
            // No model configured
            return;
        }

        try
        {
            await _emailService.LoadModelAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // No model to load - this is fine
        }
    }

    /// <summary>
    /// Gets the LLM email service instance.
    /// Returns null if not initialized.
    /// </summary>
    public static LlmEmailService? GetEmailService() => _emailService;

    /// <summary>
    /// Gets the model manager instance.
    /// Returns null if not initialized.
    /// </summary>
    public static ModelManager? GetModelManager() => _modelManager;

    /// <summary>
    /// Shuts down the LLM service and releases resources.
    /// </summary>
    public static void Shutdown()
    {
        _emailService?.Dispose();
        _emailService = null;

        _modelManager?.Dispose();
        _modelManager = null;

        _initialized = false;
    }

    /// <summary>
    /// Reloads the model if the active model changed.
    /// </summary>
    public static async Task ReloadModelAsync(CancellationToken ct = default)
    {
        if (!_initialized || _emailService == null) return;

        _emailService.UnloadModel();
        await LoadActiveModelAsync(ct);
    }

    /// <summary>
    /// Whether the LLM is currently generating content.
    /// </summary>
    public static bool IsGenerating => _emailService?.IsGenerating ?? false;
}
