namespace InertiCorp.Core.Email;

/// <summary>
/// Interface for email content generation. Allows swapping between
/// template-based and LLM-based content generation.
/// </summary>
public interface IEmailContentProvider
{
    /// <summary>
    /// Generates an opening line for an email.
    /// </summary>
    string GetOpening(EmailContentContext context);

    /// <summary>
    /// Generates a closing line for an email.
    /// </summary>
    string GetClosing(EmailContentContext context);

    /// <summary>
    /// Generates the body text for an outcome response.
    /// </summary>
    string GetOutcomeBody(EmailContentContext context, OutcomeTier outcome);

    /// <summary>
    /// Generates a crisis email body.
    /// </summary>
    string GetCrisisBody(EmailContentContext context, string crisisTitle, string crisisDescription);

    /// <summary>
    /// Generates a crisis resolution email body.
    /// </summary>
    string GetCrisisResolutionBody(EmailContentContext context, string crisisTitle, string choiceLabel, OutcomeTier outcome);

    /// <summary>
    /// Generates a board directive email body.
    /// </summary>
    string GetBoardDirectiveBody(EmailContentContext context, string directiveTitle, int requiredAmount, int quarterNumber, int pressureLevel);
}

/// <summary>
/// Context for email content generation, providing all info needed
/// to generate appropriate content.
/// </summary>
public sealed record EmailContentContext(
    EmailTone Tone,
    SenderArchetype Sender,
    int Seed,
    string EventId,
    int QuarterNumber = 1,
    int Alignment = 50,
    int PressureLevel = 1,
    int EvilScore = 0);
