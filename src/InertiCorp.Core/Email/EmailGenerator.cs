using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Email;

/// <summary>
/// Generates threaded email conversations from game actions.
/// Each played card creates a thread: CEO ask → NPC reply(s).
/// Uses deterministic template selection for reproducibility.
/// </summary>
public sealed class EmailGenerator
{
    private readonly int _seed;
    private readonly IEmailContentProvider _contentProvider;
    private int _threadCounter;
    private int _messageCounter;

    public EmailGenerator(int seed, IEmailContentProvider? contentProvider = null)
    {
        _seed = seed;
        // Use LLM-aware provider by default - it falls back to canned templates when LLM isn't ready
        _contentProvider = contentProvider ?? new LlmEmailContentProvider();
        _threadCounter = 0;
        _messageCounter = 0;
    }

    /// <summary>
    /// Creates an email thread for a played card with outcome.
    /// Returns the CEO's ask message and the NPC's reply.
    /// For Revenue cards, includes profit impact in the email.
    /// </summary>
    public EmailThread CreateCardThread(
        PlayableCard card,
        OutcomeTier outcome,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas,
        int turnNumber,
        int alignment,
        int profitDelta = 0,
        int evilScoreDelta = 0)
    {
        var threadId = GenerateThreadId();
        var subject = GenerateSubject(card);

        // Create CEO's ask message
        var askMessage = CreateAskMessage(threadId, subject, card, turnNumber);

        // Create NPC reply message
        var replyMessage = CreateReplyMessage(
            threadId, subject, card, outcome, meterDeltas, turnNumber, alignment, profitDelta);

        // Create pending effects for display (player must acknowledge)
        var pendingEffects = new PendingProjectEffects(
            MeterChanges: meterDeltas,
            ProfitDelta: profitDelta,
            EvilScoreDelta: evilScoreDelta,
            OutcomeText: outcome.ToString());

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: card.CardId,
            CreatedOnTurn: turnNumber,
            Messages: new[] { askMessage, replyMessage },
            ThreadType: EmailThreadType.CardResult,
            PendingEffects: pendingEffects);
    }

    /// <summary>
    /// Adds a follow-up reply to an existing thread (for aftershocks).
    /// </summary>
    public EmailMessage CreateFollowUpReply(
        string threadId,
        string subject,
        string eventDescription,
        IReadOnlyList<(Meter Meter, int Delta)> effects,
        int turnNumber,
        int _,
        SenderArchetype sender)
    {
        var eventId = $"{threadId}_followup_{turnNumber}";
        var tone = effects.Any(e => e.Delta < 0)
            ? EmailTone.Panicked
            : EmailTone.Aloof;

        var body = BuildFollowUpBody(eventDescription, effects, tone, eventId);

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {subject}",
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });
    }

    /// <summary>
    /// Creates a good follow-up reply (positive news about a project).
    /// </summary>
    public EmailMessage CreateGoodFollowUpReply(
        string threadId,
        string threadSubject,
        string cardTitle,
        IReadOnlyList<(Meter Meter, int Delta)> effects,
        int turnNumber)
    {
        var eventId = $"{threadId}_good_followup_{turnNumber}";

        var effectDescriptions = effects.Select(e =>
            $"{e.Meter}: {(e.Delta >= 0 ? "+" : "")}{e.Delta}").ToList();

        var body = $"""
            CEO,

            Great news regarding {cardTitle}!

            {GetGoodFollowUpMessage()}

            Impact:
            {string.Join("\n", effectDescriptions.Select(e => $"• {e}"))}

            Regards,
            {GetRandomSender(SenderArchetype.PM)}
            """;

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {threadSubject} - Good News!",
            Body: body,
            From: SenderArchetype.PM,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Enthusiastic,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });
    }

    /// <summary>
    /// Creates a meh follow-up reply (neutral/mild news about a project).
    /// </summary>
    public EmailMessage CreateMehFollowUpReply(
        string threadId,
        string threadSubject,
        string cardTitle,
        IReadOnlyList<(Meter Meter, int Delta)> effects,
        int turnNumber)
    {
        var eventId = $"{threadId}_meh_followup_{turnNumber}";
        var isPositive = effects.Any() && effects[0].Delta >= 0;

        var effectDescriptions = effects.Select(e =>
            $"{e.Meter}: {(e.Delta >= 0 ? "+" : "")}{e.Delta}").ToList();

        var body = $"""
            CEO,

            Quick update on {cardTitle}.

            {GetMehFollowUpMessage(isPositive)}

            Impact:
            {string.Join("\n", effectDescriptions.Select(e => $"• {e}"))}

            - {GetRandomSender(SenderArchetype.EngManager)}
            """;

        var tone = isPositive ? EmailTone.Professional : EmailTone.Aloof;

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {threadSubject} - Update",
            Body: body,
            From: SenderArchetype.EngManager,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });
    }

    private string GetGoodFollowUpMessage()
    {
        var messages = new[]
        {
            "The initiative exceeded expectations. The team really pulled together on this one.",
            "We're seeing better results than projected. Stakeholders are pleased.",
            "This is turning into a real success story. Marketing wants to do a case study.",
            "The ripple effects have been surprisingly positive across the organization.",
            "Sometimes things just work out. This was one of those times.",
        };
        return messages[_seed % messages.Length];
    }

    private string GetMehFollowUpMessage(bool isPositive)
    {
        if (isPositive)
        {
            var messages = new[]
            {
                "Nothing dramatic, but we're seeing some modest improvements.",
                "Small wins are still wins. Progress is progress.",
                "The numbers are trending in the right direction, slowly but surely.",
            };
            return messages[_seed % messages.Length];
        }
        else
        {
            var messages = new[]
            {
                "We've hit a small snag. Nothing catastrophic, but worth noting.",
                "Some minor complications have emerged. We're handling it.",
                "Not ideal, but we'll manage. Just wanted to keep you in the loop.",
                "A few bumps in the road. The team is adjusting.",
            };
            return messages[_seed % messages.Length];
        }
    }

    private string GetRandomSender(SenderArchetype archetype)
    {
        var names = archetype switch
        {
            SenderArchetype.PM => new[] { "Sarah Chen", "Mike Rodriguez", "Jennifer Walsh" },
            SenderArchetype.EngManager => new[] { "Dave Kumar", "Lisa Park", "Tom Stevens" },
            _ => new[] { "A. Employee" }
        };
        return names[_seed % names.Length];
    }

    /// <summary>
    /// Creates a standalone notification thread (for events not tied to cards).
    /// </summary>
    public EmailThread CreateNotificationThread(
        string eventId,
        string subject,
        string body,
        SenderArchetype sender,
        int turnNumber,
        EmailTone tone = EmailTone.Professional)
    {
        var threadId = GenerateThreadId();

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: turnNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }

    /// <summary>
    /// Creates the initial welcome email from the board congratulating the new CEO.
    /// Explains the game mechanics in corporate speak.
    /// </summary>
    public EmailThread CreateWelcomeThread()
    {
        var threadId = GenerateThreadId();
        var eventId = "welcome_aboard";

        var subject = "Congratulations on Your Appointment as Chief Executive Officer";
        var body = """
            Dear New CEO,

            On behalf of the Board of Directors, I am pleased to formally welcome you to your new role as Chief Executive Officer of InertiCorp.

            After an exhaustive search process involving multiple synergy assessments and cultural fit evaluations, the Board has determined that you possess the optimal alignment of competencies to drive our organizational transformation forward.

            As you onboard into this leadership position, please be advised of several key operational parameters:

            ═══════════════════════════════════════
            QUARTERLY PROFIT EXPECTATIONS
            ═══════════════════════════════════════
            Each quarter, the Board will issue a profit growth directive. Your quarterly profit consists of:
            • Base Operations Revenue — A dice roll based on your organizational health
            • Revenue Project Impact — Only REVENUE-type projects affect profit directly

            Important: Standard projects (Digital Transformation, Culture Initiatives, etc.) affect organizational metrics but NOT profit. Only Revenue projects drive the bottom line.

            ═══════════════════════════════════════
            POLITICAL CAPITAL (PC)
            ═══════════════════════════════════════
            PC represents your organizational influence. You earn PC through:
            • Restraint — Playing fewer cards earns bonus PC (0 cards = +3 PC, 1 card = +2 PC)
            • Strong organizational health (high metrics = PC income)
            • Trading organizational health for influence (meter exchange)

            PC can be spent on:
            • Additional projects — 2nd card costs 1 PC (+10% risk), 3rd card costs 2 PC (+20% risk)
            • Meter boost — Spend 1 PC to boost any metric by 5 points
            • Board schmoozing — Spend 2 PC to improve favorability (15% backfire chance)
            • Hand re-org — Spend 3 PC to discard projects and draw 5 new ones
            • Image rehabilitation — Spend 2 PC to reduce Evil Score by 1

            ═══════════════════════════════════════
            THE HONEYMOON PERIOD (Q1-Q3)
            ═══════════════════════════════════════
            The Board extends new executives a grace period. During your first three quarters, outcomes will be more favorable. Use this time to build reserves — the honeymoon ends in Q4.

            ═══════════════════════════════════════
            EVIL SCORE & CORPORATE CHOICES
            ═══════════════════════════════════════
            Some projects and choices are... ethically flexible. These "Corporate" options increase your Evil Score. The Board tolerates evil when profits are strong, but scrutinizes it when numbers disappoint. High evil + poor performance = accelerated termination.

            ═══════════════════════════════════════
            GOLDEN PARACHUTE
            ═══════════════════════════════════════
            Should your tenure end prematurely, your severance depends on longevity:
            • Under 1 year (Q1-Q3): A Starbucks gift card. We barely knew you.
            • 1+ years: Meaningful severance scaling with tenure and performance

            Survive longer, negotiate harder, and your parachute grows accordingly.

            ═══════════════════════════════════════

            The previous CEO lasted four quarters. We trust you will exceed that benchmark.

            Regards,
            Patricia Sterling
            Chairperson, Board of Directors
            InertiCorp Holdings, LLC

            P.S. — Your predecessor's personal effects have been removed from the corner office. The plant did not survive.
            """;

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.BoardMember,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Professional,
            TurnNumber: 1,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: 1,
            Messages: new[] { message },
            ThreadType: EmailThreadType.BoardDirective);
    }

    /// <summary>
    /// Creates a board directive email thread (sent at START of quarter).
    /// </summary>
    public EmailThread CreateBoardDirectiveThread(
        BoardDirective directive,
        int pressureLevel,
        int quarterNumber)
    {
        var threadId = GenerateThreadId();
        var eventId = $"directive_{directive.DirectiveId}_Q{quarterNumber}";
        var requiredAmount = directive.GetRequiredAmount(pressureLevel);

        var context = new EmailContentContext(
            pressureLevel >= 3 ? EmailTone.Blunt : EmailTone.Professional,
            SenderArchetype.BoardMember,
            _seed,
            eventId,
            quarterNumber,
            PressureLevel: pressureLevel);

        var subject = $"{QuarterState.FormatQuarter(quarterNumber)} Board Expectations";
        var body = _contentProvider.GetBoardDirectiveBody(context, directive.Title, requiredAmount, quarterNumber, pressureLevel);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.BoardMember,
            To: SenderArchetype.CEO,
            Tone: pressureLevel >= 3 ? EmailTone.Blunt : EmailTone.Professional,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: directive.DirectiveId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.BoardDirective);
    }

    /// <summary>
    /// Creates a board decision/review email (sent at END of quarter).
    /// Includes income statement, bonus award, and board's reaction.
    /// </summary>
    public EmailThread CreateBoardDecisionThread(
        int quarterNumber,
        int baseOperations,
        int projectImpact,
        int totalProfit,
        int lastQuarterProfit,
        int requiredIncrease,
        bool directiveMet,
        int favorabilityChange,
        int newFavorability,
        int pressureLevel,
        OrgState org,
        bool survived,
        int bonusAmount,
        IReadOnlyList<string> bonusReasons,
        int accumulatedBonus,
        bool canRetire,
        IReadOnlyList<(Meter Meter, int Delta)>? metricRewards = null)
    {
        var threadId = GenerateThreadId();
        var eventId = $"board_decision_Q{quarterNumber}";

        var subject = survived
            ? $"{QuarterState.FormatQuarter(quarterNumber)} Performance Review - Board Decision"
            : $"{QuarterState.FormatQuarter(quarterNumber)} Performance Review - Termination Notice";

        var metricsAssessment = BuildMetricsAssessment(org);

        // Build income statement
        var incomeStatement = BuildIncomeStatement(baseOperations, projectImpact, totalProfit);

        // Build profit comparison
        var actualIncrease = totalProfit - lastQuarterProfit;
        var increaseSign = actualIncrease >= 0 ? "+" : "";

        var comparisonText = lastQuarterProfit > 0
            ? $"Previous Quarter Total: ${lastQuarterProfit}M\n" +
              $"            Quarter-over-Quarter Change: {increaseSign}${actualIncrease}M (Required: +${requiredIncrease}M)"
            : $"First quarter target: +${requiredIncrease}M growth";

        var directiveResult = directiveMet
            ? "✓ DIRECTIVE MET: The board acknowledges acceptable performance."
            : $"✗ DIRECTIVE FAILED: Growth of {increaseSign}${actualIncrease}M did not meet the +${requiredIncrease}M target.";

        var favorabilityText = favorabilityChange >= 0
            ? $"Your standing with the board has improved by {favorabilityChange} points (now {newFavorability}%)."
            : $"Your standing with the board has declined by {Math.Abs(favorabilityChange)} points (now {newFavorability}%).";

        // Build bonus section
        var bonusSection = BuildBonusSection(bonusAmount, bonusReasons, accumulatedBonus, canRetire);

        // Build metric rewards section if any
        var rewardsSection = metricRewards is { Count: > 0 }
            ? BuildMetricRewardsSection(metricRewards)
            : "";

        var closingText = survived
            ? GetSurvivalClosing(pressureLevel, newFavorability, canRetire)
            : GetTerminationClosing();

        var body = $"""
            Dear CEO,

            The board has concluded its Q{quarterNumber} performance review. Our findings are as follows:

            ═══════════════════════════════════════
            QUARTERLY INCOME STATEMENT
            ═══════════════════════════════════════
            {incomeStatement}
            ═══════════════════════════════════════

            GROWTH ASSESSMENT
            {comparisonText}
            {directiveResult}

            ORGANIZATIONAL HEALTH METRICS
            {metricsAssessment}
            {rewardsSection}
            COMPENSATION COMMITTEE DECISION
            {bonusSection}

            BOARD SENTIMENT
            {favorabilityText}

            {closingText}

            Regards,
            Patricia Sterling
            Chairperson, Board of Directors
            InertiCorp Holdings, LLC
            """;

        var tone = survived
            ? (favorabilityChange >= 0 ? EmailTone.Professional : EmailTone.Blunt)
            : EmailTone.Panicked;

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.BoardMember,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.BoardDirective);
    }

    private static string BuildBonusSection(int bonusAmount, IReadOnlyList<string> bonusReasons, int accumulatedBonus, bool canRetire)
    {
        var lines = new List<string>();

        if (bonusAmount > 0)
        {
            lines.Add($"The compensation committee has approved a quarterly bonus of ${bonusAmount}M.");
            lines.Add("");
            lines.Add("Bonus factors:");
            foreach (var reason in bonusReasons)
            {
                lines.Add($"  • {reason}");
            }
        }
        else
        {
            lines.Add("The compensation committee has determined no bonus is warranted this quarter.");
            if (bonusReasons.Count > 0)
            {
                lines.Add("");
                lines.Add("Factors considered:");
                foreach (var reason in bonusReasons)
                {
                    lines.Add($"  • {reason}");
                }
            }
        }

        lines.Add("");
        lines.Add($"Accumulated Bonus to Date: ${accumulatedBonus}M");

        if (canRetire)
        {
            lines.Add("");
            lines.Add("═══ RETIREMENT ELIGIBLE ═══");
            lines.Add($"You have accumulated ${accumulatedBonus}M in bonuses, exceeding the ${CEOState.RetirementThreshold}M");
            lines.Add("threshold for voluntary retirement with full benefits.");
            lines.Add("You may choose to retire at your discretion.");
        }
        else
        {
            var remaining = CEOState.RetirementThreshold - accumulatedBonus;
            lines.Add($"(${remaining}M more needed for retirement eligibility)");
        }

        return string.Join("\n            ", lines);
    }

    private static string BuildMetricRewardsSection(IReadOnlyList<(Meter Meter, int Delta)> rewards)
    {
        var lines = new List<string>
        {
            "",
            "BOARD DISCRETIONARY AWARDS",
            "In recognition of exceptional performance, the board has authorized:"
        };

        foreach (var (meter, delta) in rewards)
        {
            var description = meter switch
            {
                Meter.Runway => $"Additional budget allocation: +{delta} Runway",
                Meter.Morale => $"Team recognition program: +{delta} Morale",
                Meter.Delivery => $"Process improvement investment: +{delta} Delivery",
                Meter.Governance => $"Compliance enhancement: +{delta} Governance",
                Meter.Alignment => $"Strategic clarity initiative: +{delta} Alignment",
                _ => $"+{delta} {meter}"
            };
            lines.Add($"  • {description}");
        }

        lines.Add("");
        return string.Join("\n            ", lines);
    }

    private static string BuildIncomeStatement(int baseOperations, int projectImpact, int totalProfit)
    {
        var lines = new List<string>();

        // Format base operations with market commentary
        var baseComment = baseOperations switch
        {
            < 0 => "(Market downturn)",
            < 90 => "(Below expectations)",
            < 120 => "(On target)",
            _ => "(Strong market)"
        };

        lines.Add($"Base Operations Revenue:     {FormatProfit(baseOperations),10} {baseComment}");

        // Show project impact if any
        if (projectImpact != 0)
        {
            var projectComment = projectImpact switch
            {
                >= 30 => "(Excellent execution)",
                >= 10 => "(Solid performance)",
                >= 0 => "(Marginal gains)",
                >= -15 => "(Minor setbacks)",
                _ => "(Significant losses)"
            };
            lines.Add($"Strategic Initiative Impact: {FormatProfit(projectImpact),10} {projectComment}");
        }
        else
        {
            lines.Add($"Strategic Initiative Impact: {"$0M",10} (No revenue projects this quarter)");
        }

        lines.Add($"───────────────────────────────────────");
        lines.Add($"NET QUARTERLY PROFIT:        {FormatProfit(totalProfit),10}");

        return string.Join("\n            ", lines);
    }

    private static string FormatProfit(int amount)
    {
        if (amount >= 0)
            return $"${amount}M";
        return $"-${Math.Abs(amount)}M";
    }

    private static string BuildMetricsAssessment(OrgState org)
    {
        var lines = new List<string>();

        lines.Add(GetMetricLine("Delivery", org.Delivery));
        lines.Add(GetMetricLine("Morale", org.Morale));
        lines.Add(GetMetricLine("Governance", org.Governance));
        lines.Add(GetMetricLine("Alignment", org.Alignment));
        lines.Add(GetMetricLine("Runway", org.Runway));

        return string.Join("\n", lines);
    }

    private static string GetMetricLine(string name, int value)
    {
        var assessment = value switch
        {
            >= 80 => "Exceptional",
            >= 60 => "Satisfactory",
            >= 40 => "Concerning",
            >= 20 => "Critical",
            _ => "CATASTROPHIC"
        };

        var commentary = value switch
        {
            >= 80 => "The board commends this performance.",
            >= 60 => "Continue current trajectory.",
            >= 40 => "Improvement expected next quarter.",
            >= 20 => "Immediate remediation required.",
            _ => "This is grounds for termination."
        };

        return $"• {name}: {value}/100 ({assessment}) - {commentary}";
    }

    private static string GetSurvivalClosing(int pressureLevel, int favorability, bool canRetire = false)
    {
        var retirementNote = canRetire
            ? "\n\n                P.S. The board notes your eligibility for voluntary retirement. Should you choose to\n                step down gracefully, we are prepared to honor your full compensation package."
            : "";

        if (favorability >= 70)
        {
            return $"""
                BOARD DECISION
                The board has voted to extend your tenure. Your performance has been... adequate.
                We look forward to continued growth in the coming quarter.

                Do not disappoint us.{retirementNote}
                """;
        }
        else if (favorability >= 40)
        {
            return $"""
                BOARD DECISION
                The board has voted to retain you — for now. However, be advised that patience is wearing thin.
                Board pressure has been set to Level {pressureLevel}. Expectations will be adjusted accordingly.

                Your position is not secure. Perform, or be replaced.{retirementNote}
                """;
        }
        else
        {
            return $"""
                BOARD DECISION
                After considerable debate, the board has elected NOT to terminate your employment at this time.
                This was not a unanimous decision. Several board members expressed serious reservations.
                Board pressure is now at Level {pressureLevel}.

                Consider this your final warning. The next review may not be so lenient.{retirementNote}
                """;
        }
    }

    private static string GetTerminationClosing()
    {
        return """
            BOARD DECISION
            After careful deliberation, the board has voted to terminate your employment, effective immediately.

            Your golden parachute compensation package will be processed per your contract terms.
            Security will escort you from the premises. Your personal effects will be shipped.

            The board thanks you for your service to InertiCorp. Your replacement has already been selected.

            Do not contact us.
            """;
    }

    /// <summary>
    /// Creates a crisis/situation email thread.
    /// </summary>
    public EmailThread CreateCrisisThread(
        EventCard crisisEvent,
        int quarterNumber,
        int alignment,
        int pressureLevel,
        int evilScore = 0)
    {
        var threadId = GenerateThreadId();
        var eventId = crisisEvent.EventId;

        var context = new EmailContentContext(
            EmailTone.Panicked,
            SenderArchetype.EngManager,
            _seed,
            eventId,
            quarterNumber,
            alignment,
            pressureLevel,
            evilScore);

        var subject = $"URGENT: {crisisEvent.Title}";
        var body = _contentProvider.GetCrisisBody(context, crisisEvent.Title, crisisEvent.Description);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.EngManager,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Panicked,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Crisis);
    }

    private EmailMessage CreateAskMessage(
        string threadId,
        string subject,
        PlayableCard card,
        int turnNumber)
    {
        var eventId = $"{card.CardId}_{turnNumber}_ask";
        var template = EmailTemplates.GetCeoAskTemplate(card.Category, _seed, eventId);
        var body = template.Replace("{cardTitle}", card.Title.ToLowerInvariant());

        // Add card description
        body = $"{body}\n\n{card.Description}";

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.CEO,
            To: GetRecipientForCard(card),
            Tone: EmailTone.Professional,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });
    }

    private EmailMessage CreateReplyMessage(
        string threadId,
        string subject,
        PlayableCard card,
        OutcomeTier outcome,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas,
        int turnNumber,
        int alignment,
        int profitDelta = 0)
    {
        var eventId = $"{card.CardId}_{turnNumber}_reply";
        var sender = GetResponderForCard(card, meterDeltas);
        var tone = EmailTemplates.GetToneForOutcome(outcome, alignment);

        var body = BuildReplyBody(card, outcome, meterDeltas, tone, eventId, profitDelta, turnNumber, alignment);

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {subject}",
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId, card.CardId });
    }

    private string BuildReplyBody(
        PlayableCard card,
        OutcomeTier outcome,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas,
        EmailTone tone,
        string eventId,
        int profitDelta = 0,
        int quarterNumber = 1,
        int alignment = 50)
    {
        var context = new EmailContentContext(tone, SenderArchetype.EngManager, _seed, eventId, quarterNumber, alignment);
        var opening = _contentProvider.GetOpening(context);
        var outcomeText = _contentProvider.GetOutcomeBody(context, outcome);
        var closing = _contentProvider.GetClosing(context);

        var parts = new List<string>
        {
            opening,
            "",
            outcomeText
        };

        // Add revenue impact for Revenue cards (prominently at the top)
        if (card.Category == Cards.CardCategory.Revenue && profitDelta != 0)
        {
            parts.Add("");
            var sign = profitDelta >= 0 ? "+" : "";
            var revenueLabel = profitDelta >= 0 ? "REVENUE GENERATED" : "REVENUE IMPACT";
            parts.Add($"═══ {revenueLabel} ═══");
            parts.Add($"💰 {sign}${profitDelta}M");
            parts.Add("═══════════════════════");
        }

        // Add meter impacts
        if (meterDeltas.Count > 0)
        {
            parts.Add("");
            parts.Add("Organizational Impact:");
            parts.Add(EmailTemplates.FormatMeterDeltas(meterDeltas));
        }

        // Add corporate flavor if applicable
        if (card.IsCorporate)
        {
            parts.Add("");
            parts.Add($"(Corporate initiative intensity: {card.CorporateIntensity})");
        }

        parts.Add("");
        parts.Add(closing);

        return string.Join("\n", parts);
    }

    private string BuildFollowUpBody(
        string eventDescription,
        IReadOnlyList<(Meter Meter, int Delta)> effects,
        EmailTone tone,
        string eventId)
    {
        var context = new EmailContentContext(tone, SenderArchetype.EngManager, _seed, eventId);
        var opening = _contentProvider.GetOpening(context);
        var closing = _contentProvider.GetClosing(context);

        var parts = new List<string>
        {
            opening,
            "",
            eventDescription
        };

        if (effects.Count > 0)
        {
            parts.Add("");
            parts.Add("Updated Impact:");
            parts.Add(EmailTemplates.FormatMeterDeltas(effects));
        }

        parts.Add("");
        parts.Add(closing);

        return string.Join("\n", parts);
    }

    private static string GenerateSubject(PlayableCard card)
    {
        return card.Category switch
        {
            CardCategory.Action => $"Initiative: {card.Title}",
            CardCategory.Response => $"Re: {card.Title}",
            CardCategory.Corporate => $"Strategic: {card.Title}",
            CardCategory.Email => $"FW: {card.Title}",
            _ => card.Title
        };
    }

    private static SenderArchetype GetRecipientForCard(PlayableCard card) =>
        EmailTemplates.GetSenderForCategory(card.Category);

    private static SenderArchetype GetResponderForCard(
        PlayableCard card,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas)
    {
        // Pick responder based on the most affected meter
        var primaryMeter = meterDeltas
            .OrderByDescending(d => Math.Abs(d.Delta))
            .Select(d => d.Meter)
            .FirstOrDefault();

        if (primaryMeter != default)
        {
            return EmailTemplates.GetSenderForMeter(primaryMeter);
        }

        return EmailTemplates.GetSenderForCategory(card.Category);
    }

    private string GenerateThreadId() => $"thread_{++_threadCounter}_{_seed}";
    private string GenerateMessageId() => $"msg_{++_messageCounter}_{_seed}";

    // === Crisis Resolution Emails ===

    /// <summary>
    /// Creates a CEO response email when the player makes a crisis decision.
    /// This shows the player's choice in the email thread.
    /// </summary>
    public EmailMessage CreateCEOResponseEmail(
        string threadId,
        string threadSubject,
        string choiceLabel,
        int turnNumber,
        bool isCorporateChoice = false)
    {
        var eventId = $"{threadId}_ceo_response_{turnNumber}";

        // Generate a brief CEO response based on the choice
        var responseBody = GetCEOResponseBody(choiceLabel, isCorporateChoice);

        var body = $"""
            Team,

            {responseBody}

            Make it happen.

            - CEO
            """;

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {threadSubject}",
            Body: body,
            From: SenderArchetype.CEO,
            To: SenderArchetype.EngManager, // Addressed to the team
            Tone: isCorporateChoice ? EmailTone.Blunt : EmailTone.Professional,
            TurnNumber: turnNumber,
            LinkedEventIds: new[] { eventId });
    }

    private string GetCEOResponseBody(string choiceLabel, bool isCorporateChoice)
    {
        // Extract key action from the choice label
        var action = choiceLabel.Length > 50 ? choiceLabel[..47] + "..." : choiceLabel;

        if (isCorporateChoice)
        {
            var corporateResponses = new[]
            {
                $"After careful consideration of our options, I've decided we need to take the pragmatic approach. Proceed with: {action}",
                $"Sometimes leadership requires difficult decisions. We're going with: {action}",
                $"I've weighed the alternatives. The board will understand. Execute: {action}",
                $"Let's not overthink this. Do what needs to be done: {action}",
            };
            return corporateResponses[_seed % corporateResponses.Length];
        }
        else
        {
            var standardResponses = new[]
            {
                $"I've reviewed the situation. Here's our path forward: {action}",
                $"After considering our options, I'm directing the team to: {action}",
                $"Let's move forward with the following approach: {action}",
                $"This is our play: {action}",
            };
            return standardResponses[_seed % standardResponses.Length];
        }
    }

    /// <summary>
    /// Creates a humorous email thread explaining the resolution of a crisis.
    /// </summary>
    public EmailThread CreateCrisisResolutionThread(
        string crisisTitle,
        string choiceLabel,
        OutcomeTier outcome,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas,
        int quarterNumber,
        bool wasCorporateChoice = false,
        int evilScoreDelta = 0)
    {
        var threadId = GenerateThreadId();
        var eventId = $"crisis_resolution_{quarterNumber}_{crisisTitle.GetHashCode()}";

        var tone = outcome switch
        {
            OutcomeTier.Good => EmailTone.Enthusiastic,
            OutcomeTier.Expected => EmailTone.Professional,
            OutcomeTier.Bad => EmailTone.Panicked,
            _ => EmailTone.Aloof
        };

        var context = new EmailContentContext(tone, SenderArchetype.EngManager, _seed, eventId, quarterNumber);

        var subject = outcome switch
        {
            OutcomeTier.Good => $"Re: {crisisTitle} - Situation Resolved ✓",
            OutcomeTier.Expected => $"Re: {crisisTitle} - Update",
            OutcomeTier.Bad => $"Re: {crisisTitle} - We Need to Talk",
            _ => $"Re: {crisisTitle} - Follow-up"
        };

        // Get the resolution body from content provider
        var resolutionBody = _contentProvider.GetCrisisResolutionBody(context, crisisTitle, choiceLabel, outcome);

        // Format meter impacts
        var impactText = "";
        if (meterDeltas.Count > 0)
        {
            var impacts = meterDeltas.Select(m =>
                $"• {m.Meter}: {(m.Delta >= 0 ? "+" : "")}{m.Delta}");
            impactText = $"\n\nOrganizational Impact:\n{string.Join("\n", impacts)}";
        }

        var corporateNote = wasCorporateChoice
            ? "\n\n[Note: The board has taken notice of your... pragmatic approach.]"
            : "";

        var closing = _contentProvider.GetClosing(context);

        var body = $"""
            CEO,

            {resolutionBody}{impactText}{corporateNote}

            {closing}
            """;

        var sender = outcome switch
        {
            OutcomeTier.Good => SenderArchetype.PM,
            OutcomeTier.Expected => SenderArchetype.EngManager,
            OutcomeTier.Bad => SenderArchetype.Legal,
            _ => SenderArchetype.EngManager
        };

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        // Create pending effects for display (player must acknowledge)
        var pendingEffects = new PendingProjectEffects(
            MeterChanges: meterDeltas,
            ProfitDelta: 0,  // Crises don't affect profit directly
            EvilScoreDelta: evilScoreDelta,
            OutcomeText: outcome.ToString());

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification,
            PendingEffects: pendingEffects);
    }

    /// <summary>
    /// Creates a reply message for crisis resolution to add to existing thread.
    /// </summary>
    public EmailMessage CreateCrisisResolutionReply(
        string threadId,
        string threadSubject,
        string crisisTitle,
        string choiceLabel,
        OutcomeTier outcome,
        IReadOnlyList<(Meter Meter, int Delta)> meterDeltas,
        int quarterNumber,
        bool wasCorporateChoice = false)
    {
        var eventId = $"crisis_resolution_{quarterNumber}_{crisisTitle.GetHashCode()}";

        var tone = outcome switch
        {
            OutcomeTier.Good => EmailTone.Enthusiastic,
            OutcomeTier.Expected => EmailTone.Professional,
            OutcomeTier.Bad => EmailTone.Panicked,
            _ => EmailTone.Aloof
        };

        var context = new EmailContentContext(tone, SenderArchetype.EngManager, _seed, eventId, quarterNumber);

        var statusTag = outcome switch
        {
            OutcomeTier.Good => "[RESOLVED ✓]",
            OutcomeTier.Expected => "[UPDATE]",
            OutcomeTier.Bad => "[ESCALATED]",
            _ => "[UPDATE]"
        };

        // Get the resolution body from content provider
        var resolutionBody = _contentProvider.GetCrisisResolutionBody(context, crisisTitle, choiceLabel, outcome);

        // Format meter impacts
        var impactText = "";
        if (meterDeltas.Count > 0)
        {
            var impacts = meterDeltas.Select(m =>
                $"• {m.Meter}: {(m.Delta >= 0 ? "+" : "")}{m.Delta}");
            impactText = $"\n\nOrganizational Impact:\n{string.Join("\n", impacts)}";
        }

        var corporateNote = wasCorporateChoice
            ? "\n\n[Note: The board has taken notice of your... pragmatic approach.]"
            : "";

        var closing = _contentProvider.GetClosing(context);

        var body = $"""
            CEO,

            {statusTag}

            {resolutionBody}{impactText}{corporateNote}

            {closing}
            """;

        var sender = outcome switch
        {
            OutcomeTier.Good => SenderArchetype.PM,
            OutcomeTier.Expected => SenderArchetype.EngManager,
            OutcomeTier.Bad => SenderArchetype.Legal,
            _ => SenderArchetype.EngManager
        };

        return new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: $"RE: {threadSubject}",
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: tone,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });
    }

    // === PC Spending Emails ===

    /// <summary>
    /// Creates an email thread for spending PC to boost a meter.
    /// </summary>
    public EmailThread CreateMeterBoostThread(
        Meter meter,
        int oldValue,
        int newValue,
        int quarterNumber)
    {
        var threadId = GenerateThreadId();
        var eventId = $"meter_boost_{meter}_{quarterNumber}";

        var sender = EmailTemplates.GetSenderForMeter(meter);
        var meterName = meter.ToString();

        var subject = $"Re: {meterName} Initiative Update";

        var openings = new[]
        {
            $"Good news from the {meterName} front!",
            $"Per your directive, we've reallocated resources to {meterName}.",
            $"Following up on your {meterName} prioritization request.",
            $"The team has responded positively to your {meterName} focus.",
        };

        var bodies = new[]
        {
            $"I'm pleased to report that your investment of political capital has paid off. Our {meterName} metrics have improved from {oldValue} to {newValue}.\n\nThe team appreciates the executive attention on this area. Sometimes, showing you care is half the battle.",
            $"Your leadership visibility on {meterName} has had an immediate impact. We've moved the needle from {oldValue} to {newValue}.\n\nAs they say, what gets measured gets managed. And what gets CEO attention gets prioritized.",
            $"Your strategic intervention has successfully moved our {meterName} score from {oldValue} to {newValue}.\n\nIt's amazing what a little executive focus can accomplish. The team is energized.",
        };

        var hash = Math.Abs(HashCode.Combine(_seed, eventId, quarterNumber));
        var opening = openings[hash % openings.Length];
        var bodyTemplate = bodies[(hash / openings.Length) % bodies.Length];

        var body = $"""
            CEO,

            {opening}

            {bodyTemplate}

            Best regards
            """;

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Professional,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }

    /// <summary>
    /// Creates a humorous email thread for schmoozing the board (success or failure).
    /// </summary>
    public EmailThread CreateSchmoozingThread(
        bool success,
        int favorabilityChange,
        int newFavorability,
        int quarterNumber)
    {
        var threadId = GenerateThreadId();
        var eventId = $"schmooze_{(success ? "success" : "fail")}_{quarterNumber}";

        var templates = success
            ? Content.EmailTemplateLoader.Templates.SchmoozeSuccess
            : Content.EmailTemplateLoader.Templates.SchmoozeFailure;

        var subject = Content.EmailTemplateLoader.SelectTemplate(templates.Subjects, _seed, eventId);
        var bodyTemplate = Content.EmailTemplateLoader.SelectTemplate(templates.Bodies, _seed, eventId + "_body");

        var substitutions = new Dictionary<string, string>
        {
            ["change"] = Math.Abs(favorabilityChange).ToString(),
            ["newFav"] = newFavorability.ToString(),
            ["quarterNumber"] = quarterNumber.ToString()
        };
        var body = Content.EmailTemplateLoader.ApplySubstitutions(bodyTemplate, substitutions);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.CFO,
            To: SenderArchetype.CEO,
            Tone: success ? EmailTone.Obsequious : EmailTone.Passive,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }

    /// <summary>
    /// Creates a humorous email thread for hand re-org (discarding and drawing new projects).
    /// </summary>
    public EmailThread CreateReorgThread(
        int cardsDiscarded,
        int cardsDrawn,
        int quarterNumber)
    {
        var threadId = GenerateThreadId();
        var eventId = $"reorg_{quarterNumber}";

        var templates = Content.EmailTemplateLoader.Templates.Reorg;
        var subject = Content.EmailTemplateLoader.SelectTemplate(templates.Subjects, _seed, eventId);
        var bodyTemplate = Content.EmailTemplateLoader.SelectTemplate(templates.Bodies, _seed, eventId + "_body");

        var substitutions = new Dictionary<string, string>
        {
            ["cardsDiscarded"] = cardsDiscarded.ToString(),
            ["cardsDrawn"] = cardsDrawn.ToString(),
            ["quarterNumber"] = quarterNumber.ToString()
        };
        var body = Content.EmailTemplateLoader.ApplySubstitutions(bodyTemplate, substitutions);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.EngManager,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Aloof,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }

    /// <summary>
    /// Creates a humorous email thread for image rehabilitation (reducing evil score).
    /// </summary>
    public EmailThread CreateImageRehabThread(
        int oldEvilScore,
        int newEvilScore,
        int quarterNumber)
    {
        var threadId = GenerateThreadId();
        var eventId = $"image_rehab_{quarterNumber}";

        var templates = Content.EmailTemplateLoader.Templates.ImageRehab;
        var subject = Content.EmailTemplateLoader.SelectTemplate(templates.Subjects, _seed, eventId);
        var bodyTemplate = Content.EmailTemplateLoader.SelectTemplate(templates.Bodies, _seed, eventId + "_body");

        var substitutions = new Dictionary<string, string>
        {
            ["oldEvil"] = oldEvilScore.ToString(),
            ["newEvil"] = newEvilScore.ToString(),
            ["quarterNumber"] = quarterNumber.ToString()
        };
        var body = Content.EmailTemplateLoader.ApplySubstitutions(bodyTemplate, substitutions);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: SenderArchetype.Marketing,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Enthusiastic,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }

    // === Misinterpretation support ===

    /// <summary>
    /// Calculates misinterpretation chance based on alignment and pressure.
    /// </summary>
    public static int CalculateMisinterpretationChance(int alignment, int boardPressure)
    {
        var alignmentModifier = 50 - alignment;
        var pressureModifier = (boardPressure - 1) * 5;
        return Math.Clamp(10 + alignmentModifier + pressureModifier, 0, 80);
    }

    /// <summary>
    /// Determines if a message should be misinterpreted.
    /// Uses deterministic selection based on eventId.
    /// </summary>
    public bool ShouldMisinterpret(int alignment, int boardPressure, string eventId)
    {
        var chance = CalculateMisinterpretationChance(alignment, boardPressure);
        var hash = Math.Abs(HashCode.Combine(_seed, eventId, "misinterpret"));
        return (hash % 100) < chance;
    }

    // === Suck-Up Email ===

    /// <summary>
    /// Creates a sycophantic email thread from management when no crisis occurs.
    /// References silly KPIs to highlight the absurdity of corporate metrics.
    /// </summary>
    public EmailThread CreateSuckUpThread(int quarterNumber, string sillyKpiName, IRng rng)
    {
        var threadId = GenerateThreadId();
        var eventId = $"suckup_q{quarterNumber}";

        var (sender, subject, body) = Content.SuckUpEmails.GetRandomEmail(rng, sillyKpiName);

        var message = new EmailMessage(
            MessageId: GenerateMessageId(),
            ThreadId: threadId,
            Subject: subject,
            Body: body + $"\n\n{sender}",
            From: SenderArchetype.PM,  // Will be overridden by sender name in body
            To: SenderArchetype.CEO,
            Tone: EmailTone.Obsequious,
            TurnNumber: quarterNumber,
            LinkedEventIds: new[] { eventId });

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: eventId,
            CreatedOnTurn: quarterNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Notification);
    }
}
