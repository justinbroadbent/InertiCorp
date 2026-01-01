using InertiCorp.Core.Llm;
using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Email;

/// <summary>
/// LLM-aware email content provider that uses AI-generated content when available,
/// falling back to the canned template provider when not.
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
        // Use fallback for openings - LLM doesn't add much value here
        return _fallback.GetOpening(context);
    }

    public string GetClosing(EmailContentContext context)
    {
        // Use fallback for closings - LLM doesn't add much value here
        return _fallback.GetClosing(context);
    }

    public string GetOutcomeBody(EmailContentContext context, OutcomeTier outcome)
    {
        // For now, use fallback for card outcomes
        // This keeps card responses fast while crisis uses LLM
        return _fallback.GetOutcomeBody(context, outcome);
    }

    public string GetCrisisBody(EmailContentContext context, string crisisTitle, string crisisDescription)
    {
        // Try to get LLM-generated content for crisis
        var llmContent = TryGetLlmCrisisBody(context, crisisTitle, crisisDescription);
        if (llmContent != null)
        {
            return llmContent;
        }

        // Fall back to canned template
        return _fallback.GetCrisisBody(context, crisisTitle, crisisDescription);
    }

    public string GetCrisisResolutionBody(EmailContentContext context, string crisisTitle, string choiceLabel, OutcomeTier outcome)
    {
        // Try to get LLM-generated resolution content
        var llmContent = TryGetLlmResolutionBody(context, crisisTitle, choiceLabel, outcome);
        if (llmContent != null)
        {
            return llmContent;
        }

        // Fall back to canned template
        return _fallback.GetCrisisResolutionBody(context, crisisTitle, choiceLabel, outcome);
    }

    public string GetBoardDirectiveBody(EmailContentContext context, string directiveTitle, int requiredAmount, int quarterNumber, int pressureLevel)
    {
        // Use fallback for board directives - they're formulaic anyway
        return _fallback.GetBoardDirectiveBody(context, directiveTitle, requiredAmount, quarterNumber, pressureLevel);
    }

    private static string? TryGetLlmCrisisBody(EmailContentContext context, string crisisTitle, string crisisDescription)
    {
        if (!LlmServiceManager.IsReady) return null;

        var emailService = LlmServiceManager.GetEmailService();
        if (emailService == null) return null;

        // Create a synthetic situation for the crisis
        var situation = new SituationDefinition(
            SituationId: context.EventId,
            Title: crisisTitle,
            Description: crisisDescription,
            EmailSubject: $"URGENT: {crisisTitle}",
            EmailBody: crisisDescription,
            Severity: SituationSeverity.Moderate,
            Responses: Array.Empty<SituationResponse>());

        // Try to get generated email - use Expected as default outcome for initial crisis
        var generated = emailService.GetSituationEmail(
            situation,
            OutcomeTier.Expected,
            GetSenderName(context.Sender),
            GetSenderTitle(context.Sender));

        return generated?.Body;
    }

    private static string? TryGetLlmResolutionBody(EmailContentContext context, string crisisTitle, string choiceLabel, OutcomeTier outcome)
    {
        if (!LlmServiceManager.IsReady) return null;

        var emailService = LlmServiceManager.GetEmailService();
        if (emailService == null) return null;

        // Create a synthetic situation for the resolution
        var situation = new SituationDefinition(
            SituationId: $"{context.EventId}_resolution",
            Title: $"{crisisTitle} - {choiceLabel}",
            Description: $"Resolution of {crisisTitle} after choosing to {choiceLabel.ToLowerInvariant()}.",
            EmailSubject: $"RE: {crisisTitle}",
            EmailBody: $"Following up on {crisisTitle}.",
            Severity: SituationSeverity.Moderate,
            Responses: Array.Empty<SituationResponse>());

        var generated = emailService.GetSituationEmail(
            situation,
            outcome,
            GetSenderName(context.Sender),
            GetSenderTitle(context.Sender));

        return generated?.Body;
    }

    private static string GetSenderName(SenderArchetype sender) => sender switch
    {
        SenderArchetype.CEO => "The CEO",
        SenderArchetype.CFO => "Janet Chen",
        SenderArchetype.HR => "Patricia Lawson",
        SenderArchetype.Legal => "David Morrison",
        SenderArchetype.EngManager => "Kevin Park",
        SenderArchetype.PM => "Sarah Martinez",
        SenderArchetype.Marketing => "Alex Thompson",
        SenderArchetype.BoardMember => "Board of Directors",
        SenderArchetype.TechLead => "Jordan Lee",
        SenderArchetype.Security => "Mike Williams",
        SenderArchetype.Compliance => "Rachel Adams",
        SenderArchetype.Anonymous => "Anonymous",
        _ => "Corporate Communications"
    };

    private static string GetSenderTitle(SenderArchetype sender) => sender switch
    {
        SenderArchetype.CEO => "Chief Executive Officer",
        SenderArchetype.CFO => "Chief Financial Officer",
        SenderArchetype.HR => "VP Human Resources",
        SenderArchetype.Legal => "General Counsel",
        SenderArchetype.EngManager => "VP Engineering",
        SenderArchetype.PM => "Director of Product",
        SenderArchetype.Marketing => "VP Marketing",
        SenderArchetype.BoardMember => "Board Representative",
        SenderArchetype.TechLead => "Technical Lead",
        SenderArchetype.Security => "Chief Security Officer",
        SenderArchetype.Compliance => "Chief Compliance Officer",
        SenderArchetype.Anonymous => "Anonymous",
        _ => "VP Communications"
    };
}
