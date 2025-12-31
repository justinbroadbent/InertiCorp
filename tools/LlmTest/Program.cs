using System.Diagnostics;
using System.Text;
using LLama;
using LLama.Common;

// Get model from command line args or default to Qwen
var modelName = args.Length > 0 ? args[0] : "qwen";
var modelPath = modelName.ToLowerInvariant() switch
{
    "phi" or "phi3" => @"C:\src\InertiCorp\tools\LlmTest\models\phi-3-mini-4k-instruct-q4_k_m.gguf",
    _ => @"C:\src\InertiCorp\tools\LlmTest\models\qwen2.5-3b-instruct-q4_k_m.gguf"
};

Console.WriteLine($"=== InertiCorp Email Generation Test ({modelName.ToUpperInvariant()}) ===\n");
Console.WriteLine($"Loading model from: {Path.GetFileName(modelPath)}");

var sw = Stopwatch.StartNew();

var parameters = new ModelParams(modelPath)
{
    ContextSize = 2048,
    GpuLayerCount = 35,
};

using var model = await LLamaWeights.LoadFromFileAsync(parameters);
using var context = model.CreateContext(parameters);

Console.WriteLine($"Model loaded in {sw.Elapsed.TotalSeconds:F1}s\n");

// Test cases - using branching situation scenario (the hybrid approach)
var testCases = new[]
{
    new {
        Situation = "Key Performer Quits",
        TriggeringCard = "Global Operating Model",
        Context = "After the restructuring, your top engineer Sarah has handed in her resignation.",
        Outcome = "Bad",
        Effects = "Delivery -15, Morale -10"
    },
    new {
        Situation = "Glassdoor Firestorm",
        TriggeringCard = "Workplace of Tomorrow",
        Context = "Employees are posting scathing reviews about the hot-desking policy.",
        Outcome = "Expected",
        Effects = "Morale -5, Alignment -5"
    },
    new {
        Situation = "Surprise Acquisition Interest",
        TriggeringCard = "AI Center of Excellence",
        Context = "A larger competitor has noticed your AI initiative and wants to talk acquisition.",
        Outcome = "Good",
        Effects = "Runway +20, Board Favor +10"
    },
};

var senders = new[]
{
    ("Derek Chen", "VP Engineering"),
    ("Patricia Holloway", "HR Business Partner"),
    ("Marcus Webb", "Director of Talent"),
};

var random = new Random(42);
var totalTokens = 0;
var totalTime = 0.0;

Console.WriteLine("Generating situation emails (hybrid approach test)...\n");
Console.WriteLine(new string('=', 60));

foreach (var test in testCases)
{
    var sender = senders[random.Next(senders.Length)];

    // Format prompt based on model
    string prompt;
    string[] antiPrompts;

    if (modelName.ToLowerInvariant().Contains("phi"))
    {
        // Phi-3 format
        prompt = $"""
<|user|>
You write satirical corporate emails for a dark comedy game. Write a short email (60-80 words) with dark humor and passive-aggressive corporate speak.

Situation: {test.Situation}
Triggered by: {test.TriggeringCard} project
Context: {test.Context}
Outcome: {test.Outcome}
From: {sender.Item1}, {sender.Item2}

Write ONLY the email body addressed to "CEO". No subject line, no headers. Be satirical and darkly funny.<|end|>
<|assistant|>
""";
        antiPrompts = new[] { "<|end|>", "<|user|>" };
    }
    else
    {
        // Qwen format
        prompt = $"""
<|im_start|>system
You write satirical corporate emails for a dark comedy game. Your emails use passive-aggressive corporate speak and dark humor. Output ONLY the email body, no headers.
<|im_end|>
<|im_start|>user
Situation: {test.Situation}
Triggered by: {test.TriggeringCard} project
Context: {test.Context}
Outcome: {test.Outcome}
From: {sender.Item1}, {sender.Item2}

Write a short satirical email (60-80 words) from {sender.Item1} to the CEO about this situation. Be darkly funny.
<|im_end|>
<|im_start|>assistant
""";
        antiPrompts = new[] { "<|im_end|>", "<|im_start|>" };
    }

    Console.WriteLine($"\n🔥 {test.Situation} (from {test.TriggeringCard})");
    Console.WriteLine($"   From: {sender.Item1}, {sender.Item2}");
    Console.WriteLine($"   Outcome: {test.Outcome} | Effects: {test.Effects}");
    Console.WriteLine(new string('-', 60));

    var executor = new StatelessExecutor(model, parameters);
    var inferenceParams = new InferenceParams
    {
        MaxTokens = 150,
        AntiPrompts = antiPrompts,
        SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
        {
            Temperature = 0.7f,
            TopP = 0.9f,
        },
    };

    var tokenSw = Stopwatch.StartNew();
    var result = new StringBuilder();
    var tokenCount = 0;

    await foreach (var token in executor.InferAsync(prompt, inferenceParams))
    {
        result.Append(token);
        tokenCount++;
        Console.Write(token);
    }

    tokenSw.Stop();
    var tokensPerSec = tokenCount / tokenSw.Elapsed.TotalSeconds;
    totalTokens += tokenCount;
    totalTime += tokenSw.Elapsed.TotalSeconds;

    Console.WriteLine();
    Console.WriteLine($"\n   [{tokenCount} tokens in {tokenSw.Elapsed.TotalSeconds:F1}s = {tokensPerSec:F1} tok/s]");
    Console.WriteLine(new string('=', 60));
}

Console.WriteLine($"\n✅ Test complete!");
Console.WriteLine($"Model: {modelName.ToUpperInvariant()}");
Console.WriteLine($"Average: {totalTokens / testCases.Length} tokens, {totalTime / testCases.Length:F1}s per email");
Console.WriteLine($"Speed: {totalTokens / totalTime:F1} tok/s");
Console.WriteLine($"Total time: {sw.Elapsed.TotalSeconds:F1}s");
