using InertiCorp.Core.Situation;

namespace InertiCorp.Core.Content;

/// <summary>
/// Contains all situation definitions for the game.
/// </summary>
public static class SituationContent
{
    /// <summary>
    /// All situations in the game.
    /// </summary>
    public static IReadOnlyDictionary<string, SituationDefinition> All => _situations;

    private static readonly Dictionary<string, SituationDefinition> _situations = new()
    {
        // === LAYOFFS SITUATIONS ===
        [SIT_UNION_ORGANIZING] = new SituationDefinition(
            SituationId: SIT_UNION_ORGANIZING,
            Title: "Union Organizing Effort",
            Description: "Workers are organizing. The labor relations team is in a panic.",
            EmailSubject: "RE: URGENT - Labor Relations Matter",
            EmailBody: "Following recent restructuring activities, we've detected coordinated organizing efforts among employees. This could significantly impact operations and labor costs. We need direction on how to proceed.",
            Severity: SituationSeverity.Major,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Hire labor relations consultants",
                    "Bring in experts to manage the situation professionally",
                    pcCost: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Governance, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Runway, -10), new MeterEffect(Meter.Morale, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Runway, -15) })),
                SituationResponse.CreateRisk(
                    "Call an all-hands meeting",
                    "Address concerns directly - might calm things down or backfire",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Delivery, -10) })),
                SituationResponse.CreateEvil(
                    "Identify and reassign organizers",
                    "Use performance management to scatter the movement",
                    evilDelta: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, -30), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Governance, -15), new FineEffect(15, "NLRB violation settlement") })),
                SituationResponse.CreateDefer()
            }),

        [SIT_KEY_PERFORMER_QUITS] = new SituationDefinition(
            SituationId: SIT_KEY_PERFORMER_QUITS,
            Title: "Key Performer Resignation",
            Description: "A critical team member has handed in their notice.",
            EmailSubject: "RE: Resignation Notice - Critical Role",
            EmailBody: "One of our most valuable contributors has submitted their two weeks' notice. Their departure could significantly impact current projects and team morale.",
            Severity: SituationSeverity.Moderate,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Make a retention counter-offer",
                    "Throw money at the problem - expensive but might work",
                    pcCost: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Delivery, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -10) })),
                SituationResponse.CreateRisk(
                    "Conduct an exit interview",
                    "Learn what went wrong and do damage control",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -15) })),
                SituationResponse.CreateEvil(
                    "Enforce non-compete aggressively",
                    "Make an example of them to discourage others",
                    evilDelta: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Delivery, -15), new FineEffect(8, "Wrongful termination settlement") })),
                SituationResponse.CreateDefer()
            }),

        [SIT_GLASSDOOR_FIRESTORM] = new SituationDefinition(
            SituationId: SIT_GLASSDOOR_FIRESTORM,
            Title: "Glassdoor Review Firestorm",
            Description: "Negative reviews are flooding in and going viral.",
            EmailSubject: "RE: Social Media Alert - Employer Brand",
            EmailBody: "Our Glassdoor rating has dropped significantly due to a wave of negative reviews. Some are being shared on social media. The recruiting team is concerned about candidate pipeline impact.",
            Severity: SituationSeverity.Minor,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Hire a PR firm",
                    "Professional reputation management",
                    pcCost: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Runway, -10) })),
                SituationResponse.CreateRisk(
                    "Post a CEO response",
                    "Acknowledge the feedback publicly",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10) })),
                SituationResponse.CreateEvil(
                    "Flag reviews for removal",
                    "Use legal threats to suppress negative reviews",
                    evilDelta: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new FineEffect(3, "Legal fees") },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -5), new FineEffect(8, "Anti-SLAPP countersuit") })),
                SituationResponse.CreateDefer()
            }),

        // === DATA/PRIVACY SITUATIONS ===
        [SIT_PRIVACY_INVESTIGATION] = new SituationDefinition(
            SituationId: SIT_PRIVACY_INVESTIGATION,
            Title: "Privacy Regulator Investigation",
            Description: "A government agency is looking into our data practices.",
            EmailSubject: "RE: CONFIDENTIAL - Regulatory Inquiry",
            EmailBody: "We've received a formal inquiry from the data protection authority regarding our customer data handling practices. Legal is requesting immediate guidance on how to proceed.",
            Severity: SituationSeverity.Critical,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Engage regulatory counsel",
                    "Bring in specialized lawyers - this is serious",
                    pcCost: 3,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Alignment, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Runway, -20) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Runway, -30), new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Alignment, -10) })),
                SituationResponse.CreateRisk(
                    "Cooperate transparently",
                    "Open the books and hope for the best",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Alignment, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Governance, -5), new MeterEffect(Meter.Runway, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Alignment, -15) })),
                SituationResponse.CreateEvil(
                    "Stall and destroy evidence",
                    "Delay tactics while scrubbing problematic records",
                    evilDelta: 3,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Runway, -10), new FineEffect(10, "Regulatory fine") },
                        Bad: new IEffect[] { new MeterEffect(Meter.Governance, -35), new MeterEffect(Meter.Runway, -35), new MeterEffect(Meter.Alignment, -30), new FineEffect(50, "GDPR/Privacy Act penalty") })),
                SituationResponse.CreateDefer()
            }),

        // === TECH/OPERATIONS SITUATIONS ===
        [SIT_DATA_MIGRATION_DISASTER] = new SituationDefinition(
            SituationId: SIT_DATA_MIGRATION_DISASTER,
            Title: "Data Migration Disaster",
            Description: "The migration went sideways. Data is corrupted or missing.",
            EmailSubject: "RE: CRITICAL - Data Integrity Issue",
            EmailBody: "The recent migration has resulted in significant data inconsistencies. Some customer records appear corrupted and historical data is missing. Engineering is assessing the damage.",
            Severity: SituationSeverity.Major,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Bring in disaster recovery experts",
                    "External specialists to salvage the situation",
                    pcCost: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Governance, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -5), new MeterEffect(Meter.Runway, -15) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, -5) })),
                SituationResponse.CreateRisk(
                    "All-hands recovery effort",
                    "Rally the troops for an intensive fix",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -15) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -25), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -10) })),
                SituationResponse.CreateEvil(
                    "Blame the vendor",
                    "Point fingers elsewhere and minimize disclosure",
                    evilDelta: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Governance, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Delivery, -10), new FineEffect(12, "Vendor lawsuit settlement") })),
                SituationResponse.CreateDefer()
            }),

        [SIT_SECURITY_VULNERABILITY] = new SituationDefinition(
            SituationId: SIT_SECURITY_VULNERABILITY,
            Title: "Security Vulnerability Exposed",
            Description: "A researcher found a significant security flaw in our systems.",
            EmailSubject: "RE: Security Advisory - Responsible Disclosure",
            EmailBody: "A security researcher has contacted us about a vulnerability they discovered. They're giving us 90 days before public disclosure. The CISO is requesting an emergency budget allocation.",
            Severity: SituationSeverity.Major,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Emergency security audit",
                    "Comprehensive review and remediation",
                    pcCost: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Delivery, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Runway, -15) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Governance, -5), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Alignment, -10) })),
                SituationResponse.CreateRisk(
                    "Internal patch effort",
                    "Have our team fix it quickly",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Morale, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Governance, -5), new MeterEffect(Meter.Delivery, -15) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Delivery, -10) })),
                SituationResponse.CreateEvil(
                    "Threaten the researcher",
                    "Legal threats to buy time or suppress disclosure",
                    evilDelta: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Alignment, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -10), new FineEffect(5, "Legal fees") },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -30), new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Runway, -15), new FineEffect(25, "Data breach class action settlement") })),
                SituationResponse.CreateDefer()
            }),

        // === POSITIVE SITUATIONS ===
        [SIT_TECH_PRESS_RECOGNITION] = new SituationDefinition(
            SituationId: SIT_TECH_PRESS_RECOGNITION,
            Title: "Tech Press Recognition",
            Description: "A major publication wants to feature our innovation.",
            EmailSubject: "RE: Media Opportunity - Feature Article",
            EmailBody: "TechCrunch/Wired/etc. has reached out about featuring our recent technical achievements. This could be great for employer branding and market positioning.",
            Severity: SituationSeverity.Minor,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Professional media training",
                    "Prepare executives for maximum impact",
                    pcCost: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, -5) })),
                SituationResponse.CreateRisk(
                    "Accept the interview",
                    "Go with the flow and hope for good coverage",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5) })),
                SituationResponse.CreateEvil(
                    "Oversell and exaggerate",
                    "Make claims we can't quite back up",
                    evilDelta: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20), new ProfitEffect(10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Governance, -5) })),
                SituationResponse.CreateDefer()
            }),

        [SIT_EMPLOYEE_ENGAGEMENT_BOOST] = new SituationDefinition(
            SituationId: SIT_EMPLOYEE_ENGAGEMENT_BOOST,
            Title: "Employee Engagement Boost",
            Description: "The team is fired up after a successful initiative.",
            EmailSubject: "RE: Great Feedback from Town Hall",
            EmailBody: "The recent town hall generated overwhelmingly positive feedback. Employees are energized and engagement scores have spiked. HR is asking how we want to capitalize on this momentum.",
            Severity: SituationSeverity.Minor,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Launch a celebration event",
                    "Invest in team bonding",
                    pcCost: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 5) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -10) })),
                SituationResponse.CreateRisk(
                    "Send a company-wide thank you",
                    "Acknowledge the positive energy",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Morale, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, 0) })),
                SituationResponse.CreateEvil(
                    "Push for overtime while morale is high",
                    "Strike while the iron is hot",
                    evilDelta: 1,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Morale, -5) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15) })),
                SituationResponse.CreateDefer()
            }),

        // === RTO SITUATIONS ===
        [SIT_MASS_RESIGNATION] = new SituationDefinition(
            SituationId: SIT_MASS_RESIGNATION,
            Title: "Mass Resignation Wave",
            Description: "Multiple employees are quitting in protest.",
            EmailSubject: "RE: CRITICAL - Voluntary Turnover Spike",
            EmailBody: "We're seeing a significant uptick in voluntary departures, particularly among senior contributors. Exit interviews cite recent policy changes as a primary factor.",
            Severity: SituationSeverity.Critical,
            Responses: new[]
            {
                SituationResponse.CreatePC(
                    "Emergency retention bonuses",
                    "Throw money at the problem - this is an emergency",
                    pcCost: 3,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Delivery, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -10) })),
                SituationResponse.CreateRisk(
                    "Reverse the policy",
                    "Admit the mistake and backtrack",
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Morale, 20), new MeterEffect(Meter.Delivery, 10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Delivery, -10) },
                        Bad: new IEffect[] { new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Delivery, -25) })),
                SituationResponse.CreateEvil(
                    "Accelerate replacements",
                    "Use this as an opportunity to refresh with cheaper talent",
                    evilDelta: 2,
                    outcomes: new OutcomeProfile(
                        Good: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Delivery, -10) },
                        Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10), new FineEffect(5, "Severance disputes") },
                        Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -30), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Alignment, -15), new FineEffect(20, "Age discrimination settlement") })),
                SituationResponse.CreateDefer()
            }),
    };

    // Situation IDs as constants
    public const string SIT_UNION_ORGANIZING = "SIT_UNION_ORGANIZING";
    public const string SIT_KEY_PERFORMER_QUITS = "SIT_KEY_PERFORMER_QUITS";
    public const string SIT_GLASSDOOR_FIRESTORM = "SIT_GLASSDOOR_FIRESTORM";
    public const string SIT_PRIVACY_INVESTIGATION = "SIT_PRIVACY_INVESTIGATION";
    public const string SIT_DATA_MIGRATION_DISASTER = "SIT_DATA_MIGRATION_DISASTER";
    public const string SIT_SECURITY_VULNERABILITY = "SIT_SECURITY_VULNERABILITY";
    public const string SIT_TECH_PRESS_RECOGNITION = "SIT_TECH_PRESS_RECOGNITION";
    public const string SIT_EMPLOYEE_ENGAGEMENT_BOOST = "SIT_EMPLOYEE_ENGAGEMENT_BOOST";
    public const string SIT_MASS_RESIGNATION = "SIT_MASS_RESIGNATION";

    /// <summary>
    /// Gets a situation by ID.
    /// </summary>
    public static SituationDefinition? Get(string situationId) =>
        _situations.TryGetValue(situationId, out var situation) ? situation : null;
}
