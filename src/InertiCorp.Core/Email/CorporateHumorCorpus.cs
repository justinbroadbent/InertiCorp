namespace InertiCorp.Core.Email;

/// <summary>
/// A large corpus of Office Space-style corporate humor templates.
/// Designed to be swappable with an LLM provider in the future.
/// </summary>
public sealed class CorporateHumorCorpus : IEmailContentProvider
{
    // === OPENINGS BY TONE ===

    private static readonly Dictionary<EmailTone, string[]> HumorousOpenings = new()
    {
        [EmailTone.Professional] = new[]
        {
            "Per the agenda for the meeting about the meeting,",
            "As per my previous email (third one today),",
            "Circling back to circle forward,",
            "I hope this email finds you before the next reorg,",
            "In the spirit of cross-functional synergy,",
            "Touching base to align our alignment,",
            "Further to our discussion about having more discussions,",
            "As we continue our journey toward operational excellence (whatever that means),",
            "In accordance with best practices that nobody actually follows,",
            "As mentioned in the all-hands that ran 47 minutes over,",
            "Per the PowerPoint that nobody read,",
            "Following up on the action item from the meeting about action items,",
            "I wanted to reach out and touch base (metaphorically, HR is watching),",
            "As we navigate these unprecedented times (drink!),",
            "In the spirit of radical transparency (within approved guidelines),",
        },
        [EmailTone.Aloof] = new[]
        {
            "Just pinging you again. And again.",
            "Bumping this to the top of your inbox. You're welcome.",
            "As I mentioned in my previous email. And the one before that.",
            "Circling back because apparently my first circle wasn't circular enough.",
            "Per my last email, which you clearly didn't read,",
            "Following up on my follow-up to my initial follow-up,",
            "Looping back around for the third time this week,",
            "Since I haven't heard back (hint hint),",
            "Just wanted to make sure this didn't get lost in the shuffle (it did, didn't it?),",
            "Floating this back to the top of your 847 unread emails,",
            "Re-sending because my previous send apparently entered a black hole,",
            "Taking another lap on this particular topic,",
        },
        [EmailTone.Panicked] = new[]
        {
            "URGENT: And I mean actually urgent this time.",
            "911 EMERGENCY (corporate edition):",
            "This is not a drill. Unlike last week's drill.",
            "Drop everything. Yes, even the TPS reports.",
            "CODE RED (or whatever color means 'we're in trouble'):",
            "DEFCON 1 - All hands on deck:",
            "Houston, we have a problem. Multiple problems, actually.",
            "Everything is fine. Everything is NOT fine.",
            "You're going to want to sit down for this one.",
            "Remember when we said nothing could go wrong? About that...",
            "So... funny story. Not actually funny.",
            "I have news. It's not good news. It's the opposite of good news.",
            "Before you read this, know that I've already updated my LinkedIn,",
        },
        [EmailTone.Obsequious] = new[]
        {
            "Your brilliant strategy, as always, has inspired us!",
            "In awe of your visionary leadership,",
            "Your wisdom continues to guide us through these challenging times!",
            "Once again, your instincts were absolutely correct!",
            "As predicted by your keen executive intuition,",
            "Your decision-making prowess knows no bounds!",
            "Marveling at your strategic foresight,",
            "Your leadership style is truly an inspiration to us all,",
            "In complete alignment with your transformative vision,",
            "Standing in admiration of your executive excellence,",
        },
        [EmailTone.Passive] = new[]
        {
            "Not to point fingers, but if I were pointing fingers...",
            "I'm not saying I told you so, but I did send that email...",
            "As I may have subtly hinted in my previous 12 messages,",
            "I don't want to say 'I warned you,' so I'll just forward my previous warning,",
            "Without assigning blame (but definitely noting who was in that meeting),",
            "Just for the record, and I'm cc'ing Legal on this,",
            "I hate to be the one to bring this up again, but someone has to,",
            "Not that anyone asked for my opinion (again),",
            "As I believe I mentioned... several times... in writing...",
            "Funny story - remember when I raised this exact concern?",
            "I'm sure there's a perfectly good explanation for ignoring my advice,",
        },
        [EmailTone.Enthusiastic] = new[]
        {
            "AMAZING NEWS! (And I don't use caps lightly!)",
            "You won't BELIEVE what just happened!",
            "Hold onto your ergonomic chairs!",
            "This is the email I've been waiting to send all quarter!",
            "I'm literally doing a happy dance at my standing desk!",
            "Cancel your afternoon meetings - we're celebrating!",
            "This calls for the GOOD conference room snacks!",
            "Someone cue the motivational music!",
            "This is the kind of win that gets mentioned in all-hands!",
            "I may have just fist-pumped in my cubicle!",
        },
        [EmailTone.Cryptic] = new[]
        {
            "Regarding the matter we discussed. You know the one.",
            "Further developments on 'Project X' (you know what X is),",
            "The situation has... evolved.",
            "We need to talk. In person. No paper trail.",
            "Some things are better discussed offline, if you catch my drift,",
            "Let's just say the chickens have come home to roost,",
            "Remember what we talked about? It's happening.",
            "I trust you'll understand the subtext here,",
            "Between us (and anyone else reading this, hi Legal),",
        },
        [EmailTone.Blunt] = new[]
        {
            "Let's skip the corporate pleasantries:",
            "I'm going to be direct because I've run out of euphemisms:",
            "Here's the thing, and I'm not going to sugarcoat it:",
            "Bottom line, no spin:",
            "Cards on the table, no BS:",
            "I'm just going to say it:",
            "No amount of corporate speak can soften this, so here goes:",
            "Stripping away the jargon for once:",
            "Real talk:",
            "I don't have time for buzzwords today:",
        }
    };

    // === CLOSINGS BY TONE ===

    private static readonly Dictionary<EmailTone, string[]> HumorousClosings = new()
    {
        [EmailTone.Professional] = new[]
        {
            "Best regards (sent from my phone while pretending to listen in a meeting),",
            "Synergistically yours,",
            "Regards, and remember: there is no 'I' in team (but there is in 'email'),",
            "Best, and please don't reply-all,",
            "Regards (this email took 45 minutes to write),",
            "Warmest corporate sentiments,",
            "Professionally yours (even on Fridays),",
            "Best, and please update the Jira ticket,",
            "Regards, sent during my third meeting of the day,",
            "May your inbox have mercy on you,",
        },
        [EmailTone.Aloof] = new[]
        {
            "Let me know if you have questions. I won't have answers, but let me know.",
            "Happy to discuss in a meeting that could have been this email.",
            "Let's take this offline and never speak of it again.",
            "I'll wait. Not patiently, but I'll wait.",
            "Please advise (by which I mean, please make a decision so I don't have to).",
            "Feel free to loop in whoever else needs to be looped. It's loops all the way down.",
            "Looking forward to your reply (sent 3 weeks from now).",
            "I'll be here. At my desk. Waiting. Forever.",
            "This will be my last follow-up. (It won't be.)",
        },
        [EmailTone.Panicked] = new[]
        {
            "Please respond ASAP or I will assume the worst. I'm already assuming the worst.",
            "Awaiting your urgent response from under my desk.",
            "Send help. Or coffee. Preferably both.",
            "I'll be in the server room crying if you need me.",
            "Your immediate attention is required before I have a meltdown.",
            "Please confirm receipt so I know someone is out there.",
            "Refreshing my inbox every 30 seconds,",
            "Standing by with fire extinguisher,",
            "My anxiety is typing this,",
        },
        [EmailTone.Obsequious] = new[]
        {
            "At your service, always!",
            "Eagerly awaiting the opportunity to execute your vision!",
            "Your wish is my OKR!",
            "Ready to pivot at a moment's notice!",
            "Standing by for your transformative guidance!",
            "Honored to be part of your journey to excellence!",
            "Your humble servant in synergy,",
            "Forever your devoted direct report,",
        },
        [EmailTone.Passive] = new[]
        {
            "But what do I know, I'm just in operations.",
            "Anyway, I've said my piece. Again.",
            "I'll just be over here, not saying 'I told you so.'",
            "Consider this my official 'I raised this concern' documentation.",
            "For the record. The permanent record.",
            "Just wanted this in writing. You know, for reasons.",
            "I'm sure it will all work out fine. (It won't.)",
            "No need to thank me for the heads up. Seriously, no one ever does.",
            "Filing this under 'Things I Warned About,'",
        },
        [EmailTone.Enthusiastic] = new[]
        {
            "Can't wait to high-five you at the next all-hands!",
            "This is going in the company newsletter!",
            "Let's gooooo!",
            "Onward and upward!",
            "The future is bright and so are we!",
            "Team awesome strikes again!",
            "Victory dance pending,",
            "Popping metaphorical champagne,",
        },
        [EmailTone.Cryptic] = new[]
        {
            "We'll speak soon. Or not. Depending.",
            "More to follow. Maybe.",
            "Handle this as you see fit. I trust your judgment. Mostly.",
            "The situation will resolve itself. One way or another.",
            "Let's discuss in person. Bring coffee. Strong coffee.",
            "You'll know what to do. At least, I hope you do.",
            "Delete this email after reading. (Just kidding. Kind of.)",
        },
        [EmailTone.Blunt] = new[]
        {
            "That's it. That's the email.",
            "Fix it.",
            "Ball's in your court.",
            "Handle it.",
            "Good luck. You'll need it.",
            "I've said what I needed to say.",
            "Your move.",
            "Don't make me send another email about this.",
        }
    };

    // === OUTCOME RESPONSES ===

    private static readonly Dictionary<OutcomeTier, string[]> OutcomeResponses = new()
    {
        [OutcomeTier.Good] = new[]
        {
            "Against all odds and several actuarial tables, this actually worked. The consultants are baffled.",
            "I don't want to say it was a miracle, but the break room oracle (Carol from Accounting) predicted this.",
            "Everything went according to plan. I know, I'm as shocked as you are.",
            "Success! The kind they'll put in the quarterly newsletter. Maybe even with a stock photo.",
            "It worked. It actually worked. I need to sit down. These are tears of joy, not stress.",
            "We did it! Someone notify the motivational poster committee!",
            "This went so well that Legal is suspicious. They're checking for hidden clauses.",
            "The metrics are green across the board. I didn't even know they could BE green.",
            "Exceeded all KPIs. Someone get this on a commemorative plaque.",
            "This is the outcome we tell the interns about to give them false hope.",
            "Results are in: we actually delivered. HR wants to know if we can bottle this.",
            "I've already drafted the LinkedIn humble-brag. It's magnificent.",
            "The spreadsheet is glowing. Metaphorically. And a little bit literally - IT is looking into it.",
        },
        [OutcomeTier.Expected] = new[]
        {
            "Results are in: aggressively mediocre. Right on brand.",
            "We achieved exactly what was expected. No more, no less. The prophecy is fulfilled.",
            "Mission adequately accomplished. Just like the banner won't say.",
            "The initiative produced results that can best be described as 'fine.' Just fine.",
            "We've achieved the corporate equivalent of a participation trophy.",
            "Outcome: exactly what happens when you follow the process. Process'd.",
            "Results are 'meets expectations.' The most lukewarm of all feedback.",
            "Not a home run, not a strikeout. More of a bunt. A corporate bunt.",
            "We did the thing. The thing is done. Adequately.",
            "Everything went okay. Just okay. Painfully, boringly okay.",
            "The metrics are stable. Like a patient in a coma. Alive, but not thriving.",
            "We've maintained the status quo. The quo remains statusy.",
            "Outcome: firmly in the 'could be worse' category.",
            "Results: satisfactory. The DMV of performance ratings.",
        },
        [OutcomeTier.Bad] = new[]
        {
            "So... that didn't go as planned. To put it mildly. Very mildly.",
            "Results are in, and I've pre-drafted the post-mortem. It's mostly apologies.",
            "What happened here will be studied in future training materials under 'What Not To Do.'",
            "The good news: we've learned valuable lessons. The bad news: everything else.",
            "This has become what we in the business call 'a situation.' Capital S.",
            "I've seen better outcomes from magic 8-balls. At least those offer hope.",
            "The metrics are red. Very red. 'Stop sign in a ketchup factory' red.",
            "This will definitely come up in the quarterly review. And the annual review. And possibly my exit interview.",
            "If this were a movie, we'd be at the part where dramatic music plays.",
            "We've achieved a new benchmark for what not to do. At least we're setting records.",
            "Legal has already cleared their afternoon schedule. That's never good.",
            "On a scale of 1 to 'we should talk to a lawyer,' we're somewhere around 'definitely talk to a lawyer.'",
            "The outcome has inspired several updates to our risk assessment matrix.",
            "I'm not saying it's a disaster, but the Titanic's first mate just called to compare notes.",
        }
    };

    // === CRISIS OPENINGS ===

    private static readonly string[] CrisisPanicOpenings = new[]
    {
        "CEO, we have a situation that requires your immediate attention. And possibly a stiff drink.",
        "I don't know how to say this, so I'll just say it: we're in trouble.",
        "Remember how we said 'what's the worst that could happen?' We found out.",
        "Urgent news, and I want you to know I've already updated my resume. Just in case.",
        "This just landed on my desk and I'm forwarding it to yours because misery loves company.",
        "We need to talk. Specifically about why everything is on fire. Metaphorically. Mostly metaphorically.",
        "I regret to inform you that the situation has... escalated.",
        "Good news: we're trending on social media. Bad news: you should see why.",
        "I'm going to need you to clear your calendar. Maybe your whole week.",
        "This is the kind of email I hoped I'd never have to send. And yet, here we are.",
        "Something has come up. Something significant. Something requiring adult supervision.",
        "There's no easy way to say this, so I've asked ChatGPT to help. It also panicked.",
    };

    // === CRISIS RESOLUTION TEMPLATES ===

    private static readonly string[] GoodResolutionBodies = new[]
    {
        "Against all expectations (and my personal anxiety spiral), your decision to \"{choice}\" actually worked. The crisis team is in shock. The good kind of shock, for once.\n\nThe situation has been fully resolved. Legal is calling it 'a textbook response,' which is corporate for 'we can't believe that worked.'",
        "I'm writing to confirm that \"{choice}\" was, in fact, the correct call. The interns are already taking notes for the company mythology.\n\nCrisis averted. Somewhere, a management consultant is adding this to their case studies.",
        "Your strategic decision to \"{choice}\" has resulted in what we're calling 'optimal outcome realization.' That's corporate speak for 'you nailed it.'\n\nThe board has been informed. Patricia Sterling was overheard saying 'adequate.' From her, that's practically a parade.",
        "The \"{choice}\" approach worked perfectly. I've already seen three people update their LinkedIn with 'helped navigate crisis' (they didn't, but that's LinkedIn for you).\n\nAll metrics recovering. Team morale through the roof. Carol from Accounting says the tea leaves predicted this.",
    };

    private static readonly string[] ExpectedResolutionBodies = new[]
    {
        "Your decision to \"{choice}\" has produced... results. They're not great results, but they're not disaster results either.\n\nThink of it as the 'C+' of crisis management. Solidly average. Aggressively mediocre.",
        "Following through on \"{choice}\" resolved the immediate crisis, though 'resolved' might be generous. Let's say 'stabilized.' 'Managed.' 'No longer actively hemorrhaging.'\n\nThe fires are mostly out. Some embers remain. We have extinguishers.",
        "The \"{choice}\" strategy worked about as well as expected, which is to say: the building is still standing, but we should probably inspect the foundation.\n\nWe've survived to fight another day. That's... that's something.",
        "Implementing \"{choice}\" produced mixed results. On one hand, crisis contained. On the other hand, stakeholders remain 'concerned.' That's corporate for 'annoyed but not yet litigious.'",
    };

    private static readonly string[] BadResolutionBodies = new[]
    {
        "I regret to inform you that \"{choice}\" did not go as planned. 'As planned' would have involved less screaming.\n\nLegal is drafting talking points. HR is fielding calls. The PR team has aged visibly.",
        "The decision to \"{choice}\" has resulted in what we're diplomatically calling 'suboptimal outcomes.' Less diplomatically: we've created a situation.\n\nThe good news is we now have a very thorough 'what not to do' guide. The bad news is everything else.",
        "Following through on \"{choice}\" has... backfired. Spectacularly. In a way that will be discussed in future training materials.\n\nThe board has been notified. Patricia Sterling sent back a single emoji. It was not a happy one.",
        "Your choice to \"{choice}\" has generated what I can only describe as 'consequences.' Multiple, cascading consequences.\n\nI've begun drafting the post-mortem. It's currently 47 pages and I'm only at the first hour.",
    };

    // === BOARD DIRECTIVE TEMPLATES ===

    private static readonly string[] DirectiveOpenings = new[]
    {
        "The board has concluded their quarterly meditation on profits and emerged with demands. I mean, 'expectations.'",
        "Fresh from the mahogany-paneled boardroom comes your new set of impossible targets!",
        "Patricia Sterling has once again consulted the sacred spreadsheets and divined your quarterly fate.",
        "The board has spoken. Their words echo through the halls like disappointed sighs at a earnings call.",
        "Quarterly expectations have been calculated using a formula known only to the board and one very nervous accountant.",
    };

    private static readonly string[] DirectivePressureComments = new[]
    {
        "The board's patience is not infinite. (This is their gentle reminder.)",
        "Performance improvement expectations are being... adjusted upward.",
        "The board notes that your predecessor also received this memo. Shortly before their departure.",
        "Consider this a friendly reminder that 'job security' is just a theoretical concept.",
        "The board wishes to emphasize that these targets are 'ambitious but achievable.' (The 'achievable' part is debatable.)",
    };

    // === SELECT METHODS ===

    private static T SelectVariant<T>(T[] options, int seed, string eventId)
    {
        if (options.Length == 0)
            throw new ArgumentException("Options array cannot be empty", nameof(options));

        var hash = HashCode.Combine(seed, eventId);
        var index = Math.Abs(hash) % options.Length;
        return options[index];
    }

    public string GetOpening(EmailContentContext context)
    {
        var options = HumorousOpenings.GetValueOrDefault(context.Tone, HumorousOpenings[EmailTone.Professional]);
        return SelectVariant(options, context.Seed, context.EventId);
    }

    public string GetClosing(EmailContentContext context)
    {
        var options = HumorousClosings.GetValueOrDefault(context.Tone, HumorousClosings[EmailTone.Professional]);
        return SelectVariant(options, context.Seed, context.EventId);
    }

    public string GetOutcomeBody(EmailContentContext context, OutcomeTier outcome)
    {
        var options = OutcomeResponses[outcome];
        return SelectVariant(options, context.Seed, context.EventId);
    }

    public string GetCrisisBody(EmailContentContext context, string crisisTitle, string crisisDescription)
    {
        var opening = SelectVariant(CrisisPanicOpenings, context.Seed, context.EventId + "_opening");

        return $"{opening}\n\n{crisisDescription}\n\nThe team awaits your guidance. Anxiously.";
    }

    public string GetCrisisResolutionBody(EmailContentContext context, string crisisTitle, string choiceLabel, OutcomeTier outcome)
    {
        var templates = outcome switch
        {
            OutcomeTier.Good => GoodResolutionBodies,
            OutcomeTier.Expected => ExpectedResolutionBodies,
            OutcomeTier.Bad => BadResolutionBodies,
            _ => ExpectedResolutionBodies
        };

        var template = SelectVariant(templates, context.Seed, context.EventId);
        return template.Replace("{choice}", choiceLabel);
    }

    public string GetBoardDirectiveBody(EmailContentContext context, string directiveTitle, int requiredAmount, int quarterNumber, int pressureLevel)
    {
        var opening = SelectVariant(DirectiveOpenings, context.Seed, context.EventId + "_opening");
        var pressureComment = pressureLevel >= 3
            ? SelectVariant(DirectivePressureComments, context.Seed, context.EventId + "_pressure")
            : "We trust you will rise to the occasion.";

        return $"""
            Dear CEO,

            {opening}

            QUARTERLY DIRECTIVE: {directiveTitle}
            TARGET: +${requiredAmount}M profit increase this quarter

            {pressureComment}

            The board will be watching. They're always watching.

            P.S. — The previous CEO's nameplate has been recycled. Just FYI.
            """;
    }
}
