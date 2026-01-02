using System.Diagnostics;
using System.Text;
using LLama;
using LLama.Common;

// Get model from command line args or default to Phi3
var modelName = args.Length > 0 ? args[0] : "phi3";
var modelPath = modelName.ToLowerInvariant() switch
{
    "qwen" => @"C:\src\InertiCorp\tools\LlmTest\models\qwen2.5-3b-instruct-q4_k_m.gguf",
    _ => @"C:\src\InertiCorp\tools\LlmTest\models\phi-3-mini-4k-instruct-q4_k_m.gguf"
};

Console.WriteLine($"=== InertiCorp Email Generation Test ({modelName.ToUpperInvariant()}) ===\n");
Console.WriteLine($"Loading model from: {Path.GetFileName(modelPath)}");

var totalSw = Stopwatch.StartNew();

var parameters = new ModelParams(modelPath)
{
    ContextSize = 768,  // Match the game's optimized params
    GpuLayerCount = 40,
};

using var model = await LLamaWeights.LoadFromFileAsync(parameters);

Console.WriteLine($"Model loaded in {totalSw.Elapsed.TotalSeconds:F1}s\n");

// Prompt format for Phi-3
const string Phi3Format = "<|user|>\n{0}<|end|>\n<|assistant|>\n";
var stopTokens = new[] { "<|end|>", "<|user|>", "<|assistant|>" };

// Test with ACTUAL card data from the game
var testCases = new[]
{
    new {
        Title = "AI Center of Excellence",
        Description = "Create a dedicated AI research team to explore emerging technologies.",
        Outcome = "Expected",
        Profit = "+$2M profit",
        Effects = "Delivery: +10, Alignment: +5"
    },
    new {
        Title = "Global Operating Model",
        Description = "Restructure operations across all regions for maximum efficiency.",
        Outcome = "Bad",
        Profit = "-$5M profit",
        Effects = "Delivery: -15, Morale: -10, Runway: -5"
    },
    new {
        Title = "Hackathon Week",
        Description = "Let engineers spend a week building whatever they want.",
        Outcome = "Good",
        Profit = "+$1M profit",
        Effects = "Morale: +15, Delivery: +5"
    },
};

Console.WriteLine("Testing with exact prompts from BackgroundEmailProcessor...\n");
Console.WriteLine("=".PadRight(60, '=') + "\n");

var results = new List<(string Title, double Seconds, int Chars)>();

foreach (var test in testCases)
{
    Console.WriteLine($"PROJECT: {test.Title}");
    Console.WriteLine($"Outcome: {test.Outcome}, Effects: {test.Effects}");
    Console.WriteLine();

    // Build the EXACT prompt used by LlmEmailService.BuildCardPrompt
    var resultsContext = $"Financial impact: {test.Profit}. Organization effects: {test.Effects}. ";

    var userPrompt = $"""
        Write a DETAILED satirical corporate email (MINIMUM 5-7 sentences, 100+ words).

        Project: {test.Title} - {test.Description}
        Outcome: {test.Outcome}
        Results: {resultsContext}

        Requirements:
        - Write at least 5 full sentences
        - Be passive-aggressive and darkly funny
        - Reference the actual results with specific metrics
        - Include corporate buzzwords and jargon
        - Write the complete email body only (no greeting, no signature)
        """;

    var prompt = string.Format(Phi3Format, userPrompt);

    var inferenceParams = new InferenceParams
    {
        MaxTokens = 300,
        AntiPrompts = stopTokens,
        SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
        {
            Temperature = 0.75f,
            TopP = 0.88f,
        },
    };

    var sw = Stopwatch.StartNew();
    var result = new StringBuilder();

    var executor = new StatelessExecutor(model, parameters);
    await foreach (var token in executor.InferAsync(prompt, inferenceParams))
    {
        result.Append(token);
    }

    sw.Stop();
    var rawOutput = result.ToString().Trim();

    // Apply the same cleaning as CleanEmailBody
    var cleaned = CleanEmailBody(rawOutput);

    Console.WriteLine($"--- RAW OUTPUT ({rawOutput.Length} chars) ---");
    Console.WriteLine(rawOutput);
    Console.WriteLine();
    Console.WriteLine($"--- CLEANED ({cleaned.Length} chars) ---");
    Console.WriteLine(cleaned);
    Console.WriteLine();
    Console.WriteLine($"Generation time: {sw.Elapsed.TotalSeconds:F2}s");
    Console.WriteLine($"Meets minimum (100 chars): {(cleaned.Length >= 100 ? "YES" : "NO")}");
    Console.WriteLine();
    Console.WriteLine("=".PadRight(60, '=') + "\n");

    results.Add((test.Title, sw.Elapsed.TotalSeconds, cleaned.Length));
}

// Summary
Console.WriteLine("\n=== SUMMARY ===\n");
Console.WriteLine($"{"Project",-35} {"Time",-10} {"Chars",-10} {"OK?"}");
Console.WriteLine("-".PadRight(60, '-'));
foreach (var (title, seconds, chars) in results)
{
    var ok = chars >= 100 ? "YES" : "NO";
    Console.WriteLine($"{title,-35} {seconds,6:F2}s    {chars,6}     {ok}");
}

Console.WriteLine();
Console.WriteLine($"Average generation time: {results.Average(r => r.Seconds):F2}s");
Console.WriteLine($"Average content length: {results.Average(r => r.Chars):F0} chars");
Console.WriteLine($"Pass rate (>= 100 chars): {results.Count(r => r.Chars >= 100)}/{results.Count}");

if (results.Any(r => r.Chars < 100))
{
    Console.WriteLine("\nWARNING: Some outputs are too short! The fallback template will be used.");
}

if (results.Any(r => r.Seconds > 30))
{
    Console.WriteLine("\nWARNING: Some generations exceeded 30s timeout!");
}

// --- Helper ---
static string CleanEmailBody(string raw)
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
    var signatureMarkers = new[] { "\nBest,", "\nRegards,", "\nSincerely,", "\n--", "\nBest regards,", "\nThank you," };
    foreach (var marker in signatureMarkers)
    {
        var idx = cleaned.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx > 0 && idx > cleaned.Length * 0.7)
        {
            cleaned = cleaned[..idx].Trim();
            break;
        }
    }

    return cleaned;
}
