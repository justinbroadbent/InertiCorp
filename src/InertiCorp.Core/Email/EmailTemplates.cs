using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Email;

/// <summary>
/// Template fragments for email generation.
/// Uses deterministic selection via hash to ensure reproducibility.
/// </summary>
public static class EmailTemplates
{
    // === OPENINGS BY TONE ===

    private static readonly Dictionary<EmailTone, string[]> Openings = new()
    {
        [EmailTone.Professional] = new[]
        {
            "Hi,",
            "Hello,",
            "Good morning,",
            "Thank you for reaching out.",
        },
        [EmailTone.Aloof] = new[]
        {
            "Circling back on this.",
            "Per my last email,",
            "Just wanted to follow up.",
            "Looping back around.",
            "As discussed,",
        },
        [EmailTone.Panicked] = new[]
        {
            "URGENT:",
            "This is critical.",
            "We need to discuss this immediately.",
            "I have serious concerns.",
            "This can't wait.",
        },
        [EmailTone.Obsequious] = new[]
        {
            "Great initiative!",
            "Love this direction.",
            "Brilliant strategy.",
            "Really appreciate your leadership on this.",
            "This is exactly what we needed.",
        },
        [EmailTone.Passive] = new[]
        {
            "Per our earlier discussion,",
            "As you suggested,",
            "I wanted to make sure you're aware,",
            "Just so we're all on the same page,",
            "As I mentioned before,",
        },
        [EmailTone.Enthusiastic] = new[]
        {
            "Exciting update!",
            "Great news!",
            "I'm thrilled to share that",
            "This is amazing!",
            "Incredible progress!",
        },
        [EmailTone.Cryptic] = new[]
        {
            "Regarding the matter we discussed.",
            "Further to our conversation.",
            "In light of recent developments.",
            "As you're no doubt aware.",
        },
        [EmailTone.Blunt] = new[]
        {
            "Let me be direct:",
            "Bottom line:",
            "Here's the situation:",
            "Cutting to the chase:",
        }
    };

    // === CLOSINGS BY TONE ===

    private static readonly Dictionary<EmailTone, string[]> Closings = new()
    {
        [EmailTone.Professional] = new[]
        {
            "Best regards,",
            "Thanks,",
            "Regards,",
            "Best,",
        },
        [EmailTone.Aloof] = new[]
        {
            "Let me know if you have questions.",
            "Happy to sync on this.",
            "Let's take this offline.",
            "We can discuss in our next 1:1.",
        },
        [EmailTone.Panicked] = new[]
        {
            "Please advise ASAP.",
            "We need a decision now.",
            "This needs immediate attention.",
            "Waiting on your guidance.",
        },
        [EmailTone.Obsequious] = new[]
        {
            "Let me know how I can support!",
            "Happy to take on more here.",
            "Whatever you need.",
            "I'm at your disposal.",
        },
        [EmailTone.Passive] = new[]
        {
            "Just wanted to flag this.",
            "Thought you should know.",
            "Not sure if this was the intent, but...",
            "I'll defer to your judgment.",
        },
        [EmailTone.Enthusiastic] = new[]
        {
            "Can't wait to see how this plays out!",
            "So excited about this!",
            "Let's keep the momentum going!",
            "This is going to be great!",
        },
        [EmailTone.Cryptic] = new[]
        {
            "Please handle accordingly.",
            "I trust you'll understand the implications.",
            "More to follow.",
            "We'll revisit as needed.",
        },
        [EmailTone.Blunt] = new[]
        {
            "That's where we stand.",
            "Make it happen.",
            "Fix it.",
            "Your call.",
        }
    };

    // === CEO "ASK" TEMPLATES BY CARD CATEGORY ===

    private static readonly Dictionary<CardCategory, string[]> CeoAskTemplates = new()
    {
        [CardCategory.Action] = new[]
        {
            "I'm initiating {cardTitle}. Please coordinate with relevant teams and report back on progress.",
            "We're moving forward with {cardTitle}. I need all hands on deck.",
            "Executing {cardTitle} effective immediately. Keep me posted on developments.",
            "Green-lighting {cardTitle}. Let's make this happen.",
        },
        [CardCategory.Response] = new[]
        {
            "Given the current situation, I've decided to {cardTitle}. Align your teams accordingly.",
            "In response to recent events, we're going to {cardTitle}.",
            "After careful consideration, I'm choosing to {cardTitle}.",
            "The path forward is clear: {cardTitle}.",
        },
        [CardCategory.Corporate] = new[]
        {
            "It's time to {cardTitle}. This is a strategic imperative.",
            "I'm announcing {cardTitle}. Prepare the appropriate communications.",
            "We need to {cardTitle} to maintain our competitive edge.",
            "Strategic decision: {cardTitle}. Execute accordingly.",
        },
        [CardCategory.Email] = new[]
        {
            "Regarding the matter at hand, I'm going to {cardTitle}.",
            "After reviewing the situation, I've decided to {cardTitle}.",
            "My response: {cardTitle}.",
            "Handling this by choosing to {cardTitle}.",
        }
    };

    // === OUTCOME RESPONSE TEMPLATES ===

    private static readonly Dictionary<OutcomeTier, string[]> OutcomeResponseTemplates = new()
    {
        [OutcomeTier.Good] = new[]
        {
            "This went better than expected.",
            "Excellent results across the board.",
            "The initiative exceeded expectations.",
            "We nailed it.",
            "Everything came together perfectly.",
        },
        [OutcomeTier.Expected] = new[]
        {
            "Results are in line with projections.",
            "Things played out as anticipated.",
            "Mostly on track with some minor adjustments needed.",
            "As expected, with a few surprises.",
            "Standard outcome, no major issues.",
        },
        [OutcomeTier.Bad] = new[]
        {
            "We've hit some complications.",
            "This didn't go as planned.",
            "There are some concerns to address.",
            "We need to discuss the fallout.",
            "Unfortunately, there were issues.",
        }
    };

    // === METER IMPACT PHRASES ===

    private static readonly Dictionary<Meter, (string Positive, string Negative)> MeterPhrases = new()
    {
        [Meter.Delivery] = ("shipping velocity improved", "delivery timelines slipped"),
        [Meter.Morale] = ("team spirits are up", "morale took a hit"),
        [Meter.Governance] = ("processes are tighter", "compliance gaps emerged"),
        [Meter.Alignment] = ("board confidence increased", "board is concerned"),
        [Meter.Runway] = ("finances looking healthier", "burn rate increased"),
    };

    /// <summary>
    /// Selects a template variant deterministically using a hash.
    /// </summary>
    public static T SelectVariant<T>(T[] options, int seed, string eventId)
    {
        if (options.Length == 0)
            throw new ArgumentException("Options array cannot be empty", nameof(options));

        var hash = HashCode.Combine(seed, eventId);
        var index = Math.Abs(hash) % options.Length;
        return options[index];
    }

    /// <summary>
    /// Gets an opening phrase for the given tone.
    /// </summary>
    public static string GetOpening(EmailTone tone, int seed, string eventId) =>
        SelectVariant(Openings.GetValueOrDefault(tone, Openings[EmailTone.Professional]), seed, eventId);

    /// <summary>
    /// Gets a closing phrase for the given tone.
    /// </summary>
    public static string GetClosing(EmailTone tone, int seed, string eventId) =>
        SelectVariant(Closings.GetValueOrDefault(tone, Closings[EmailTone.Professional]), seed, eventId);

    /// <summary>
    /// Gets a CEO ask template for the given card category.
    /// </summary>
    public static string GetCeoAskTemplate(CardCategory category, int seed, string eventId) =>
        SelectVariant(CeoAskTemplates.GetValueOrDefault(category, CeoAskTemplates[CardCategory.Action]), seed, eventId);

    /// <summary>
    /// Gets an outcome response template for the given tier.
    /// </summary>
    public static string GetOutcomeResponse(OutcomeTier tier, int seed, string eventId) =>
        SelectVariant(OutcomeResponseTemplates[tier], seed, eventId);

    /// <summary>
    /// Gets a phrase describing a meter change.
    /// </summary>
    public static string GetMeterPhrase(Meter meter, int delta) =>
        delta >= 0 ? MeterPhrases[meter].Positive : MeterPhrases[meter].Negative;

    /// <summary>
    /// Formats meter deltas as a bullet list.
    /// </summary>
    public static string FormatMeterDeltas(IEnumerable<(Meter Meter, int Delta)> deltas)
    {
        var lines = deltas.Select(d =>
        {
            var sign = d.Delta >= 0 ? "+" : "";
            var phrase = GetMeterPhrase(d.Meter, d.Delta);
            return $"• {d.Meter}: {sign}{d.Delta} ({phrase})";
        });
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Gets the appropriate sender archetype for a meter-focused email.
    /// </summary>
    public static SenderArchetype GetSenderForMeter(Meter meter) => meter switch
    {
        Meter.Delivery => SenderArchetype.PM,
        Meter.Morale => SenderArchetype.HR,
        Meter.Governance => SenderArchetype.Compliance,
        Meter.Alignment => SenderArchetype.BoardMember,
        Meter.Runway => SenderArchetype.CFO,
        _ => SenderArchetype.PM
    };

    /// <summary>
    /// Gets the appropriate sender archetype for a card category.
    /// </summary>
    public static SenderArchetype GetSenderForCategory(CardCategory category) => category switch
    {
        CardCategory.Action => SenderArchetype.EngManager,
        CardCategory.Response => SenderArchetype.Legal,
        CardCategory.Corporate => SenderArchetype.BoardMember,
        CardCategory.Email => SenderArchetype.PM,
        _ => SenderArchetype.PM
    };

    /// <summary>
    /// Assigns a tone based on outcome tier and alignment.
    /// </summary>
    public static EmailTone GetToneForOutcome(OutcomeTier tier, int alignment) => tier switch
    {
        OutcomeTier.Good when alignment >= 60 => EmailTone.Enthusiastic,
        OutcomeTier.Good => EmailTone.Professional,
        OutcomeTier.Expected when alignment < 40 => EmailTone.Passive,
        OutcomeTier.Expected => EmailTone.Aloof,
        OutcomeTier.Bad when alignment < 30 => EmailTone.Panicked,
        OutcomeTier.Bad => EmailTone.Blunt,
        _ => EmailTone.Professional
    };
}
