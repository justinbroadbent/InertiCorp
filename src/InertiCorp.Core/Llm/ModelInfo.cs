namespace InertiCorp.Core.Llm;

/// <summary>
/// Performance tier for model selection guidance.
/// </summary>
public enum ModelTier
{
    /// <summary>Fastest inference, lower quality output.</summary>
    Fast,

    /// <summary>Good balance of speed and quality.</summary>
    Balanced,

    /// <summary>Best quality, slower inference.</summary>
    Quality
}

/// <summary>
/// Information about an available LLM model.
/// </summary>
public sealed record ModelInfo(
    string Id,
    string Name,
    string FileName,
    string Description,
    long SizeBytes,
    string DownloadUrl,
    string Sha256Hash,
    ModelTier Tier,
    string PromptFormat,
    string[] StopTokens)
{
    /// <summary>
    /// Human-readable size (e.g., "2.3 GB").
    /// </summary>
    public string SizeDisplay => SizeBytes switch
    {
        >= 1_000_000_000 => $"{SizeBytes / 1_000_000_000.0:F1} GB",
        >= 1_000_000 => $"{SizeBytes / 1_000_000.0:F0} MB",
        _ => $"{SizeBytes / 1_000.0:F0} KB"
    };

    /// <summary>
    /// Tier description for UI.
    /// </summary>
    public string TierDisplay => Tier switch
    {
        ModelTier.Fast => "⚡ Fastest",
        ModelTier.Balanced => "⚖️ Balanced",
        ModelTier.Quality => "✨ Best Quality",
        _ => ""
    };
}

/// <summary>
/// Download progress information.
/// </summary>
public sealed record DownloadProgress(
    long BytesDownloaded,
    long TotalBytes,
    double? BytesPerSecond = null)
{
    public double Percentage => TotalBytes > 0
        ? (double)BytesDownloaded / TotalBytes * 100
        : 0;

    public string ProgressDisplay => $"{BytesDownloaded / 1_000_000.0:F0} / {TotalBytes / 1_000_000.0:F0} MB";

    public string? SpeedDisplay => BytesPerSecond.HasValue
        ? $"{BytesPerSecond.Value / 1_000_000.0:F1} MB/s"
        : null;

    public TimeSpan? EstimatedTimeRemaining => BytesPerSecond.HasValue && BytesPerSecond.Value > 0
        ? TimeSpan.FromSeconds((TotalBytes - BytesDownloaded) / BytesPerSecond.Value)
        : null;
}

/// <summary>
/// Status of a model on the local system.
/// </summary>
public enum ModelStatus
{
    NotDownloaded,
    Downloading,
    Downloaded,
    Active
}

/// <summary>
/// Model info combined with local status.
/// </summary>
public sealed record ModelViewModel(
    ModelInfo Model,
    ModelStatus Status,
    DownloadProgress? CurrentDownload = null)
{
    public bool CanDownload => Status == ModelStatus.NotDownloaded;
    public bool CanDelete => Status is ModelStatus.Downloaded or ModelStatus.Active;
    public bool CanActivate => Status == ModelStatus.Downloaded;
    public bool IsActive => Status == ModelStatus.Active;
}
