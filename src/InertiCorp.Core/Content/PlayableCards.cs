using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Content;

/// <summary>
/// Collection of playable project cards - satirical corporate mega-initiatives.
/// Each represents the kind of massive, expensive project that Fortune 500 companies
/// love to announce and rarely successfully complete.
/// </summary>
public static class PlayableCards
{
    // === DIGITAL TRANSFORMATION ===

    public static PlayableCard DigitalTransformation { get; } = new(
        CardId: "PROJ_DIGITAL_TRANSFORMATION",
        Title: "Digital Transformation",
        Description: "Embark on a company-wide digital transformation journey.",
        FlavorText: "\"We're not sure what it means, but McKinsey said we need it.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Alignment, 10), new ProfitEffect(15) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -10), new ProfitEffect(-5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Morale, -5), new ProfitEffect(-20) }
        ),
        CorporateIntensity: 0,  // Wasteful BS but not evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Nobody knows what 'digital transformation' actually means, but the consultants assured us it's essential. We'll spend $50 million on new software that does exactly what the old software did, but in the cloud. Every department will have to 're-imagine their customer journey' and 'leverage data-driven insights.' The project will take 3x longer than planned and the CEO will declare victory regardless of outcome.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard CloudMigration { get; } = new(
        CardId: "PROJ_CLOUD_MIGRATION",
        Title: "Lift and Shift to Cloud",
        Description: "Migrate all systems to the cloud because that's what modern companies do.",
        FlavorText: "\"It's like our data center, but someone else's problem now.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Governance, 10), new ProfitEffect(10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -15), new ProfitEffect(-10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Governance, -10), new ProfitEffect(-25) }
        ),
        CorporateIntensity: 0,  // Normal tech project
        Category: CardCategory.Action,
        ExtendedDescription: "The board heard 'cloud' at a conference and now everything must move there immediately. We'll take our beautifully optimized on-prem systems and cram them into virtual machines that cost 10x more to run. When the first AWS bill arrives, there will be an emergency meeting. The security team will have concerns. Those concerns will be noted and then ignored.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard AICenter { get; } = new(
        CardId: "PROJ_AI_CENTER",
        Title: "AI Center of Excellence",
        Description: "Establish a dedicated team to explore AI opportunities.",
        FlavorText: "\"We put 'AI' in the press release. Stock up 4%.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 25), new MeterEffect(Meter.Delivery, 5), new ProfitEffect(25) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Delivery, -5), new ProfitEffect(5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10), new ProfitEffect(-15) }
        ),
        CorporateIntensity: 0,  // Hype project but not evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Hire 12 data scientists, give them a cool office space with beanbags, and wait for the magic. They'll spend 6 months cleaning data, 3 months building a model that's 2% better than a spreadsheet, and the rest of the year explaining to executives why we can't just 'add AI to that.' Investors love it though. They don't understand it, but they love it.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard BlockchainPilot { get; } = new(
        CardId: "PROJ_BLOCKCHAIN",
        Title: "Enterprise Blockchain Initiative",
        Description: "Explore blockchain applications for... something. Supply chain? Probably.",
        FlavorText: "\"It's like a database, but slower and more expensive.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Governance, 5), new ProfitEffect(10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Delivery, -10), new ProfitEffect(-8) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, -10), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -15), new ProfitEffect(-18) }
        ),
        CorporateIntensity: 0,  // Waste of money but not evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "The CEO's nephew explained blockchain at Thanksgiving and now we're 'exploring Web3 opportunities.' We'll partner with a startup that will definitely not exist in 18 months, announce a pilot program that will never leave pilot, and issue a press release using the words 'immutable,' 'decentralized,' and 'trustless.' Engineers will quietly build a normal database and tell the board it's 'blockchain-inspired.'",
        MeterAffinity: Meter.Runway
    );

    // === ORGANIZATIONAL CHANGE ===

    public static PlayableCard AgileTransformation { get; } = new(
        CardId: "PROJ_AGILE",
        Title: "Agile at Scale",
        Description: "Transform the entire organization to agile methodology.",
        FlavorText: "\"We added more meetings and called it 'agile.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Morale, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,  // Annoying but not evil
        Category: CardCategory.Action,
        ExtendedDescription: "Hire an army of Scrum Masters. Rename all your teams to 'squads.' Add daily standups to everyone's calendar. Create elaborate Jira workflows that take longer to update than the actual work. Executives will still demand fixed deadlines and detailed upfront plans, but now they'll ask for them 'in an agile way.' Warning: May cause spontaneous eye-rolling among engineers.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard GlobalOperatingModel { get; } = new(
        CardId: "PROJ_GLOBAL_OP_MODEL",
        Title: "Global Operating Model",
        Description: "Redesign the entire company structure for 'efficiency.'",
        FlavorText: "\"We're not laying people off. We're 'right-sizing.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 20), new ProfitEffect(30) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10), new ProfitEffect(10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Alignment, -15), new ProfitEffect(-15), new FineEffect(12, "WARN Act violation") }
        ),
        CorporateIntensity: 4,  // Disguised layoffs = very evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "A massive reorganization disguised as 'operating model optimization.' Consultants will produce a 400-slide deck showing how we can save $200M by 'streamlining' (firing people), 'off-shoring' (firing people here, hiring cheaper people elsewhere), and 'eliminating redundancies' (firing people). Surviving employees will do 3 jobs for the same pay and update their LinkedIn profiles 'just in case.'",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard CultureTransformation { get; } = new(
        CardId: "PROJ_CULTURE",
        Title: "Culture & Values Refresh",
        Description: "Rebrand our corporate values with new posters and a values app.",
        FlavorText: "\"We replaced 'Integrity' with 'Authentic Integrity.' Huge improvement.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Alignment, 15) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,  // Empty gestures, not evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Spend $5 million on new motivational posters, a mandatory culture workshop, and an app where employees can give each other virtual 'values badges.' The new values will be carefully crafted to be as generic as possible: 'Innovation,' 'Customer Focus,' 'Teamwork.' Same values as every other Fortune 500 company. Employees will be required to incorporate the new values into their performance reviews while leadership continues to ignore them entirely.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard WorkplaceOfFuture { get; } = new(
        CardId: "PROJ_WORKPLACE_FUTURE",
        Title: "Workplace of Tomorrow",
        Description: "Redesign all offices with hot-desking and collaboration zones.",
        FlavorText: "\"You don't need a desk. You need an 'experience.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Morale, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 1,  // Annoying employees for cost savings - mildly evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Remove everyone's desks and replace them with 'neighborhoods,' 'collaboration pods,' and a 'rejuvenation zone' that's just beanbags near the emergency exit. Employees will arrive at 6am to claim a good spot, then spend all day on Zoom calls in phone booths the size of coffins. Executives will keep their private offices. Real estate costs go down, resignation rates go up.",
        MeterAffinity: Meter.Runway
    );

    // === TECHNOLOGY INITIATIVES ===

    public static PlayableCard ERPOverhaul { get; } = new(
        CardId: "PROJ_ERP",
        Title: "Enterprise Resource Planning Overhaul",
        Description: "Replace our legacy ERP with a modern solution.",
        FlavorText: "\"The vendor promised it would only take 18 months. That was 4 years ago.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Governance, 15), new ProfitEffect(20) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, -5), new MeterEffect(Meter.Runway, -20), new MeterEffect(Meter.Governance, 5), new ProfitEffect(-15) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -25), new MeterEffect(Meter.Runway, -30), new MeterEffect(Meter.Governance, -15), new ProfitEffect(-35) }
        ),
        CorporateIntensity: 0,  // Normal IT project
        Category: CardCategory.Action,
        ExtendedDescription: "The graveyard of corporate IT is filled with ERP projects. We'll hire an army of consultants who bill more per hour than our doctors, customize the software until it's unrecognizable, and go live on a date chosen for its astrological significance rather than readiness. Finance won't be able to close the books for 3 months. But at least we'll have real-time dashboards nobody looks at.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard Customer360 { get; } = new(
        CardId: "PROJ_CUSTOMER_360",
        Title: "Customer 360 Platform",
        Description: "Build a unified view of all customer data and touchpoints.",
        FlavorText: "\"We'll finally know everything about our customers. Legally.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -15) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Governance, -15), new MeterEffect(Meter.Runway, -20) }
        ),
        CorporateIntensity: 0,  // Normal business project
        Category: CardCategory.Action,
        ExtendedDescription: "Create a 'single source of truth' for customer data by merging 47 different databases that have been accumulating since 1997. The data quality team will discover that 'John Smith' appears 50,000 times and might be anywhere from 1 to 47 different people. The privacy team will have concerns about GDPR. Sales will refuse to use it because their spreadsheets 'just work better.'",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard DataLake { get; } = new(
        CardId: "PROJ_DATA_LAKE",
        Title: "Enterprise Data Lake",
        Description: "Consolidate all company data into a central analytics platform.",
        FlavorText: "\"We have a data lake. It's more of a data swamp, really.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Governance, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 0), new MeterEffect(Meter.Runway, -15) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Runway, -20) }
        ),
        CorporateIntensity: 0,  // Normal IT project
        Category: CardCategory.Action,
        ExtendedDescription: "Dump every piece of data we have into a cloud storage bucket and call it a 'data lake.' Hire a team of data engineers to build pipelines that transform data from formats nobody understands into formats nobody else understands. In two years, we'll have petabytes of data and no one who knows what any of it means. The 'insights' will somehow always confirm what leadership already believed.",
        MeterAffinity: Meter.Governance
    );

    // === HR & PEOPLE ===

    public static PlayableCard EmployeeExperience { get; } = new(
        CardId: "PROJ_EX_PLATFORM",
        Title: "Employee Experience Platform",
        Description: "Deploy a new platform to 'enhance the employee journey.'",
        FlavorText: "\"It's like a social network, but mandatory and nobody wants to use it.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Delivery, -5) }
        ),
        CorporateIntensity: 0,  // Normal HR project
        Category: CardCategory.Action,
        ExtendedDescription: "Replace the 15 different HR systems nobody understands with 1 new HR system nobody will understand. Employees will have a 'personalized dashboard' showing their 'career journey' and 'growth opportunities' (there are none). The system will send automated birthday messages that feel somehow lonelier than no message at all. HR will call it a massive success based on login metrics they don't mention are mandatory.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard DiversityInitiative { get; } = new(
        CardId: "PROJ_DEI",
        Title: "Diversity & Inclusion Task Force",
        Description: "Launch a comprehensive D&I program with training and metrics.",
        FlavorText: "\"We put a woman on the poster. Problem solved.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Governance, 5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,  // Well-intentioned even if ineffective
        Category: CardCategory.Action,
        ExtendedDescription: "Form a committee to address our diversity numbers without actually changing anything about how we hire, promote, or pay people. Everyone will complete unconscious bias training that makes them very aware of their biases but doesn't actually change behavior. We'll celebrate cultural heritage months with themed cafeteria menus. Leadership will remain suspiciously homogeneous but will now use phrases like 'allyship' in town halls.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard PerformanceRedesign { get; } = new(
        CardId: "PROJ_PERF_MGMT",
        Title: "Performance Management Overhaul",
        Description: "Replace annual reviews with 'continuous feedback culture.'",
        FlavorText: "\"We eliminated ratings. Now nobody knows where they stand.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 0), new MeterEffect(Meter.Alignment, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 0,  // Normal HR initiative
        Category: CardCategory.Action,
        ExtendedDescription: "Abolish the performance rating system that everyone hated and replace it with a different system everyone will hate. Managers will be asked to have 'continuous conversations' instead of annual reviews, which means nobody has any conversations at all. The bonus pool will still be distributed the same way, but now it's 'not tied to ratings' which makes it feel even more arbitrary. HR will survey everyone about the new system, get terrible feedback, and declare it a success.",
        MeterAffinity: Meter.Delivery
    );

    // === CUSTOMER & MARKET ===

    public static PlayableCard CustomerCentricity { get; } = new(
        CardId: "PROJ_CUSTOMER_CENTRIC",
        Title: "Customer Centricity Initiative",
        Description: "Reorganize the entire company around the customer.",
        FlavorText: "\"We put the customer at the center of everything, except decisions.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -10) }
        ),
        CorporateIntensity: 0,  // BS but harmless
        Category: CardCategory.Corporate,
        ExtendedDescription: "Create journey maps. Persona workshops. Voice of Customer programs. Customer Advisory Boards that meet quarterly for catered lunches. We'll put the customer at the center of our PowerPoint slides and org charts. When customers actually ask for things, we'll explain why our roadmap is already set. But we'll do it in a very customer-centric way, with empathy and a survey asking how we did.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard InnovationLab { get; } = new(
        CardId: "PROJ_INNOVATION_LAB",
        Title: "Innovation Lab Launch",
        Description: "Open a trendy innovation space to 'disrupt ourselves.'",
        FlavorText: "\"It's like a startup, except with more PowerPoints and less equity.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Morale, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,  // Normal R&D expense
        Category: CardCategory.Action,
        ExtendedDescription: "Rent a hip space in a cool neighborhood. Fill it with whiteboards, exposed brick, and people wearing jeans. They'll run 'design sprints' and build prototypes that will never survive contact with Legal, Compliance, or IT Security. The main business will view them with suspicion and refuse to implement any of their ideas. After 2 years, the lab will be quietly 'integrated back into the core business' and everyone will update their LinkedIn.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard StrategicSourcing { get; } = new(
        CardId: "PROJ_SOURCING",
        Title: "Strategic Sourcing Initiative",
        Description: "Renegotiate all vendor contracts and 'optimize' the supply chain.",
        FlavorText: "\"We saved $50M by switching to vendors who are slightly worse.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 20), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 2,  // Squeezing vendors is somewhat evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Squeeze every vendor until they either cut corners or go bankrupt. Consolidate to fewer suppliers for 'efficiency,' creating single points of failure. Offshore everything that can be offshored. The finance team will celebrate the savings while ignoring that quality has plummeted and lead times have tripled. When something inevitably breaks, we'll blame the vendor and switch to a new one. Repeat annually.",
        MeterAffinity: Meter.Runway
    );

    // === COMPLIANCE & GOVERNANCE ===

    public static PlayableCard ZeroTrust { get; } = new(
        CardId: "PROJ_ZERO_TRUST",
        Title: "Zero Trust Security Architecture",
        Description: "Implement a zero-trust security model across the enterprise.",
        FlavorText: "\"We don't trust anyone. Including ourselves.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Delivery, -5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 0,  // Security is good actually
        Category: CardCategory.Action,
        ExtendedDescription: "Require authentication for everything. Re-authenticate constantly. Add MFA to MFA. Every login will require 3 apps, a hardware token, a blood sample, and a sincere apology for existing. Productivity will plummet as employees spend 40% of their day logging into things. When the CEO complains that she can't access her email, she'll get an exemption. Everyone will continue using the same passwords they've had since 2015.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard SustainabilityProgram { get; } = new(
        CardId: "PROJ_ESG",
        Title: "Sustainability Transformation",
        Description: "Launch comprehensive ESG initiatives and carbon neutrality goals.",
        FlavorText: "\"We bought carbon offsets. Somewhere a tree exists, probably.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Runway, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Morale, -10) }
        ),
        CorporateIntensity: 1,  // Greenwashing is mildly evil
        Category: CardCategory.Corporate,
        ExtendedDescription: "Announce ambitious carbon neutrality goals for 2050, a date conveniently after everyone currently in leadership has retired. Hire a Chief Sustainability Officer. Publish a glossy report with photos of windmills and diverse employees near trees. Switch to paper straws in the cafeteria while ignoring that our main product literally destroys the environment. But the ESG score looks great, and that's what matters to investors.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard ProcessExcellence { get; } = new(
        CardId: "PROJ_PROCESS",
        Title: "Process Excellence Program",
        Description: "Map, optimize, and standardize all business processes.",
        FlavorText: "\"We documented 5,000 processes. Nobody reads any of them.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Alignment, -5) }
        ),
        CorporateIntensity: 0,  // Normal operations initiative
        Category: CardCategory.Action,
        ExtendedDescription: "Hire Lean Six Sigma Black Belts to document every process in the company. Create swim lane diagrams that require a projector and squinting to read. Discover that most actual work happens through workarounds that bypass official processes entirely. Standardize everything, eliminating the workarounds that actually worked. Productivity drops 30% while process compliance reaches 95%. Mission accomplished.",
        MeterAffinity: Meter.Governance
    );

    // === REVENUE INITIATIVES ===

    public static PlayableCard SalesBlitz { get; } = new(
        CardId: "PROJ_SALES_BLITZ",
        Title: "Q-End Sales Blitz",
        Description: "Push the sales team to close every deal before quarter end.",
        FlavorText: "\"Sleep is for Q1. Close everything now.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(45), new MeterEffect(Meter.Morale, -5) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -15) }
        ),
        CorporateIntensity: 1,  // Burns out employees - mildly evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "Demand that every salesperson work nights and weekends to hit quota. Offer SPIFs, threaten PIPs, and dangle President's Club. Customers will receive 47 'final offer' emails. Deals that should close next quarter will be pulled forward with massive discounts, creating a crater in Q+1. The sales VP will ring a bell loudly. Morale will crater. But the board only sees this quarter's number.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard CostCutting { get; } = new(
        CardId: "PROJ_COST_CUTTING",
        Title: "Strategic Cost Reduction",
        Description: "Implement aggressive cost-cutting measures across the organization.",
        FlavorText: "\"We're not 'cutting.' We're 'optimizing our cost structure.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(40), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(30), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) },
            Bad: new IEffect[] { new ProfitEffect(15), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 2,  // Cuts that hurt people
        Category: CardCategory.Revenue,
        ExtendedDescription: "Cancel the holiday party. Freeze travel. Reduce the office snack budget by 80%. Eliminate 'redundancies' (people). Remaining employees will do 3 jobs for the same pay. Quality will suffer, but that's next quarter's problem. The CFO will present savings projections that assume zero productivity loss. They're always wrong, but the board loves the slides.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard PriceIncrease { get; } = new(
        CardId: "PROJ_PRICE_INCREASE",
        Title: "Strategic Pricing Adjustment",
        Description: "Raise prices across the board. For 'value alignment.'",
        FlavorText: "\"It's not a price hike. It's 'value-based pricing.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Alignment, -10) },
            Bad: new IEffect[] { new ProfitEffect(-10), new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 1,  // Squeezing customers - mildly evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "Raise prices 15% and see what happens. Marketing will craft a narrative about 'enhanced value' and 'market positioning.' Customer Success will handle the angry calls. Some customers will leave. The ones who stay won't forgive us. But analysts love 'pricing power' and the stock might tick up. If customers revolt, blame inflation.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard Layoffs { get; } = new(
        CardId: "PROJ_LAYOFFS",
        Title: "Workforce Optimization",
        Description: "Reduce headcount to improve profitability immediately.",
        FlavorText: "\"We're not firing people. We're 'building a leaner organization.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(50), new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Morale, -10) },
            Expected: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -15) },
            Bad: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Morale, -35), new MeterEffect(Meter.Delivery, -25), new MeterEffect(Meter.Governance, -15), new FineEffect(18, "Wrongful termination class action") }
        ),
        CorporateIntensity: 5,  // Firing people for profit = very evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "The nuclear option for hitting quarterly numbers. HR will prepare 'impacted employee' lists. Managers will have 'difficult conversations.' Survivors will update their LinkedIn profiles and do 2x the work. Institutional knowledge walks out the door. But Wall Street loves efficiency! The CEO will express 'heartfelt' sympathy while cashing their bonus. Evil, but effective.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard MarketExpansion { get; } = new(
        CardId: "PROJ_MARKET_EXPANSION",
        Title: "New Market Expansion",
        Description: "Enter a new market segment or geographic region.",
        FlavorText: "\"We're big in Delaware. Time to conquer... Rhode Island.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(40), new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new ProfitEffect(15), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(-20), new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Alignment, -15) }
        ),
        CorporateIntensity: 0,  // Normal business expansion
        Category: CardCategory.Revenue,
        ExtendedDescription: "Announce bold expansion plans to a market we don't understand. Hire a local team who will quit within 6 months when they realize we have no idea what we're doing. Spend heavily on localization that still gets the language wrong. If it works, it's 'strategic vision.' If it fails, we 'learned valuable lessons' and write off the investment.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard AcquisitionIntegration { get; } = new(
        CardId: "PROJ_ACQUISITION",
        Title: "Acquisition Synergy Capture",
        Description: "Finally integrate that company we bought 18 months ago.",
        FlavorText: "\"The synergies are definitely coming. Any quarter now.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(55), new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Delivery, -10) },
            Bad: new IEffect[] { new ProfitEffect(-15), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Governance, -15) }
        ),
        CorporateIntensity: 2,  // Usually involves layoffs
        Category: CardCategory.Revenue,
        ExtendedDescription: "We paid $500M for a company and promised $100M in 'synergies' that haven't materialized. Time to force-merge the teams, eliminate duplicates, and pray the integration doesn't crater both businesses. Their best people will leave for competitors. Their culture will clash with ours. But if we can just capture 10% of those promised synergies, the board might forget we overpaid.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard SubscriptionPivot { get; } = new(
        CardId: "PROJ_SUBSCRIPTION",
        Title: "Subscription Revenue Pivot",
        Description: "Convert one-time purchases to recurring subscription revenue.",
        FlavorText: "\"Customers love paying forever for things they used to own.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Runway, 15) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Alignment, -15) },
            Bad: new IEffect[] { new ProfitEffect(-5), new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 1,  // Forcing recurring payments on customers
        Category: CardCategory.Revenue,
        ExtendedDescription: "Turn your product into a 'service' by making customers pay monthly for what they used to buy once. Wall Street loves recurring revenue! Customers will complain loudly on social media. Your subreddit will be on fire. But analysts will upgrade the stock because 'predictable revenue streams.' Adobe did it. Microsoft did it. Your turn.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard PremiumTier { get; } = new(
        CardId: "PROJ_PREMIUM_TIER",
        Title: "Premium Enterprise Tier",
        Description: "Launch an expensive new tier with features nobody asked for.",
        FlavorText: "\"We're not increasing prices. We're offering 'enhanced value.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(30), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -10) }
        ),
        CorporateIntensity: 0,  // Normal product tiering
        Category: CardCategory.Revenue,
        ExtendedDescription: "Create a 'Premium' tier that's just the old product with a SSO checkbox and a dedicated Slack channel. Charge 3x more. Fortune 500 procurement departments don't blink at enterprise pricing - they expect it. The sales team will love their new commission structure. Product will hate maintaining two almost-identical versions.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard OffshoreOptimization { get; } = new(
        CardId: "PROJ_OFFSHORE",
        Title: "Global Talent Optimization",
        Description: "Move work to lower-cost regions for 'around-the-clock productivity.'",
        FlavorText: "\"It's not outsourcing. It's 'accessing global talent pools.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(40), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) },
            Bad: new IEffect[] { new ProfitEffect(10), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Governance, -10), new FineEffect(10, "Contractor misclassification settlement") }
        ),
        CorporateIntensity: 3,  // Replacing workers with cheaper labor
        Category: CardCategory.Revenue,
        ExtendedDescription: "Replace $150K San Francisco engineers with $30K Bangalore contractors. On paper, 5x cost savings! In reality, the timezone differences mean everything takes 3x longer. Institutional knowledge evaporates. The contractors are excellent but can't read minds about undocumented systems. Your remaining US employees will spend all day on calls explaining context.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard VendorConsolidation { get; } = new(
        CardId: "PROJ_VENDOR_CONSOLIDATION",
        Title: "Vendor Consolidation Program",
        Description: "Ruthlessly renegotiate every vendor contract for maximum savings.",
        FlavorText: "\"We're not squeezing them. We're 'optimizing partnerships.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(30), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Delivery, -5), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 1,  // Squeezing vendors - mildly evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "Call every vendor and demand 20% off or we walk. Some will cave. Some will call our bluff. The procurement team will feel powerful. The teams who depend on those vendors will scramble when service levels drop. 'Preferred partner' status means nothing when you're squeezing them dry. But the quarterly savings look great in the board deck.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard ChannelPartner { get; } = new(
        CardId: "PROJ_CHANNEL_PARTNER",
        Title: "Channel Partner Expansion",
        Description: "Let other companies sell our product for a cut of the revenue.",
        FlavorText: "\"Why hire salespeople when partners can do it for 30% margin?\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new ProfitEffect(15), new MeterEffect(Meter.Alignment, -5) },
            Bad: new IEffect[] { new ProfitEffect(-10), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 0,  // Normal sales strategy
        Category: CardCategory.Revenue,
        ExtendedDescription: "Build a partner program where other companies sell for you. They take 30% but you scale without hiring. The good partners will demand exclusivity. The bad partners will make promises you can't keep. Customer experience becomes 'variable.' But the revenue shows up without headcount, and that's beautiful on the P&L.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard ContractRenegotiation { get; } = new(
        CardId: "PROJ_CONTRACT_RENEGO",
        Title: "Contract Renegotiation Wave",
        Description: "Proactively reach out to customers expiring in 6 months with 'special offers.'",
        FlavorText: "\"Sign now and lock in only a 20% increase instead of 40%.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(38), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(22), new MeterEffect(Meter.Alignment, -8) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -5) }
        ),
        CorporateIntensity: 0,  // Normal sales activity
        Category: CardCategory.Revenue,
        ExtendedDescription: "Send the renewal team door-to-door (virtually) to lock customers into long-term contracts before they realize they could switch. Early renewal discounts that are somehow more expensive than the original price. Customers will feel slightly trapped but continue paying. The CFO calls it 'reducing churn risk.'",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard FeatureGating { get; } = new(
        CardId: "PROJ_FEATURE_GATE",
        Title: "Strategic Feature Gating",
        Description: "Move existing features behind premium paywall. Innovation!",
        FlavorText: "\"We're not removing features. We're creating 'opportunities to upgrade.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(32), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Alignment, -12), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new ProfitEffect(0), new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 2,  // Taking away features customers had = evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "Take features customers have been using for years and put them behind a paywall. Marketing will call it 'product tiering.' Reddit will call it something unprintable. Engineers will question their life choices. But the finance team will present beautiful upgrade conversion charts. Some customers will pay. Most will complain. A few will leave. Net positive.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard ConsultingArm { get; } = new(
        CardId: "PROJ_CONSULTING",
        Title: "Professional Services Expansion",
        Description: "Launch a consulting division to monetize our expertise.",
        FlavorText: "\"We'll charge $500/hour to explain our own product.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Delivery, -8), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Delivery, -18), new MeterEffect(Meter.Morale, -12) }
        ),
        CorporateIntensity: 0,  // Normal business
        Category: CardCategory.Revenue,
        ExtendedDescription: "Hire consultants to help customers use our confusing product. Charge premium rates for implementation that should be simple. The consulting team will become indispensable because the product never gets better. Engineering will resent that consulting makes more money. It's the Microsoft model: confuse, monetize, repeat.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard DataMonetization { get; } = new(
        CardId: "PROJ_DATA_MONETIZE",
        Title: "Data Monetization Initiative",
        Description: "Sell anonymized customer usage data to third parties.",
        FlavorText: "\"It's anonymized. Mostly. Probably. Legal said it's fine.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(45), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(28), new MeterEffect(Meter.Governance, -10) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Governance, -25), new MeterEffect(Meter.Alignment, -20), new FineEffect(30, "FTC data privacy consent decree") }
        ),
        CorporateIntensity: 3,  // Selling customer data = very evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "We have petabytes of customer data. Why let it sit idle? Sell 'aggregated insights' to advertisers, hedge funds, and companies we've never heard of. The privacy policy technically allows it if you squint. GDPR compliance is 'in progress.' When customers find out, we'll call it 'market research partnerships.' Pure profit margin.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard MaintenanceFees { get; } = new(
        CardId: "PROJ_MAINTENANCE_FEES",
        Title: "Support & Maintenance Restructuring",
        Description: "Charge for support that used to be included.",
        FlavorText: "\"Basic support is still free. Basic means email-only with 5-day SLA.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(28), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Alignment, -10) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -22), new MeterEffect(Meter.Governance, -8) }
        ),
        CorporateIntensity: 1,  // Reducing service for same price - mildly evil
        Category: CardCategory.Revenue,
        ExtendedDescription: "Introduce tiered support: Bronze (email, maybe we respond), Silver (phone, but hold times are rough), Gold (an actual human who cares). Existing customers get 'grandfathered' into Bronze. Want the support you used to get? That's Platinum now. The support team will be demoralized. The customers will be frustrated. The revenue will be fantastic.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard UpsellBlitz { get; } = new(
        CardId: "PROJ_UPSELL",
        Title: "Account Expansion Campaign",
        Description: "Aggressively upsell existing customers on features they didn't know existed.",
        FlavorText: "\"They're already paying. Let's see how much more we can extract.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(42), new MeterEffect(Meter.Morale, 5) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Alignment, -8) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -18), new MeterEffect(Meter.Morale, -8) }
        ),
        CorporateIntensity: 0,  // Normal sales activity
        Category: CardCategory.Revenue,
        ExtendedDescription: "Deploy the Customer Success team as a sales force in disguise. Every 'check-in call' becomes a pitch. Quarterly business reviews are really 'why you need the enterprise tier' presentations. Some customers will appreciate learning about features. Most will feel ambushed. But expansion revenue is the best revenue - no acquisition cost!",
        MeterAffinity: Meter.Morale
    );

    // === NEW NON-REVENUE CARDS ===

    public static PlayableCard MetaverseStrategy { get; } = new(
        CardId: "PROJ_METAVERSE",
        Title: "Metaverse Strategy Initiative",
        Description: "Establish corporate presence in virtual worlds nobody uses.",
        FlavorText: "\"Our virtual headquarters has zero visitors. But what a headquarters!\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 15), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Morale, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Corporate,
        ExtendedDescription: "Buy virtual land for $2M. Build a digital corporate campus complete with conference rooms no one enters. Force employees to attend 'metaverse town halls' where their avatars stare into the void. The CEO will be very excited to show the board his virtual office. The stock might bump 2% when we announce it. Worth it.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard LowCodePlatform { get; } = new(
        CardId: "PROJ_LOWCODE",
        Title: "Low-Code Citizen Development",
        Description: "Let non-engineers build apps. What could go wrong?",
        FlavorText: "\"Karen from Accounting built a customer database. It crashed twice today.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Governance, -5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Governance, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Governance, -20), new MeterEffect(Meter.Morale, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Deploy a no-code platform and tell business users to build their own solutions. IT will lose visibility into shadow apps multiplying like tribbles. Security will have concerns. Compliance will have concerns. But velocity! Empowerment! When something breaks catastrophically, we'll blame 'governance gaps' and buy another platform.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard GenerativeAIPolicy { get; } = new(
        CardId: "PROJ_GENAI_POLICY",
        Title: "Generative AI Guidelines",
        Description: "Create a 47-page policy on acceptable AI usage that no one reads.",
        FlavorText: "\"You can use ChatGPT but you can't tell it anything. Very helpful.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, -5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Legal and IT will spend 6 months crafting comprehensive AI usage guidelines. The policy will be so restrictive that compliant AI usage is essentially impossible. Employees will ignore it entirely and use Claude on their phones. When an incident occurs, we'll update the policy to 48 pages.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard HybridWorkPolicy { get; } = new(
        CardId: "PROJ_HYBRID",
        Title: "Hybrid Work Framework",
        Description: "Define which days employees must be in office. Change it quarterly.",
        FlavorText: "\"Tuesdays and Thursdays are mandatory. No wait, Wednesday is now mandatory.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Delivery, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Action,
        ExtendedDescription: "Form a committee to determine the perfect in-office schedule. Survey employees, ignore the results, then mandate three days in office because 'collaboration.' Middle managers will enforce badge swipes. Employees will badge in, attend one meeting, then leave. Measure presence, not productivity. The future of work!",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard MicroservicesRewrite { get; } = new(
        CardId: "PROJ_MICROSERVICES",
        Title: "Microservices Architecture",
        Description: "Break our working monolith into 200 tiny services.",
        FlavorText: "\"The monolith worked. But microservices are what cool companies do.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Governance, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Runway, -15), new MeterEffect(Meter.Morale, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Runway, -25), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Take the monolith that's worked for 10 years and shatter it into hundreds of services that now need a PhD in Kubernetes to deploy. Each team owns 12 services they don't understand. Distributed tracing becomes your new religion. Production debugging becomes archaeology. But the architecture diagram looks amazing.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard LeadershipOffsite { get; } = new(
        CardId: "PROJ_OFFSITE",
        Title: "Executive Leadership Offsite",
        Description: "Send executives to a resort to 'align on strategy.'",
        FlavorText: "\"We spent $500K on a Napa retreat. The strategy is still unclear.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Corporate,
        ExtendedDescription: "Fly the C-suite to somewhere beautiful where they'll do trust falls and create vision statements that mean nothing. The rest of the company will see Instagram posts of executives doing yoga while they work through lunch. At least they'll come back with a new strategic pillar that contradicts the last three.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard VendorManagementOffice { get; } = new(
        CardId: "PROJ_VMO",
        Title: "Vendor Management Office",
        Description: "Add a layer of bureaucracy between teams and their tools.",
        FlavorText: "\"You need approval to buy post-it notes. It takes 6 weeks.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -20), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Create a team whose job is to review every vendor purchase. They'll require 8 approvals for a $500 software license. Procurement cycles will stretch to 3 months. Teams will find creative workarounds using personal credit cards. But we'll definitely reduce 'vendor sprawl' by approximately zero percent.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard WellnessProgram { get; } = new(
        CardId: "PROJ_WELLNESS",
        Title: "Employee Wellness Initiative",
        Description: "Launch meditation apps and yoga classes to reduce burnout from overwork.",
        FlavorText: "\"We can't reduce your workload, but here's a mindfulness webinar.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Delivery, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 8), new MeterEffect(Meter.Runway, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Address the symptoms of workplace stress without touching the causes. Offer meditation subscriptions to employees working 60-hour weeks. Install nap pods that no one has time to use. Launch a step-counting challenge while scheduling meetings during lunch. Wellness theater at its finest.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard TechDebtSprint { get; } = new(
        CardId: "PROJ_TECHDEBT",
        Title: "Tech Debt Paydown Sprint",
        Description: "Dedicate one sprint to fixing the code everyone's afraid to touch.",
        FlavorText: "\"We scheduled 2 weeks for tech debt. We need 2 years.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 20), new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Runway, -5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, 0), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Runway, -15) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Allow engineers two weeks to fix the horrors they've been complaining about for years. They'll triage ruthlessly, fix 2% of the problems, and discover 50 new ones. The codebase will be marginally less terrifying. Leadership will call it a success and cancel the next scheduled tech debt sprint for 'critical business priorities.'",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard ReorgAnnouncement { get; } = new(
        CardId: "PROJ_REORG",
        Title: "Organizational Restructure",
        Description: "Shuffle the org chart and call it 'strategic alignment.'",
        FlavorText: "\"Your new manager is someone you've never met. Good luck!\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -20) }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Corporate,
        ExtendedDescription: "Announce a major reorg with 48 hours notice. Everyone's manager changes. Teams are split and merged seemingly at random. The org chart now looks like a Jackson Pollock painting. Productivity halts for 3 months while everyone figures out their new roles. Best people update LinkedIn and leave.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard BrandRefresh { get; } = new(
        CardId: "PROJ_REBRAND",
        Title: "Corporate Brand Refresh",
        Description: "Spend $5M on a new logo that looks like the old one.",
        FlavorText: "\"We changed the font. Revolutionary.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Runway, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Runway, -15) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Corporate,
        ExtendedDescription: "Hire a branding agency to spend 6 months creating a new logo that's slightly more minimal. Pay them $3M. Spend another $5M updating all the collateral, signage, and swag. Customers won't notice. Employees will hate the new colors. But the brand guidelines PDF will be absolutely gorgeous.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard ComplianceTraining { get; } = new(
        CardId: "PROJ_COMPLIANCE_TRAIN",
        Title: "Mandatory Compliance Training",
        Description: "Make everyone complete 40 hours of click-through training.",
        FlavorText: "\"Click. Click. Click. Yes I promise not to insider trade. Click.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Morale, -5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Annual training season arrives. Every employee spends a week clicking through modules on harassment, security, ethics, and fire safety. They'll click 'Next' without reading, fail the quiz, and retake it until they pass. Compliance will have documentation. Actual compliance is anyone's guess.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard InternalAudit { get; } = new(
        CardId: "PROJ_AUDIT",
        Title: "Internal Audit Initiative",
        Description: "Have auditors review everything and produce findings nobody reads.",
        FlavorText: "\"We have 847 audit findings. We've addressed 12.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Delivery, -5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, -10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Morale, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Unleash internal audit on unsuspecting departments. They'll find problems everyone already knows about and document them extensively. Management will promise to remediate. Nothing will change until external auditors arrive and find the same issues. Then there will be panic. Circle of life.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard NPSProgram { get; } = new(
        CardId: "PROJ_NPS",
        Title: "Net Promoter Score Program",
        Description: "Survey customers constantly and obsess over a single number.",
        FlavorText: "\"Our NPS is 32. Is that good? Bad? Nobody knows but we're measuring it.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Morale, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Launch an NPS program that surveys customers after every interaction. The score becomes everyone's OKR. Teams game the system by surveying only happy customers. Support reps beg for 10s. The number goes up while actual satisfaction stays flat. But the board loves simple metrics.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard LearningPlatform { get; } = new(
        CardId: "PROJ_LEARNING",
        Title: "Learning & Development Platform",
        Description: "Buy a learning platform with 10,000 courses nobody takes.",
        FlavorText: "\"We have unlimited LinkedIn Learning access. Completion rate: 3%.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 10), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Runway, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Runway, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Deploy an enterprise learning platform with thousands of courses. Announce it as a 'growth opportunity.' Nobody has time to actually take courses because they're too busy working. HR will send monthly emails about utilization. Managers will put 'complete 5 courses' in performance goals. Nobody will.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard HiringFreeze { get; } = new(
        CardId: "PROJ_HIRING_FREEZE",
        Title: "Strategic Hiring Pause",
        Description: "Stop all hiring while pretending it's temporary.",
        FlavorText: "\"We're not freezing hiring. We're being 'thoughtful about growth.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 15) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -10) },
            Bad: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -20) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Action,
        ExtendedDescription: "Announce a 'temporary' hiring freeze that will last 18 months. Teams that were already understaffed will now be critically understaffed. Backfills require CEO approval. Exception requests pile up. Best employees leave for companies that are hiring. But the savings look great on the quarterly report.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard OutsourcingStudy { get; } = new(
        CardId: "PROJ_OUTSOURCE_STUDY",
        Title: "Outsourcing Feasibility Study",
        Description: "Hire consultants to determine what work we can offshore.",
        FlavorText: "\"The consultants recommend we outsource everything except consultants.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Runway, 5), new MeterEffect(Meter.Morale, -15) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Corporate,
        ExtendedDescription: "Commission a study on which functions could be outsourced. Word will leak. Everyone will assume their job is at risk. The best people will find new jobs preemptively. The study will conclude that outsourcing would save money if you ignore all the hidden costs. Leadership will proudly announce a 'balanced approach.'",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard KnowledgeManagement { get; } = new(
        CardId: "PROJ_KNOWLEDGE_MGMT",
        Title: "Knowledge Management System",
        Description: "Create a wiki that will be outdated within 6 months.",
        FlavorText: "\"The documentation is comprehensive. It's also from 2019.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 15), new MeterEffect(Meter.Governance, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -5), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Governance, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Deploy Confluence, Notion, or whatever knowledge tool is trendy. Mandate that all teams document everything. For 3 months, documentation flourishes. Then everyone gets busy and stops updating. New employees will find beautiful but obsolete docs. Tribal knowledge wins again.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard IncidentManagement { get; } = new(
        CardId: "PROJ_INCIDENT_MGMT",
        Title: "Incident Management Overhaul",
        Description: "Create elaborate processes for when things break.",
        FlavorText: "\"We have a 47-step incident response procedure. Step 1: Panic.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, 0), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Build a comprehensive incident management system. War rooms, severity levels, communication templates, postmortems. Engineers will fill out forms while servers burn. Postmortems will identify root causes that are never addressed. But the audit trail will be impeccable.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard MentorshipProgram { get; } = new(
        CardId: "PROJ_MENTORSHIP",
        Title: "Formal Mentorship Program",
        Description: "Match employees with mentors who are too busy to mentor.",
        FlavorText: "\"Your mentor cancelled again. Something about a 'fire drill.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Morale, 15), new MeterEffect(Meter.Delivery, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Morale, 5), new MeterEffect(Meter.Delivery, 0) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Alignment, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Launch a formal mentorship program matching junior employees with senior leaders. The algorithm will make incomprehensible pairings. Meetings will be cancelled due to 'urgent priorities.' When it works, it's transformative. When it doesn't, mentees feel forgotten. Success rate: 20%.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard ChangeManagement { get; } = new(
        CardId: "PROJ_CHANGE_MGMT",
        Title: "Change Management Framework",
        Description: "Create processes to manage the resistance to all your other projects.",
        FlavorText: "\"We're implementing change management. The irony is not lost on us.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -5), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Hire change management consultants to ease adoption of initiatives. They'll produce stakeholder maps, communication plans, and resistance assessments. Employees will be 'change fatigued' before anything changes. The consultants will recommend more change management. Recursive bureaucracy achieved.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard RiskAssessment { get; } = new(
        CardId: "PROJ_RISK_ASSESS",
        Title: "Enterprise Risk Assessment",
        Description: "Document all the risks we're ignoring in a comprehensive spreadsheet.",
        FlavorText: "\"We've identified 200 risks. We're mitigating 3.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 20), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, -10), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Conduct a comprehensive risk assessment with heat maps and probability matrices. Identify existential risks that leadership will dismiss as 'unlikely.' Document everything meticulously. When a risk materializes, everyone will remember the assessment that warned about it. Nobody will have done anything.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard SuccessionPlanning { get; } = new(
        CardId: "PROJ_SUCCESSION",
        Title: "Succession Planning Initiative",
        Description: "Identify replacements for leaders who will never leave.",
        FlavorText: "\"You're the backup for someone who's been here 30 years. Enjoy waiting.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Governance, 10), new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Morale, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Governance, 5), new MeterEffect(Meter.Morale, 0) },
            Bad: new IEffect[] { new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Alignment, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Create succession plans for key roles. High potentials will be identified and told they're 'on the list.' They'll wait years with no actual opportunity. When roles open, external candidates will be hired. The identified successors will leave. The list will be updated with their replacements.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard CustomerAdvisoryBoard { get; } = new(
        CardId: "PROJ_CAB",
        Title: "Customer Advisory Board",
        Description: "Gather top customers quarterly to tell us things we'll ignore.",
        FlavorText: "\"Our advisory board has great ideas. We've implemented none of them.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 20), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 10), new MeterEffect(Meter.Runway, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Morale, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Invite top customers to exclusive quarterly sessions at nice venues. They'll share valuable feedback and product ideas. Sales will use it as a relationship-building exercise. Product will attend, take notes, and continue with their existing roadmap. Customers will feel heard but unsatisfied. Repeat quarterly.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard OKRImplementation { get; } = new(
        CardId: "PROJ_OKR",
        Title: "OKR Framework Rollout",
        Description: "Implement Objectives and Key Results. Measure everything, change nothing.",
        FlavorText: "\"Our key result is improving our key results. Very meta.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Alignment, 15), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Alignment, 5), new MeterEffect(Meter.Morale, -5), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Action,
        ExtendedDescription: "Deploy OKRs company-wide because Google uses them. Teams will spend weeks wordsmithing objectives. Key results will be either unmeasurable aspirations or things that would happen anyway. Quarterly OKR reviews will be theater. Everyone will achieve 70% by definition. Intel is somehow blamed.",
        MeterAffinity: Meter.Alignment
    );

    // === NEW REVENUE CARDS ===

    public static PlayableCard EmergencyBudgetCuts { get; } = new(
        CardId: "PROJ_EMERGENCY_CUTS",
        Title: "Emergency Budget Reduction",
        Description: "Slash budgets mid-year with immediate effect.",
        FlavorText: "\"All spending frozen effective yesterday. Good luck finishing projects.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(48), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(32), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Delivery, -15) },
            Bad: new IEffect[] { new ProfitEffect(15), new MeterEffect(Meter.Morale, -30), new MeterEffect(Meter.Delivery, -25), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Announce emergency cost cuts effective immediately. Cancel vendor contracts mid-stream. Freeze all travel and discretionary spend. Teams scramble to figure out how to finish projects with no budget. The savings are real but so is the chaos. Next quarter's numbers will be worse because of delayed projects.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard AssetSale { get; } = new(
        CardId: "PROJ_ASSET_SALE",
        Title: "Non-Core Asset Divestiture",
        Description: "Sell off business units that don't fit the 'strategic focus.'",
        FlavorText: "\"We're selling the profitable division to focus on our core unprofitable one.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(65), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new ProfitEffect(45), new MeterEffect(Meter.Morale, -10) },
            Bad: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Morale, -20), new MeterEffect(Meter.Alignment, -15) }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Sell a business unit to hit quarterly numbers. Employees in that unit will be 'transitioned' to the buyer. The rest of the company will wonder if they're next. Private equity loves buying these cast-offs. We'll call it 'strategic portfolio optimization.' The one-time gain looks beautiful on the P&L.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard PaymentTermsHardball { get; } = new(
        CardId: "PROJ_PAYMENT_TERMS",
        Title: "Payment Terms Renegotiation",
        Description: "Demand faster payment from customers, extend terms to vendors.",
        FlavorText: "\"Pay us in 15 days. We'll pay you in 90. It's called cash flow optimization.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(32), new MeterEffect(Meter.Runway, 15) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Delivery, -15) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Squeeze the supply chain from both ends. Demand customers pay faster with early payment discounts that are actually penalties for late payment. Extend vendor terms to 90 days. Our cash position improves dramatically. Vendor relationships deteriorate. Customers grumble. Cash is king.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard ProductSunset { get; } = new(
        CardId: "PROJ_SUNSET",
        Title: "Legacy Product Sunset",
        Description: "Kill old products to force customers onto new ones.",
        FlavorText: "\"We're not killing your favorite product. We're 'sunsetting' it.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new ProfitEffect(22), new MeterEffect(Meter.Alignment, -12), new MeterEffect(Meter.Morale, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -25), new MeterEffect(Meter.Morale, -15) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Revenue,
        ExtendedDescription: "End-of-life the legacy product that 40% of customers still use. They'll be 'migrated' to the new platform whether they want to or not. Engineering no longer has to maintain two codebases. Some customers will upgrade, some will leave, and support tickets will spike for 6 months. But the portfolio is now 'streamlined.'",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard AutomationWave { get; } = new(
        CardId: "PROJ_AUTOMATION",
        Title: "Process Automation Initiative",
        Description: "Replace manual processes with bots. And the people who did them.",
        FlavorText: "\"The bot does the work of 12 people. 11 of them now work elsewhere.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(52), new MeterEffect(Meter.Delivery, 10), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Morale, -15), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Morale, -25), new MeterEffect(Meter.Delivery, -15), new MeterEffect(Meter.Governance, -10) }
        ),
        CorporateIntensity: 3,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Deploy RPA bots to automate repetitive tasks currently done by humans. The humans will be 'redeployed to higher-value work' (laid off). The bots will work 24/7 without complaints or benefits. When they break, nobody will understand how to fix them. But the cost savings are undeniable.",
        MeterAffinity: Meter.Delivery
    );

    public static PlayableCard DeferredRevenue { get; } = new(
        CardId: "PROJ_DEFERRED",
        Title: "Revenue Recognition Acceleration",
        Description: "Find creative ways to recognize revenue sooner.",
        FlavorText: "\"It's not fraud. It's 'aggressive accounting.' Very different.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(40), new MeterEffect(Meter.Governance, -5) },
            Expected: new IEffect[] { new ProfitEffect(28), new MeterEffect(Meter.Governance, -15) },
            Bad: new IEffect[] { new ProfitEffect(10), new MeterEffect(Meter.Governance, -25), new FineEffect(20, "SEC revenue recognition inquiry") }
        ),
        CorporateIntensity: 3,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Work with auditors to find ways to recognize revenue earlier. Bill-and-hold arrangements, percentage of completion adjustments, and other techniques that are 'within GAAP' if you squint. This quarter looks great. Future quarters will be missing revenue we already recognized. The CFO is confident it's fine.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard InsuranceClaim { get; } = new(
        CardId: "PROJ_INSURANCE",
        Title: "Business Interruption Claim",
        Description: "File insurance claims for everything remotely claimable.",
        FlavorText: "\"Our insurance company is not returning our calls. Good sign?\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(50), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Governance, 0) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Governance, -10), new MeterEffect(Meter.Runway, -10) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Revenue,
        ExtendedDescription: "After any incident, deploy the risk management team to maximize insurance recovery. Document everything extensively. Hire claims consultants who work on contingency. The insurer will fight every line item. The settlement will be less than hoped but more than expected. Pure profit margin if it works.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard BonusDeferral { get; } = new(
        CardId: "PROJ_BONUS_DEFER",
        Title: "Incentive Compensation Restructure",
        Description: "Defer bonuses and switch to 'long-term incentives.'",
        FlavorText: "\"Your bonus is now a 4-year vesting grant. You're welcome for the retention.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(38), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Morale, -18), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(12), new MeterEffect(Meter.Morale, -30), new MeterEffect(Meter.Delivery, -15) }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Restructure compensation to reduce cash bonuses in favor of equity that vests over 4 years. Frame it as 'long-term alignment.' Employees see through it immediately. The best ones leave for companies with real bonuses. The rest quietly update their resumes while waiting for grants to vest.",
        MeterAffinity: Meter.Morale
    );

    public static PlayableCard FreemiumConversion { get; } = new(
        CardId: "PROJ_FREEMIUM",
        Title: "Freemium Monetization Push",
        Description: "Aggressively convert free users to paid plans.",
        FlavorText: "\"Free users aren't customers. They're conversion opportunities.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(42), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(25), new MeterEffect(Meter.Alignment, -10) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -22), new MeterEffect(Meter.Morale, -8) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Add more friction to the free tier. Limit features, add pop-ups, make the upgrade button bigger. Free users will either convert or leave—both outcomes the CFO considers wins. Power users will complain loudly on social media. Analysts will praise the improved unit economics.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard SaaSRevenue { get; } = new(
        CardId: "PROJ_SAAS_PUSH",
        Title: "SaaS Conversion Campaign",
        Description: "Move on-premise customers to cloud subscriptions.",
        FlavorText: "\"You loved owning software. You'll love renting it even more.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(45), new MeterEffect(Meter.Runway, 15) },
            Expected: new IEffect[] { new ProfitEffect(28), new MeterEffect(Meter.Alignment, -8), new MeterEffect(Meter.Delivery, -5) },
            Bad: new IEffect[] { new ProfitEffect(10), new MeterEffect(Meter.Alignment, -18), new MeterEffect(Meter.Delivery, -12) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Announce end-of-support for on-premise versions. Customers must migrate to SaaS or lose security updates. Some will pay 3x more annually for the privilege of not owning software. Others will flee to competitors. ARR goes up, customer sentiment goes down. Wall Street only sees the first metric.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard InventoryLiquidation { get; } = new(
        CardId: "PROJ_LIQUIDATION",
        Title: "Inventory Liquidation Event",
        Description: "Dump obsolete inventory at steep discounts.",
        FlavorText: "\"It's not a fire sale. It's a 'strategic inventory optimization event.'\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Runway, 10), new MeterEffect(Meter.Governance, 5) },
            Expected: new IEffect[] { new ProfitEffect(20), new MeterEffect(Meter.Alignment, -5) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Governance, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Clear out warehouses of products nobody wants at 70% off. Cash comes in, warehouse costs go down, and the write-off was already taken. Customers who paid full price will be annoyed. Channel partners will be furious. But the balance sheet looks cleaner and cash flow improves.",
        MeterAffinity: Meter.Governance
    );

    public static PlayableCard RebateReduction { get; } = new(
        CardId: "PROJ_REBATES",
        Title: "Rebate Program Restructure",
        Description: "Make rebates harder to claim while technically still offering them.",
        FlavorText: "\"The rebate is available. Just fill out these 47 forms.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(30), new MeterEffect(Meter.Runway, 5) },
            Expected: new IEffect[] { new ProfitEffect(18), new MeterEffect(Meter.Alignment, -12) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -22), new FineEffect(8, "FTC deceptive practices investigation") }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Advertise attractive rebates while making redemption nearly impossible. Require original receipts, UPC codes, specific forms, and submission within 14 days. Redemption rates drop from 80% to 20%. Marketing gets the sales bump, finance keeps most of the rebate budget. Customers rarely notice until it's too late.",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard CrossSellMandates { get; } = new(
        CardId: "PROJ_CROSSSELL",
        Title: "Cross-Sell Revenue Targets",
        Description: "Force every customer-facing team to push additional products.",
        FlavorText: "\"You called about a bug? Have you considered our premium support tier?\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(38), new MeterEffect(Meter.Alignment, 5) },
            Expected: new IEffect[] { new ProfitEffect(22), new MeterEffect(Meter.Alignment, -10), new MeterEffect(Meter.Morale, -8) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -20), new MeterEffect(Meter.Morale, -18) }
        ),
        CorporateIntensity: 1,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Give every customer-facing team cross-sell quotas. Support reps must pitch upgrades during help calls. Account managers have bundle targets. Customers will feel constantly sold to. Employee satisfaction drops as they're measured on sales instead of service. But attach rates go up, and that's the goal.",
        MeterAffinity: Meter.Alignment
    );

    public static PlayableCard EarlyTerminationFees { get; } = new(
        CardId: "PROJ_TERMINATION_FEES",
        Title: "Contract Lock-In Strengthening",
        Description: "Increase penalties for customers who want to leave.",
        FlavorText: "\"Cancellation is free. The early termination fee is 200% of remaining value.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(35), new MeterEffect(Meter.Runway, 10) },
            Expected: new IEffect[] { new ProfitEffect(22), new MeterEffect(Meter.Alignment, -15) },
            Bad: new IEffect[] { new ProfitEffect(8), new MeterEffect(Meter.Alignment, -25), new FineEffect(12, "Contract practices class action") }
        ),
        CorporateIntensity: 2,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Add punitive termination fees to all new contracts. Auto-renewal with 90-day cancellation windows nobody remembers. Customers who try to leave will pay dearly—or stay. Customer lifetime value increases artificially. NPS decreases organically. The CFO calls it 'sticky revenue.'",
        MeterAffinity: Meter.Runway
    );

    public static PlayableCard ReferralProgram { get; } = new(
        CardId: "PROJ_REFERRAL",
        Title: "Customer Referral Engine",
        Description: "Turn customers into an unpaid sales force.",
        FlavorText: "\"Refer 3 friends and get 10% off your next overpriced invoice.\"",
        Outcomes: new OutcomeProfile(
            Good: new IEffect[] { new ProfitEffect(40), new MeterEffect(Meter.Alignment, 10) },
            Expected: new IEffect[] { new ProfitEffect(22), new MeterEffect(Meter.Alignment, -5) },
            Bad: new IEffect[] { new ProfitEffect(5), new MeterEffect(Meter.Alignment, -15), new MeterEffect(Meter.Delivery, -5) }
        ),
        CorporateIntensity: 0,
        Category: CardCategory.Revenue,
        ExtendedDescription: "Launch a referral program that makes customers do sales work for modest discounts. The most enthusiastic customers become unpaid evangelists. CAC drops dramatically for referred customers. Gaming and fraud will be an issue. But the unit economics look phenomenal in the board deck.",
        MeterAffinity: Meter.Alignment
    );

    /// <summary>
    /// All playable cards for the starter deck.
    /// </summary>
    public static IReadOnlyList<PlayableCard> StarterDeck { get; } = new[]
    {
        DigitalTransformation,
        CloudMigration,
        AICenter,
        BlockchainPilot,
        AgileTransformation,
        GlobalOperatingModel,
        CultureTransformation,
        WorkplaceOfFuture,
        ERPOverhaul,
        Customer360,
        DataLake,
        EmployeeExperience,
        DiversityInitiative,
        PerformanceRedesign,
        CustomerCentricity,
        InnovationLab,
        StrategicSourcing,
        ZeroTrust,
        SustainabilityProgram,
        ProcessExcellence,
        // Revenue initiatives (original 17)
        SalesBlitz,
        CostCutting,
        PriceIncrease,
        Layoffs,
        MarketExpansion,
        AcquisitionIntegration,
        SubscriptionPivot,
        PremiumTier,
        OffshoreOptimization,
        VendorConsolidation,
        ChannelPartner,
        ContractRenegotiation,
        FeatureGating,
        ConsultingArm,
        DataMonetization,
        MaintenanceFees,
        UpsellBlitz,
        // New non-revenue cards (25)
        MetaverseStrategy,
        LowCodePlatform,
        GenerativeAIPolicy,
        HybridWorkPolicy,
        MicroservicesRewrite,
        LeadershipOffsite,
        VendorManagementOffice,
        WellnessProgram,
        TechDebtSprint,
        ReorgAnnouncement,
        BrandRefresh,
        ComplianceTraining,
        InternalAudit,
        NPSProgram,
        LearningPlatform,
        HiringFreeze,
        OutsourcingStudy,
        KnowledgeManagement,
        IncidentManagement,
        MentorshipProgram,
        ChangeManagement,
        RiskAssessment,
        SuccessionPlanning,
        CustomerAdvisoryBoard,
        OKRImplementation,
        // New revenue cards (15)
        EmergencyBudgetCuts,
        AssetSale,
        PaymentTermsHardball,
        ProductSunset,
        AutomationWave,
        DeferredRevenue,
        InsuranceClaim,
        BonusDeferral,
        FreemiumConversion,
        SaaSRevenue,
        InventoryLiquidation,
        RebateReduction,
        CrossSellMandates,
        EarlyTerminationFees,
        ReferralProgram
    };
}
