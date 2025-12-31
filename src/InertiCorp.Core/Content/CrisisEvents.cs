namespace InertiCorp.Core.Content;

/// <summary>
/// Crisis deck cards - bad events requiring damage control.
/// These are drawn during the Crisis phase of each quarter.
/// </summary>
public static class CrisisEvents
{
    // === COMPLIANCE & GOVERNANCE ===

    public static EventCard AuditFinding { get; } = new EventCard(
        "CRISIS_001_AUDIT",
        "Audit Finding",
        "Internal audit found critical compliance gaps. They're demanding immediate action.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_CEO_INTERVENTION", "Personally oversee remediation", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 25), new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Delivery, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, -10) }
                )),
            Choice.Tiered("CHC_IMMEDIATE_REMEDIATION", "Immediate remediation",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Delivery, -10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -15) }
                )),
            Choice.Corporate("CHC_RISK_ACCEPTANCE", "Accept and ignore the risk",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -10), new MeterEffect(Meter.Governance, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, -25) }
                ), corporateIntensityDelta: 2)
        }
    );

    // === TALENT CRISES ===

    public static EventCard KeyPersonQuits { get; } = new EventCard(
        "CRISIS_002_KEY_PERSON",
        "Key Person Quits",
        "Your most critical team member just handed in their resignation. They have unique knowledge.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_CEO_RETENTION", "Personal appeal from you", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 20), new MeterEffect(Meter.Alignment, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -15) }
                )),
            Choice.Tiered("CHC_RETENTION_BONUS", "Offer retention bonus",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Runway, -15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, -20) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Morale, -5) }
                )),
            Choice.Corporate("CHC_NO_NEGOTIATION", "Let them go, no negotiation",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Morale, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -25), new MeterEffect(Meter.Morale, -15) }
                ), corporateIntensityDelta: 1)
        }
    );

    // === TECH FAILURES ===

    public static EventCard ProductionOutage { get; } = new EventCard(
        "CRISIS_003_OUTAGE",
        "Production Outage",
        "A critical production system is down. Revenue is being impacted every minute.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_HIRE_CONSULTANTS", "Fly in expensive consultants", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 25), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Runway, -15) }
                )),
            Choice.Tiered("CHC_ALL_HANDS", "All hands on deck",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Morale, -10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Morale, -20) }
                )),
            Choice.Corporate("CHC_BLAME_VENDOR", "Blame the vendor publicly",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Alignment, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    // === DRUNK MANAGER / PARTY DISASTERS ===

    public static EventCard ChristmasPartyIncident { get; } = new EventCard(
        "CRISIS_004_XMAS_PARTY",
        "Holiday Party Incident",
        "The VP of Sales got extremely drunk at the company holiday party and made inappropriate comments to several employees. Someone recorded it on their phone.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_CEO_APOLOGY_TOUR", "Personal damage control tour", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Alignment, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Alignment, -5) }
                )),
            Choice.Tiered("CHC_FIRE_VP", "Terminate the VP immediately",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Governance, 15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Governance, 5) }
                )),
            Choice.Corporate("CHC_COVER_UP", "Make the video disappear",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -15) }
                ), corporateIntensityDelta: 3)
        }
    );

    public static EventCard ExecutiveKaraoke { get; } = new EventCard(
        "CRISIS_005_KARAOKE",
        "Viral Karaoke Disaster",
        "The CFO's enthusiastic but deeply unfortunate karaoke performance at the team offsite has gone viral on TikTok. The hashtag #CorporateCringe is trending.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_LEAN_INTO_IT", "Hire a PR firm to spin it as 'authentic leadership'", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Runway, -10) }
                )),
            Choice.Tiered("CHC_ISSUE_STATEMENT", "Issue formal statement",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Morale, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -10) }
                )),
            Choice.Corporate("CHC_IGNORE_VIRAL", "Pretend it doesn't exist",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -10) }
                ), corporateIntensityDelta: 1)
        }
    );

    // === INCOMPETENT EMPLOYEES ===

    public static EventCard InternDatabaseDrop { get; } = new EventCard(
        "CRISIS_006_INTERN_DROP",
        "Intern Dropped Production Database",
        "The summer intern was given production access 'to learn' and accidentally ran DROP DATABASE. The backups haven't been tested in 8 months.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_BRING_IN_EXPERTS", "Bring in data recovery specialists", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Runway, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Runway, -20) }
                )),
            Choice.Tiered("CHC_REBUILD_FROM_BACKUP", "Attempt recovery from backups",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Governance, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -30), new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_BLAME_INTERN", "Fire the intern, claim it was malicious",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard ManagerSentAllStaff { get; } = new EventCard(
        "CRISIS_007_REPLY_ALL",
        "Catastrophic Reply-All",
        "A mid-level manager accidentally sent a brutally honest email about layoff plans to the entire company. The email included the phrase 'dead weight employees' and a list of names.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_EMERGENCY_TOWNHALL", "Emergency all-hands meeting", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Alignment, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Alignment, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -5) }
                )),
            Choice.Tiered("CHC_FIRE_MANAGER", "Terminate the manager",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -15) }
                )),
            Choice.Corporate("CHC_DENY_LAYOFFS", "Deny any layoff plans exist",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Governance, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -20) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard ExecutiveExpenseReport { get; } = new EventCard(
        "CRISIS_008_EXPENSES",
        "Questionable Executive Expenses",
        "Finance flagged the VP of Marketing's expense report: $47,000 at a Vegas 'team building' event, including $8,000 for 'client entertainment' at an establishment whose name ends in 'Gentlemen's Club.'",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_QUIET_REPAYMENT", "Quietly arrange repayment", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Runway, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -5), new MeterEffect(Meter.Morale, -10) }
                )),
            Choice.Tiered("CHC_FORMAL_INVESTIGATION", "Launch formal investigation",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -15) }
                )),
            Choice.Corporate("CHC_APPROVE_ANYWAY", "Approve it - we need them",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Morale, -15) }
                ), corporateIntensityDelta: 3)
        }
    );

    // === SENIOR MANAGER INCOMPETENCE ===

    public static EventCard DirectorPresentationDisaster { get; } = new EventCard(
        "CRISIS_009_PRESENTATION",
        "Board Presentation Disaster",
        "The Director of Strategy just presented last year's numbers to the board instead of this year's. They're asking why revenue 'went backwards.' The director is sweating visibly.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_CEO_TAKEOVER", "Take over the presentation personally", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Governance, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Morale, -10) }
                )),
            Choice.Tiered("CHC_CALL_BREAK", "Call an emergency break",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Alignment, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Alignment, -15) }
                )),
            Choice.Corporate("CHC_BLAME_DIRECTOR", "Throw the director under the bus",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Delivery, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard VPWentRogue { get; } = new EventCard(
        "CRISIS_010_ROGUE_VP",
        "VP Announces Unauthorized Partnership",
        "The VP of Business Development just announced a major partnership with a competitor on LinkedIn. Legal wasn't consulted. The board wasn't told. The partner company has no idea what she's talking about.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_SAVE_THE_DEAL", "Scramble to actually close the deal", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20), new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Runway, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, -15) }
                )),
            Choice.Tiered("CHC_PUBLIC_RETRACTION", "Issue public retraction",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Alignment, -5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Morale, -15) }
                )),
            Choice.Corporate("CHC_GASLIGHT", "Claim it was 'aspirational'",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Governance, -20) }
                ), corporateIntensityDelta: 2)
        }
    );

    // === HR DISASTERS ===

    public static EventCard HRSystemLeak { get; } = new EventCard(
        "CRISIS_011_HR_LEAK",
        "Salary Data Leaked",
        "Someone posted a spreadsheet with everyone's salaries to the company Slack. The pay disparities are... significant. Several employees are demanding explanations.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_SALARY_ADJUSTMENT", "Announce immediate pay equity review", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 20), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Governance, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -25) }
                )),
            Choice.Tiered("CHC_TRANSPARENCY_APPROACH", "Embrace radical transparency",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Alignment, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_HUNT_LEAKER", "Launch investigation to find the leaker",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Governance, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, -30), new MeterEffect(Meter.Alignment, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard OffsiteFoodPoisoning { get; } = new EventCard(
        "CRISIS_012_FOOD_POISON",
        "Leadership Offsite Food Poisoning",
        "The entire executive team got food poisoning at the leadership offsite. Half of them are in the hospital. The company has no succession plan and a board meeting tomorrow.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_HIRE_INTERIM", "Hire interim executives immediately", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Delivery, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Tiered("CHC_POSTPONE_MEETING", "Request board meeting postponement",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Alignment, -20) }
                )),
            Choice.Corporate("CHC_WEEKEND_AT_BERNIES", "Prop them up on Zoom, claim 'connection issues'",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -15) }
                ), corporateIntensityDelta: 3)
        }
    );

    // === TECH INCOMPETENCE ===

    public static EventCard AutomationGoneWrong { get; } = new EventCard(
        "CRISIS_013_AUTOMATION",
        "Automated Email Gone Wrong",
        "The marketing team's 'personalized' email campaign has a bug. It sent 50,000 customers an email starting with 'Dear [CUSTOMER_NAME], your order of [PRODUCT_NAME] will arrive [ERROR_NULL_REFERENCE]...'",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_APOLOGY_DISCOUNT", "Send apology with 20% discount", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, -15) }
                )),
            Choice.Tiered("CHC_APOLOGIZE_SIMPLE", "Send simple apology email",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Governance, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Governance, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -5) }
                )),
            Choice.Corporate("CHC_PRETEND_PHISHING", "Claim it was a phishing test",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Governance, -20) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard AIBotGoneRogue { get; } = new EventCard(
        "CRISIS_014_AI_BOT",
        "Customer Service Bot Malfunction",
        "The new AI customer service bot has started telling customers to 'seek professional help' and 'consider their life choices' when they complain. Screenshots are everywhere.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_TURN_INTO_MARKETING", "Hire crisis PR to make it a 'viral moment'", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Governance, -10) }
                )),
            Choice.Tiered("CHC_DISABLE_BOT", "Disable the bot, issue apology",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Delivery, -15) }
                )),
            Choice.Corporate("CHC_BOT_WAS_RIGHT", "Double down - the bot has a point",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Governance, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -30), new MeterEffect(Meter.Governance, -25) }
                ), corporateIntensityDelta: 3)
        }
    );

    // === PR DISASTERS ===

    public static EventCard CEOTwitterHacked { get; } = new EventCard(
        "CRISIS_015_TWITTER",
        "Corporate Social Media Compromised",
        "The company Twitter account posted 'We hate our customers almost as much as we hate working here.' Social media manager claims they were 'hacked' but IT can't find evidence of intrusion.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_HUMOROUS_RESPONSE", "Hire comedy writers for humorous response", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20), new MeterEffect(Meter.Morale, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, 5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Governance, -10) }
                )),
            Choice.Tiered("CHC_FIRE_SOCIAL_MANAGER", "Fire the social media manager",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Alignment, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_CLAIM_ACTUALLY_HACKED", "Insist it was sophisticated hackers",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Governance, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Governance, -20) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard ProductLaunchFail { get; } = new EventCard(
        "CRISIS_016_LAUNCH_FAIL",
        "Product Demo Goes Catastrophically Wrong",
        "The big product launch demo just crashed live on stage. The engineering VP muttered 'works on my machine' loudly enough for the front row to hear. Investors are present.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_PRIVATE_DEMO", "Arrange private demos with investors", 3,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Runway, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Delivery, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Runway, -10) }
                )),
            Choice.Tiered("CHC_DELAY_LAUNCH", "Delay launch, do it right",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -15) }
                )),
            Choice.Corporate("CHC_SHIP_ANYWAY", "Ship it anyway, fix later",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Delivery, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    // === ABSURD SITUATIONS ===

    public static EventCard OfficePetEmergency { get; } = new EventCard(
        "CRISIS_017_OFFICE_PET",
        "Office Comfort Animal Incident",
        "The 'office comfort animal' policy has backfired. Someone brought an emotional support peacock that attacked the CFO. Legal is asking who approved this policy.",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_PREMIUM_VET", "Pay for the CFO's 'recovery retreat' and peacock rehoming", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Governance, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -15) }
                )),
            Choice.Tiered("CHC_BAN_ANIMALS", "Ban all animals, issue formal policy",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Morale, -5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -15) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_BLAME_CFO", "Suggest CFO provoked the peacock",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Alignment, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -15) }
                ), corporateIntensityDelta: 2)
        }
    );

    public static EventCard CryptoBroExecutive { get; } = new EventCard(
        "CRISIS_018_CRYPTO_BRO",
        "Executive Crypto Scheme Exposed",
        "The Head of Innovation has been mining cryptocurrency on company servers. IT noticed when the electricity bill tripled. He's asking if this can be written off as 'R&D.'",
        new List<Choice>
        {
            Choice.WithPCCost("CHC_QUIET_TERMINATION", "Quiet termination with NDA", 2,
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Runway, 10) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Runway, -5) }
                )),
            Choice.Tiered("CHC_PUBLIC_FIRING", "Very public termination",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Morale, 5) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -5) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Alignment, -10) }
                )),
            Choice.Corporate("CHC_LEGITIMIZE_MINING", "Rebrand as 'Web3 Innovation Initiative'",
                new OutcomeProfile(
                    Good: new IEffect[] { new MeterEffect(Meter.Runway, 15) },
                    Expected: new IEffect[] { new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Runway, -10) },
                    Bad: new IEffect[] { new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Alignment, -15) }
                ), corporateIntensityDelta: 3)
        }
    );

    /// <summary>
    /// All crisis events.
    /// </summary>
    public static IReadOnlyList<EventCard> All { get; } = new[]
    {
        AuditFinding,
        KeyPersonQuits,
        ProductionOutage,
        ChristmasPartyIncident,
        ExecutiveKaraoke,
        InternDatabaseDrop,
        ManagerSentAllStaff,
        ExecutiveExpenseReport,
        DirectorPresentationDisaster,
        VPWentRogue,
        HRSystemLeak,
        OffsiteFoodPoisoning,
        AutomationGoneWrong,
        AIBotGoneRogue,
        CEOTwitterHacked,
        ProductLaunchFail,
        OfficePetEmergency,
        CryptoBroExecutive
    };
}
