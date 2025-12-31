using System.Reflection;
using System.Text.Json;

namespace InertiCorp.Core.Content;

/// <summary>
/// Loads email templates from embedded JSON resources.
/// </summary>
public static class EmailTemplateLoader
{
    private static EmailTemplateData? _cachedTemplates;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the loaded email templates, loading from embedded resource if needed.
    /// </summary>
    public static EmailTemplateData Templates
    {
        get
        {
            if (_cachedTemplates is null)
            {
                lock (_lock)
                {
                    _cachedTemplates ??= LoadTemplates();
                }
            }
            return _cachedTemplates;
        }
    }

    private static EmailTemplateData LoadTemplates()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "InertiCorp.Core.Content.EmailTemplates.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // Fallback to empty templates if resource not found
            return new EmailTemplateData();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<EmailTemplateData>(json, options) ?? new EmailTemplateData();
    }

    /// <summary>
    /// Selects a template deterministically based on seed and event ID.
    /// </summary>
    public static string SelectTemplate(IReadOnlyList<string> templates, int seed, string eventId)
    {
        if (templates.Count == 0) return string.Empty;
        var hash = Math.Abs(HashCode.Combine(seed, eventId));
        return templates[hash % templates.Count];
    }

    /// <summary>
    /// Applies variable substitutions to a template string.
    /// </summary>
    public static string ApplySubstitutions(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace($"{{{key}}}", value);
        }
        return result;
    }
}

/// <summary>
/// Root structure for email template data.
/// </summary>
public sealed class EmailTemplateData
{
    public EmailTemplateCategory Reorg { get; set; } = new();
    public EmailTemplateCategory ImageRehab { get; set; } = new();
    public EmailTemplateCategory SchmoozeSuccess { get; set; } = new();
    public EmailTemplateCategory SchmoozeFailure { get; set; } = new();
}

/// <summary>
/// A category of email templates with subjects and bodies.
/// </summary>
public sealed class EmailTemplateCategory
{
    public List<string> Subjects { get; set; } = new();
    public List<string> Bodies { get; set; } = new();
}
