using System.Security.Cryptography;

namespace InertiCorp.Core.Llm;

/// <summary>
/// Manages LLM model downloads, storage, and activation.
/// </summary>
public sealed class ModelManager : IDisposable
{
    private readonly string _modelsDirectory;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();
    private string? _activeModelId;

    /// <summary>
    /// Event raised when a model's status changes.
    /// </summary>
    public event Action<string>? ModelStatusChanged;

    /// <summary>
    /// Event raised when the active model changes.
    /// </summary>
    public event Action<string?>? ActiveModelChanged;

    public ModelManager(string? modelsDirectory = null)
    {
        _modelsDirectory = modelsDirectory ?? GetDefaultModelsDirectory();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InertiCorp/1.0");

        Directory.CreateDirectory(_modelsDirectory);
        LoadActiveModelSetting();
    }

    /// <summary>
    /// Gets the default models directory in user's local app data.
    /// </summary>
    public static string GetDefaultModelsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "InertiCorp", "models");
    }

    /// <summary>
    /// Gets the full path for a model file.
    /// </summary>
    public string GetModelPath(string modelId)
    {
        var model = ModelCatalog.GetById(modelId);
        if (model == null) throw new ArgumentException($"Unknown model: {modelId}");
        return Path.Combine(_modelsDirectory, model.FileName);
    }

    /// <summary>
    /// Gets the path for a model's in-progress download.
    /// </summary>
    private string GetDownloadPath(string modelId) => GetModelPath(modelId) + ".downloading";

    /// <summary>
    /// Gets the path for storing download progress (for resume).
    /// </summary>
    private string GetProgressPath(string modelId) => GetModelPath(modelId) + ".progress";

    /// <summary>
    /// Checks if a model is downloaded.
    /// </summary>
    public bool IsDownloaded(string modelId)
    {
        var path = GetModelPath(modelId);
        return File.Exists(path);
    }

    /// <summary>
    /// Checks if a model is currently downloading.
    /// </summary>
    public bool IsDownloading(string modelId) => _activeDownloads.ContainsKey(modelId);

    /// <summary>
    /// Gets the currently active model ID.
    /// </summary>
    public string? ActiveModelId => _activeModelId;

    /// <summary>
    /// Gets the path to the active model, or null if none.
    /// </summary>
    public string? GetActiveModelPath()
    {
        if (_activeModelId == null) return null;
        var path = GetModelPath(_activeModelId);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Gets the status of a model.
    /// </summary>
    public ModelStatus GetStatus(string modelId)
    {
        if (_activeModelId == modelId && IsDownloaded(modelId))
            return ModelStatus.Active;
        if (IsDownloading(modelId))
            return ModelStatus.Downloading;
        if (IsDownloaded(modelId))
            return ModelStatus.Downloaded;
        return ModelStatus.NotDownloaded;
    }

    /// <summary>
    /// Gets view models for all available models.
    /// </summary>
    public IEnumerable<ModelViewModel> GetAllModels()
    {
        return ModelCatalog.Models.Select(m => new ModelViewModel(m, GetStatus(m.Id)));
    }

    /// <summary>
    /// Sets the active model.
    /// </summary>
    public void SetActiveModel(string? modelId)
    {
        if (modelId != null && !IsDownloaded(modelId))
            throw new InvalidOperationException($"Model {modelId} is not downloaded");

        _activeModelId = modelId;
        SaveActiveModelSetting();
        ActiveModelChanged?.Invoke(modelId);
    }

    /// <summary>
    /// Downloads a model with progress reporting.
    /// </summary>
    public async Task DownloadAsync(
        string modelId,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var model = ModelCatalog.GetById(modelId)
            ?? throw new ArgumentException($"Unknown model: {modelId}");

        if (IsDownloaded(modelId))
            throw new InvalidOperationException($"Model {modelId} is already downloaded");

        if (IsDownloading(modelId))
            throw new InvalidOperationException($"Model {modelId} is already downloading");

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeDownloads[modelId] = cts;
        ModelStatusChanged?.Invoke(modelId);

        var downloadPath = GetDownloadPath(modelId);
        var finalPath = GetModelPath(modelId);

        try
        {
            // Check for existing partial download
            long existingBytes = 0;
            if (File.Exists(downloadPath))
            {
                existingBytes = new FileInfo(downloadPath).Length;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, model.DownloadUrl);

            // Resume from where we left off
            if (existingBytes > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? model.SizeBytes;

            // If we're resuming, add existing bytes to total tracking
            var isResuming = response.StatusCode == System.Net.HttpStatusCode.PartialContent;
            if (isResuming)
            {
                totalBytes += existingBytes;
            }
            else
            {
                // Server doesn't support resume, start fresh
                existingBytes = 0;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cts.Token);
            await using var fileStream = new FileStream(
                downloadPath,
                isResuming ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var buffer = new byte[81920];
            long downloadedBytes = existingBytes;
            var lastProgressTime = DateTime.UtcNow;
            long lastProgressBytes = existingBytes;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cts.Token);
                downloadedBytes += bytesRead;

                // Report progress every 100ms
                var now = DateTime.UtcNow;
                if ((now - lastProgressTime).TotalMilliseconds >= 100)
                {
                    var elapsed = (now - lastProgressTime).TotalSeconds;
                    var bytesPerSecond = (downloadedBytes - lastProgressBytes) / elapsed;

                    progress?.Report(new DownloadProgress(downloadedBytes, totalBytes, bytesPerSecond));

                    lastProgressTime = now;
                    lastProgressBytes = downloadedBytes;
                }
            }

            // Final progress report
            progress?.Report(new DownloadProgress(downloadedBytes, totalBytes));

            // Close file before verification
            await fileStream.FlushAsync(cts.Token);
            fileStream.Close();

            // Verify hash if provided
            if (!string.IsNullOrEmpty(model.Sha256Hash))
            {
                var actualHash = await ComputeHashAsync(downloadPath, cts.Token);
                if (!actualHash.Equals(model.Sha256Hash, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(downloadPath);
                    throw new InvalidOperationException(
                        $"Download corrupted: hash mismatch. Expected {model.Sha256Hash}, got {actualHash}");
                }
            }

            // Move to final location
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(downloadPath, finalPath);

            // Clean up progress file if exists
            var progressPath = GetProgressPath(modelId);
            if (File.Exists(progressPath))
                File.Delete(progressPath);
        }
        catch (OperationCanceledException)
        {
            // Download cancelled - leave partial file for resume
            throw;
        }
        catch
        {
            // Other error - clean up partial download
            if (File.Exists(downloadPath))
                File.Delete(downloadPath);
            throw;
        }
        finally
        {
            _activeDownloads.Remove(modelId);
            ModelStatusChanged?.Invoke(modelId);
        }
    }

    /// <summary>
    /// Cancels an in-progress download.
    /// </summary>
    public void CancelDownload(string modelId)
    {
        if (_activeDownloads.TryGetValue(modelId, out var cts))
        {
            cts.Cancel();
        }
    }

    /// <summary>
    /// Deletes a downloaded model.
    /// </summary>
    public void Delete(string modelId)
    {
        if (IsDownloading(modelId))
            throw new InvalidOperationException($"Cannot delete {modelId} while downloading");

        var path = GetModelPath(modelId);
        if (File.Exists(path))
            File.Delete(path);

        // Clear active model if this was it
        if (_activeModelId == modelId)
        {
            _activeModelId = null;
            SaveActiveModelSetting();
            ActiveModelChanged?.Invoke(null);
        }

        ModelStatusChanged?.Invoke(modelId);
    }

    /// <summary>
    /// Gets the total size of downloaded models.
    /// </summary>
    public long GetTotalDownloadedSize()
    {
        return ModelCatalog.Models
            .Where(m => IsDownloaded(m.Id))
            .Sum(m => m.SizeBytes);
    }

    private static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private void LoadActiveModelSetting()
    {
        var settingsPath = Path.Combine(_modelsDirectory, "active_model.txt");
        if (File.Exists(settingsPath))
        {
            var modelId = File.ReadAllText(settingsPath).Trim();
            if (IsDownloaded(modelId))
            {
                _activeModelId = modelId;
            }
        }
    }

    private void SaveActiveModelSetting()
    {
        var settingsPath = Path.Combine(_modelsDirectory, "active_model.txt");
        if (_activeModelId != null)
        {
            File.WriteAllText(settingsPath, _activeModelId);
        }
        else if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    public void Dispose()
    {
        foreach (var cts in _activeDownloads.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _activeDownloads.Clear();
        _httpClient.Dispose();
    }
}
