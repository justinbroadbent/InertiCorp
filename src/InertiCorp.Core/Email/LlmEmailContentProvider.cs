namespace InertiCorp.Core.Email;

/// <summary>
/// LLM-aware email content provider.
/// Note: All LLM generation for emails is now handled by BackgroundEmailProcessor
/// which updates email content asynchronously. This provider now just delegates
/// to the fallback template provider for initial content.
/// </summary>
public sealed class LlmEmailContentProvider : IEmailContentProvider
{
    private readonly IEmailContentProvider _fallback;

    public LlmEmailContentProvider(IEmailContentProvider? fallback = null)
    {
        _fallback = fallback ?? new CorporateHumorCorpus();
    }

    public string GetOpening(EmailContentContext context)
    {
        return _fallback.GetOpening(context);
    }

    public string GetClosing(EmailContentContext context)
    {
        return _fallback.GetClosing(context);
    }

    public string GetOutcomeBody(EmailContentContext context, OutcomeTier outcome)
    {
        return _fallback.GetOutcomeBody(context, outcome);
    }

    public string GetCrisisBody(EmailContentContext context, string crisisTitle, string crisisDescription)
    {
        // Initial crisis emails use fallback template.
        // BackgroundEmailProcessor will update with AI content when ready.
        return _fallback.GetCrisisBody(context, crisisTitle, crisisDescription);
    }

    public string GetCrisisResolutionBody(EmailContentContext context, string crisisTitle, string choiceLabel, OutcomeTier outcome)
    {
        // Initial resolution emails use fallback template.
        // BackgroundEmailProcessor will update with AI content when ready.
        return _fallback.GetCrisisResolutionBody(context, crisisTitle, choiceLabel, outcome);
    }

    public string GetBoardDirectiveBody(EmailContentContext context, string directiveTitle, int requiredAmount, int quarterNumber, int pressureLevel)
    {
        return _fallback.GetBoardDirectiveBody(context, directiveTitle, requiredAmount, quarterNumber, pressureLevel);
    }
}
