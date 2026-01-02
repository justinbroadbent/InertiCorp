namespace InertiCorp.Core.Email;

/// <summary>
/// An email message in a thread.
/// </summary>
public sealed record EmailMessage(
    string MessageId,
    string ThreadId,
    string Subject,
    string Body,
    SenderArchetype From,
    SenderArchetype To,
    EmailTone Tone,
    int TurnNumber,
    IReadOnlyList<string> LinkedEventIds,
    bool IsRead = false)
{
    /// <summary>
    /// Returns a new message marked as read.
    /// </summary>
    public EmailMessage WithRead() => this with { IsRead = true };

    /// <summary>
    /// Gets a display string for the sender based on message ID for consistency.
    /// </summary>
    public string FromDisplay => GetArchetypeDisplay(From, MessageId);

    /// <summary>
    /// Gets a display string for the recipient.
    /// </summary>
    public string ToDisplay => GetArchetypeDisplay(To, MessageId);

    /// <summary>
    /// Whether this message is from the CEO (the player).
    /// </summary>
    public bool IsFromPlayer => From == SenderArchetype.CEO;

    private static string GetArchetypeDisplay(SenderArchetype archetype, string messageId)
    {
        if (archetype == SenderArchetype.CEO)
            return "You (CEO)";
        if (archetype == SenderArchetype.Anonymous)
            return "Anonymous";

        // Use company directory for consistent names across display and signature
        var employee = Content.CompanyDirectory.GetEmployeeForEvent(archetype, messageId);
        return $"{employee.Name}, {employee.Title}";
    }
}


/// <summary>
/// Pending effects from a project that the player must accept.
/// </summary>
public sealed record PendingProjectEffects(
    IReadOnlyList<(Core.Meter Meter, int Delta)> MeterChanges,
    int ProfitDelta = 0,
    int EvilScoreDelta = 0,
    string OutcomeText = "Expected")
{
    /// <summary>
    /// Whether there are any effects to apply.
    /// </summary>
    public bool HasEffects => MeterChanges.Count > 0 || ProfitDelta != 0 || EvilScoreDelta != 0;
}

/// <summary>
/// A thread of related emails (original + follow-ups).
/// </summary>
public sealed record EmailThread(
    string ThreadId,
    string Subject,
    string? OriginatingCardId,
    int CreatedOnTurn,
    IReadOnlyList<EmailMessage> Messages,
    EmailThreadType ThreadType = EmailThreadType.CardResult,
    long SequenceNumber = 0,
    PendingProjectEffects? PendingEffects = null,
    bool EffectsAccepted = false,
    bool IsVisible = true)
{
    /// <summary>
    /// Whether this thread has pending effects that need player acceptance.
    /// </summary>
    public bool HasPendingEffects => PendingEffects?.HasEffects == true && !EffectsAccepted;

    /// <summary>
    /// Returns a new thread with effects marked as accepted.
    /// </summary>
    public EmailThread WithEffectsAccepted() => this with { EffectsAccepted = true };
    /// <summary>
    /// The initial "ask" message from the CEO.
    /// </summary>
    public EmailMessage? AskMessage => Messages.FirstOrDefault(m => m.IsFromPlayer);

    /// <summary>
    /// Reply messages from NPCs.
    /// </summary>
    public IReadOnlyList<EmailMessage> Replies => Messages.Where(m => !m.IsFromPlayer).ToList();

    /// <summary>
    /// The most recent message in the thread.
    /// </summary>
    public EmailMessage? LatestMessage => Messages.LastOrDefault();

    /// <summary>
    /// Whether all messages have been read.
    /// </summary>
    public bool IsFullyRead => Messages.All(m => m.IsRead);

    /// <summary>
    /// Whether this is a crisis/situation email.
    /// </summary>
    public bool IsCrisis => ThreadType == EmailThreadType.Crisis;

    /// <summary>
    /// Whether this is a board directive notification.
    /// </summary>
    public bool IsBoardDirective => ThreadType == EmailThreadType.BoardDirective;

    /// <summary>
    /// Whether this thread is high priority.
    /// </summary>
    public bool IsHighPriority => ThreadType == EmailThreadType.Crisis;

    /// <summary>
    /// Adds a follow-up message to this thread.
    /// </summary>
    public EmailThread WithFollowUp(EmailMessage message) =>
        this with { Messages = Messages.Append(message).ToList() };
}

/// <summary>
/// Type of email thread.
/// </summary>
public enum EmailThreadType
{
    CardResult,      // Result of playing a card
    Crisis,          // Crisis event requiring response
    BoardDirective,  // Board directive notification
    Notification,    // General notification
    Fluff            // Random corporate noise (reports, suck-ups, spam)
}

/// <summary>
/// Types of senders in the corporate hierarchy.
/// </summary>
public enum SenderArchetype
{
    CEO,           // The player
    PM,            // Product Manager - delivery focused
    EngManager,    // Engineering Director - delivery/morale
    TechLead,      // Tech Lead - delivery/governance
    Legal,         // Legal Counsel - governance
    Security,      // Security Lead - governance
    CFO,           // CFO - runway
    HR,            // HRBP - morale/alignment
    Marketing,     // Marketing VP - alignment
    BoardMember,   // Board Member - alignment/pressure
    Compliance,    // Compliance Officer - governance
    Anonymous      // Anonymous tip/leak
}

/// <summary>
/// Tone of an email - affects writing style.
/// </summary>
public enum EmailTone
{
    Professional,   // Neutral, by-the-book
    Aloof,          // Detached, "circling back..."
    Panicked,       // Urgent, worried
    Obsequious,     // Sycophantic, "great idea boss"
    Passive,        // Passive-aggressive
    Enthusiastic,   // Overly positive
    Cryptic,        // Vague, bureaucratic
    Blunt           // Direct, no-nonsense
}

/// <summary>
/// Email urgency levels.
/// </summary>
public enum EmailUrgency
{
    Low,
    Medium,
    High,
    Critical
}

// Legacy type alias for backwards compatibility
/// <summary>
/// Legacy email type - use EmailMessage for new code.
/// </summary>
public sealed record Email(
    string EmailId,
    string Subject,
    string Body,
    SenderArchetype Sender,
    EmailUrgency Urgency,
    EmailType Type,
    int AgeInQuarters = 0,
    bool IsRead = false,
    bool IsMisinterpretation = false)
{
    public Email WithAged() => this with { AgeInQuarters = AgeInQuarters + 1 };
    public Email WithRead() => this with { IsRead = true };
    public string SenderDisplay => Sender.ToString();
}

/// <summary>
/// Type of email - queue (actionable) or feedback (informational).
/// </summary>
public enum EmailType
{
    Queue,
    Feedback
}
