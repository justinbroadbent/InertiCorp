namespace InertiCorp.Core.Llm;

/// <summary>
/// Catalog of available LLM models for email generation.
/// </summary>
public static class ModelCatalog
{
    /// <summary>
    /// The default model ID to use for first-time setup.
    /// </summary>
    public const string DefaultModelId = "phi3-mini";

    /// <summary>
    /// Prompt format for Phi-3 models.
    /// </summary>
    private const string Phi3Format = "<|user|>\n{0}<|end|>\n<|assistant|>\n";

    /// <summary>
    /// Prompt format for Qwen models.
    /// </summary>
    private const string QwenFormat = "<|im_start|>system\n{1}<|im_end|>\n<|im_start|>user\n{0}<|im_end|>\n<|im_start|>assistant\n";

    /// <summary>
    /// Prompt format for Llama/Mistral models.
    /// </summary>
    private const string LlamaFormat = "[INST] {0} [/INST]";

    /// <summary>
    /// All available models.
    /// </summary>
    public static IReadOnlyList<ModelInfo> Models { get; } = new[]
    {
        new ModelInfo(
            Id: "phi3-mini",
            Name: "Phi-3 Mini",
            FileName: "phi-3-mini-4k-instruct-q4.gguf",
            Description: "Microsoft's compact model. Good humor, reliable formatting.",
            SizeBytes: 2_300_000_000,
            DownloadUrl: "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf",
            Sha256Hash: "", // TODO: Compute and add
            Tier: ModelTier.Balanced,
            PromptFormat: Phi3Format,
            StopTokens: new[] { "<|end|>", "<|user|>", "<|assistant|>" }
        ),

        new ModelInfo(
            Id: "qwen25-3b",
            Name: "Qwen 2.5 3B",
            FileName: "qwen2.5-3b-instruct-q4_k_m.gguf",
            Description: "Fast inference, good at following instructions.",
            SizeBytes: 2_000_000_000,
            DownloadUrl: "https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf",
            Sha256Hash: "", // TODO: Compute and add
            Tier: ModelTier.Fast,
            PromptFormat: QwenFormat,
            StopTokens: new[] { "<|im_end|>", "<|im_start|>" }
        ),

        new ModelInfo(
            Id: "tinyllama",
            Name: "TinyLlama 1.1B",
            FileName: "tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf",
            Description: "Smallest and fastest. For older hardware. Lower quality.",
            SizeBytes: 670_000_000,
            DownloadUrl: "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf",
            Sha256Hash: "", // TODO: Compute and add
            Tier: ModelTier.Fast,
            PromptFormat: LlamaFormat,
            StopTokens: new[] { "</s>", "[INST]" }
        ),

        new ModelInfo(
            Id: "mistral-7b",
            Name: "Mistral 7B",
            FileName: "mistral-7b-instruct-v0.2.Q4_K_M.gguf",
            Description: "Highest quality output. Requires more RAM and GPU.",
            SizeBytes: 4_370_000_000,
            DownloadUrl: "https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/resolve/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf",
            Sha256Hash: "", // TODO: Compute and add
            Tier: ModelTier.Quality,
            PromptFormat: LlamaFormat,
            StopTokens: new[] { "</s>", "[INST]" }
        ),
    };

    /// <summary>
    /// Get a model by ID.
    /// </summary>
    public static ModelInfo? GetById(string id)
        => Models.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Get the default model.
    /// </summary>
    public static ModelInfo Default => GetById(DefaultModelId)!;

    /// <summary>
    /// Get models by tier.
    /// </summary>
    public static IEnumerable<ModelInfo> GetByTier(ModelTier tier)
        => Models.Where(m => m.Tier == tier);
}
