using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Content;

/// <summary>
/// Loads playable cards and crisis cards from embedded JSON resources.
/// </summary>
public static class CardContentLoader
{
    private static IReadOnlyList<PlayableCard>? _cachedProjectCards;
    private static IReadOnlyList<EventCard>? _cachedCrisisCards;
    private static readonly object _projectLock = new();
    private static readonly object _crisisLock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets all playable project cards from the embedded JSON resource.
    /// Humans can edit ProjectCards.json to add/balance cards.
    /// Falls back to PlayableCards.StarterDeck if JSON is missing or empty.
    /// </summary>
    public static IReadOnlyList<PlayableCard> ProjectCards
    {
        get
        {
            if (_cachedProjectCards is null)
            {
                lock (_projectLock)
                {
                    _cachedProjectCards ??= LoadProjectCards();
                }
            }
            return _cachedProjectCards;
        }
    }

    /// <summary>
    /// Gets all crisis event cards from the embedded JSON resource.
    /// </summary>
    public static IReadOnlyList<EventCard> CrisisCards
    {
        get
        {
            if (_cachedCrisisCards is null)
            {
                lock (_crisisLock)
                {
                    _cachedCrisisCards ??= LoadCrisisCards();
                }
            }
            return _cachedCrisisCards;
        }
    }

    private static IReadOnlyList<EventCard> LoadCrisisCards()
    {
        var json = LoadEmbeddedResource("InertiCorp.Core.Content.CrisisCards.json");
        if (string.IsNullOrEmpty(json))
        {
            return CrisisEvents.All; // Fallback to hardcoded
        }

        var data = JsonSerializer.Deserialize<CrisisCardData>(json, _jsonOptions);
        if (data?.Cards is null || data.Cards.Count == 0)
        {
            return CrisisEvents.All;
        }

        return data.Cards.Select(ConvertToEventCard).ToList();
    }

    private static IReadOnlyList<PlayableCard> LoadProjectCards()
    {
        var json = LoadEmbeddedResource("InertiCorp.Core.Content.ProjectCards.json");
        if (string.IsNullOrEmpty(json))
        {
            return PlayableCards.StarterDeck; // Fallback to hardcoded
        }

        var data = JsonSerializer.Deserialize<ProjectCardData>(json, _jsonOptions);
        if (data?.Cards is null || data.Cards.Count == 0)
        {
            return PlayableCards.StarterDeck;
        }

        return data.Cards.Select(ConvertToPlayableCard).ToList();
    }

    private static PlayableCard ConvertToPlayableCard(JsonProjectCard json)
    {
        var outcomes = new OutcomeProfile(
            Good: ConvertEffects(json.Outcomes.Good),
            Expected: ConvertEffects(json.Outcomes.Expected),
            Bad: ConvertEffects(json.Outcomes.Bad)
        );

        // Parse category
        var category = CardCategory.Action;
        if (!string.IsNullOrEmpty(json.Category) && Enum.TryParse<CardCategory>(json.Category, true, out var parsedCategory))
        {
            category = parsedCategory;
        }

        // Parse meter affinity
        Meter? meterAffinity = null;
        if (!string.IsNullOrEmpty(json.MeterAffinity) && Enum.TryParse<Meter>(json.MeterAffinity, true, out var parsedMeter))
        {
            meterAffinity = parsedMeter;
        }

        return new PlayableCard(
            CardId: json.CardId,
            Title: json.Title,
            Description: json.Description,
            FlavorText: json.FlavorText ?? "",
            Outcomes: outcomes,
            CorporateIntensity: json.CorporateIntensity,
            Category: category,
            ExtendedDescription: json.ExtendedDescription,
            MeterAffinity: meterAffinity,
            RiskLevel: json.RiskLevel > 0 ? json.RiskLevel : 2
        );
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static EventCard ConvertToEventCard(JsonCrisisCard json)
    {
        var choices = json.Choices.Select(ConvertToChoice).ToList();
        return new EventCard(json.EventId, json.Title, json.Description, choices);
    }

    private static Choice ConvertToChoice(JsonChoice json)
    {
        var outcomes = new OutcomeProfile(
            Good: ConvertEffects(json.Outcomes.Good),
            Expected: ConvertEffects(json.Outcomes.Expected),
            Bad: ConvertEffects(json.Outcomes.Bad)
        );

        if (json.HasPCCost && json.PCCost > 0)
        {
            return Choice.WithPCCost(json.ChoiceId, json.Text, json.PCCost, outcomes);
        }
        else if (json.IsCorporate)
        {
            return Choice.Corporate(json.ChoiceId, json.Text, outcomes, json.CorporateIntensityDelta);
        }
        else
        {
            return Choice.Tiered(json.ChoiceId, json.Text, outcomes);
        }
    }

    private static IEffect[] ConvertEffects(List<JsonEffect>? effects)
    {
        if (effects is null || effects.Count == 0)
            return Array.Empty<IEffect>();

        var result = new List<IEffect>();
        foreach (var e in effects)
        {
            if (!string.IsNullOrEmpty(e.Meter) && Enum.TryParse<Meter>(e.Meter, true, out var meter))
            {
                result.Add(new MeterEffect(meter, e.Delta));
            }
            else if (e.Profit.HasValue)
            {
                result.Add(new ProfitEffect(e.Profit.Value));
            }
        }
        return result.ToArray();
    }
}

#region JSON Data Models

internal sealed class ProjectCardData
{
    public List<JsonProjectCard> Cards { get; set; } = new();
}

internal sealed class JsonProjectCard
{
    public string CardId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? FlavorText { get; set; }
    public string? ExtendedDescription { get; set; }
    public string Category { get; set; } = "Action";
    public string? MeterAffinity { get; set; }
    public int CorporateIntensity { get; set; }
    public int RiskLevel { get; set; } = 2;
    public JsonOutcomes Outcomes { get; set; } = new();
}

internal sealed class CrisisCardData
{
    public List<JsonCrisisCard> Cards { get; set; } = new();
}

internal sealed class JsonCrisisCard
{
    public string EventId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<JsonChoice> Choices { get; set; } = new();
}

internal sealed class JsonChoice
{
    public string ChoiceId { get; set; } = "";
    public string Text { get; set; } = "";
    public bool HasPCCost { get; set; }
    public int PCCost { get; set; }
    public bool IsCorporate { get; set; }
    public int CorporateIntensityDelta { get; set; }
    public JsonOutcomes Outcomes { get; set; } = new();
}

internal sealed class JsonOutcomes
{
    public List<JsonEffect> Good { get; set; } = new();
    public List<JsonEffect> Expected { get; set; } = new();
    public List<JsonEffect> Bad { get; set; } = new();
}

internal sealed class JsonEffect
{
    public string? Meter { get; set; }
    public int Delta { get; set; }
    public int? Profit { get; set; }
}

#endregion
