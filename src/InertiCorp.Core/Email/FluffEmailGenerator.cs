namespace InertiCorp.Core.Email;

/// <summary>
/// Generates random "fluff" emails - corporate noise that doesn't require any response.
/// Includes BS reports, corporate suck-ups, sales pitches, and other bureaucratic detritus.
/// </summary>
public sealed class FluffEmailGenerator
{
    private readonly Random _rng;
    private int _counter;

    public FluffEmailGenerator(int seed)
    {
        _rng = new Random(seed);
        _counter = 0;
    }

    /// <summary>
    /// Generates a random fluff email thread.
    /// </summary>
    public EmailThread GenerateFluffEmail(int turnNumber)
    {
        var fluffType = (FluffType)_rng.Next(4);

        return fluffType switch
        {
            FluffType.MetricsReport => CreateMetricsReport(turnNumber),
            FluffType.SuckUp => CreateSuckUpEmail(turnNumber),
            FluffType.SalesPitch => CreateSalesPitch(turnNumber),
            FluffType.Announcement => CreateAnnouncement(turnNumber),
            _ => CreateMetricsReport(turnNumber)
        };
    }

    private EmailThread CreateMetricsReport(int turnNumber)
    {
        var subjects = new[]
        {
            "Q{0} Synergy Metrics Dashboard",
            "Weekly KPI Alignment Report",
            "Cross-Functional Performance Analytics",
            "Strategic Initiative Progress Update",
            "Operational Excellence Scorecard",
            "Enterprise Velocity Metrics",
            "Stakeholder Engagement Index Update",
            "Digital Transformation Progress Report"
        };

        var bodies = new[]
        {
            "Attached please find the latest metrics dashboard showcasing our continued " +
            "journey toward operational excellence. Key highlights:\n\n" +
            "• Synergy Index: Up 12% (target exceeded)\n" +
            "• Cross-functional Alignment: 94.7%\n" +
            "• Innovation Velocity: Trending positive\n" +
            "• Stakeholder Satisfaction: Within acceptable parameters\n\n" +
            "Please review at your earliest convenience. No action required.",

            "Per our commitment to data-driven decision making, here are this week's KPIs:\n\n" +
            "• Team Productivity Score: 8.2/10\n" +
            "• Resource Utilization: 87%\n" +
            "• Sprint Velocity: Nominal\n" +
            "• Technical Debt Ratio: Manageable\n\n" +
            "Full dashboard available on the intranet.",

            "This is your automated report from the Enterprise Performance Management System.\n\n" +
            "All metrics are GREEN. No anomalies detected.\n\n" +
            "Remember: What gets measured, gets managed!\n\n" +
            "[This is an automated message - please do not reply]",

            "Exciting news! Our Holistic Performance Framework shows continued improvement:\n\n" +
            "• Employee NPS: Stable\n" +
            "• Process Efficiency: Optimized\n" +
            "• Strategic Alignment Score: 92%\n" +
            "• Culture Health Index: Thriving\n\n" +
            "Great work, team! Keep leveraging those synergies!"
        };

        return CreateFluffThread(
            subjects[_rng.Next(subjects.Length)],
            bodies[_rng.Next(bodies.Length)],
            GetRandomInternalSender(),
            turnNumber);
    }

    private EmailThread CreateSuckUpEmail(int turnNumber)
    {
        var subjects = new[]
        {
            "RE: Your Brilliant Strategy",
            "Inspired by Your Leadership",
            "Thank You for Your Vision",
            "Your Guidance Made the Difference",
            "Grateful for Your Direction",
            "Following Up on Your Excellent Idea"
        };

        var bodies = new[]
        {
            "I just wanted to take a moment to express how inspired I am by your " +
            "strategic vision. The way you handled that last initiative was truly " +
            "masterful. The team is energized and aligned thanks to your leadership.\n\n" +
            "Looking forward to continuing to execute on your brilliant strategy!",

            "Your comments in the all-hands really resonated with me. It's rare to see " +
            "a CEO with such a clear understanding of both the big picture AND the details. " +
            "I've already incorporated your insights into my team's approach.\n\n" +
            "Thank you for being such an inspiring leader!",

            "I've been reflecting on your recent guidance, and I have to say - it's " +
            "exactly what we needed to hear. Your ability to cut through complexity " +
            "and identify what matters is remarkable.\n\n" +
            "The team morale has never been higher!",

            "Just a quick note to say how much I appreciate your hands-on approach. " +
            "Not every executive takes the time to understand the ground-level challenges. " +
            "Your leadership style is refreshing and effective.\n\n" +
            "Honored to be part of your team!",

            "I shared your strategic framework with my team and they were blown away. " +
            "\"This is exactly what we needed,\" they said. Your vision is truly " +
            "transformational.\n\n" +
            "Can't wait to see what we accomplish together!"
        };

        return CreateFluffThread(
            subjects[_rng.Next(subjects.Length)],
            bodies[_rng.Next(bodies.Length)],
            GetRandomInternalSender(),
            turnNumber);
    }

    private EmailThread CreateSalesPitch(int turnNumber)
    {
        var subjects = new[]
        {
            "Exclusive Offer for InertiCorp Executives",
            "Transform Your Enterprise with AI-Powered Solutions",
            "Quick Question About Your Q{0} Goals",
            "Saw Your Recent Success - Let's Talk Scaling",
            "[URGENT] Limited Time Partnership Opportunity",
            "Are You Leaving Money on the Table?",
            "Your Competitors Are Already Using This..."
        };

        var bodies = new[]
        {
            "Hi [CEO Name],\n\n" +
            "I noticed InertiCorp has been making waves in the industry. " +
            "Our AI-powered Enterprise Synergy Platform has helped 500+ companies " +
            "increase their operational efficiency by up to 340%.\n\n" +
            "Would you be open to a quick 15-minute call to explore how we could " +
            "replicate these results for InertiCorp?\n\n" +
            "Best regards,\n" +
            "Chad Salesington\n" +
            "Enterprise Account Executive\n" +
            "SynergyMax Solutions Inc.",

            "Dear Executive Team,\n\n" +
            "STOP leaving productivity on the table. Our BlockchainCloud™ platform " +
            "is revolutionizing how enterprises operate.\n\n" +
            "Features include:\n" +
            "• AI-Driven Synergy Optimization\n" +
            "• Quantum-Ready Infrastructure\n" +
            "• Metaverse Integration Roadmap\n\n" +
            "Act now - pricing increases next month!\n\n" +
            "Reply YES to schedule a demo.",

            "I hope this email finds you well!\n\n" +
            "I've been following InertiCorp's journey and I'm impressed. " +
            "That's why I wanted to reach out personally about our Strategic " +
            "Consulting Engagement Program.\n\n" +
            "We've helped 3 of your direct competitors achieve 50% cost reduction. " +
            "Shouldn't you find out how?\n\n" +
            "P.S. - I'm persistent because I genuinely believe we can help.",

            "Subject: Re: Re: Re: Following Up\n\n" +
            "Just bumping this to the top of your inbox! I know you're busy " +
            "but I truly believe our Enterprise Excellence Suite could be a " +
            "game-changer for InertiCorp.\n\n" +
            "I'll try calling your office again tomorrow. Looking forward to connecting!\n\n" +
            "Best,\n" +
            "The Persistent Team at VendorForce Solutions"
        };

        return CreateFluffThread(
            subjects[_rng.Next(subjects.Length)],
            bodies[_rng.Next(bodies.Length)],
            SenderArchetype.Anonymous, // External vendor
            turnNumber);
    }

    private EmailThread CreateAnnouncement(int turnNumber)
    {
        var subjects = new[]
        {
            "[All-Staff] Parking Lot Maintenance Notice",
            "Updated PTO Policy - Please Review",
            "New Coffee Machine in Break Room 3!",
            "Reminder: Mandatory Training Due",
            "Building A Elevator Maintenance",
            "Lost & Found: Someone Left a Plant",
            "IT Notice: Password Reset Required",
            "Wellness Wednesday Reminder"
        };

        var bodies = new[]
        {
            "Please be advised that the east parking lot will be undergoing " +
            "maintenance this weekend. Please use the west lot during this time.\n\n" +
            "We apologize for any inconvenience.\n\n" +
            "- Facilities Management",

            "Hi everyone!\n\n" +
            "Great news - we've upgraded the coffee machine in Break Room 3! " +
            "It now features cold brew, oat milk options, and a \"executive blend\" " +
            "setting.\n\n" +
            "Enjoy responsibly!\n\n" +
            "- Office Happiness Committee",

            "This is a reminder that the annual Workplace Safety training must be " +
            "completed by end of quarter. Please log in to LearnForce Pro to " +
            "complete your modules.\n\n" +
            "Completion is mandatory for all employees.\n\n" +
            "- HR Training Department",

            "Someone left a lovely potted fern in Conference Room B. " +
            "If this belongs to you, please come claim it from the reception desk.\n\n" +
            "The plant is being well cared for in the meantime.\n\n" +
            "- Reception",

            "Wellness Wednesday is coming up! Join us for:\n\n" +
            "• 10am: Chair yoga in the lobby\n" +
            "• 12pm: Healthy lunch options in cafeteria\n" +
            "• 3pm: Mindfulness session (Zoom link TBD)\n\n" +
            "Your wellbeing matters to us!\n\n" +
            "- The Wellness Team"
        };

        return CreateFluffThread(
            subjects[_rng.Next(subjects.Length)],
            bodies[_rng.Next(bodies.Length)],
            SenderArchetype.HR,
            turnNumber);
    }

    private EmailThread CreateFluffThread(string subjectTemplate, string body, SenderArchetype sender, int turnNumber)
    {
        var threadId = $"fluff_{++_counter}_{turnNumber}";
        var subject = string.Format(subjectTemplate, (turnNumber / 4) + 1); // Convert turn to quarter

        var message = new EmailMessage(
            MessageId: $"msg_{threadId}",
            ThreadId: threadId,
            Subject: subject,
            Body: body,
            From: sender,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Professional,
            TurnNumber: turnNumber,
            LinkedEventIds: Array.Empty<string>());

        return new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: null,
            CreatedOnTurn: turnNumber,
            Messages: new[] { message },
            ThreadType: EmailThreadType.Fluff);
    }

    private SenderArchetype GetRandomInternalSender()
    {
        var senders = new[]
        {
            SenderArchetype.PM,
            SenderArchetype.EngManager,
            SenderArchetype.HR,
            SenderArchetype.Marketing,
            SenderArchetype.CFO,
            SenderArchetype.Compliance
        };
        return senders[_rng.Next(senders.Length)];
    }

    private enum FluffType
    {
        MetricsReport,
        SuckUp,
        SalesPitch,
        Announcement
    }
}
