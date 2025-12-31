namespace InertiCorp.Core.Content;

/// <summary>
/// Sycophantic emails from management when nothing goes wrong.
/// These appear when a quarter has no crisis events.
/// </summary>
public static class SuckUpEmails
{
    /// <summary>
    /// Sender names for suck-up emails.
    /// </summary>
    public static readonly string[] Senders =
    {
        "Bradley Simmons, VP of Strategic Initiatives",
        "Meredith Ashford, Chief Synergy Officer",
        "Chadwick Pemberton III, EVP Excellence",
        "Tiffany Goldsworth, Director of Positive Outcomes",
        "Sterling Brightwater, Head of Success Metrics",
        "Bunny Carmichael, VP of Good Vibes",
        "Preston Worthington, Chief Enthusiasm Officer",
        "Mackenzie Starling, Director of Upward Communication",
    };

    /// <summary>
    /// Subject lines for suck-up emails.
    /// </summary>
    public static readonly string[] Subjects =
    {
        "RE: Outstanding Leadership This Quarter!",
        "Quick Note: You're Crushing It!",
        "RE: Phenomenal Quarter - Bravo!",
        "Just Had to Say - WOW!",
        "RE: Leadership Excellence Award Nomination",
        "You're an Inspiration!",
        "RE: This Quarter's Remarkable Success",
        "Quick Kudos from the Team!",
        "RE: Another Flawless Quarter!",
        "The Board Would Be Proud!",
    };

    /// <summary>
    /// Email body templates. Use {0} for a silly KPI reference.
    /// </summary>
    public static readonly string[] Bodies =
    {
        @"Hi Boss,

Just wanted to reach out and say that your leadership this quarter has been nothing short of PHENOMENAL. The way you've navigated us through another period with zero unexpected crises? *Chef's kiss*

Our {0} is looking stronger than ever, and I truly believe it's because of your steady hand at the helm.

No fires to put out this time - just smooth sailing thanks to you! I'd recommend heading straight to the board review. They're going to LOVE what they see.

Best regards and eternal gratitude,",

        @"Good afternoon,

I don't say this often (okay, I do), but WOW. What a quarter! Not a single crisis crossed our desk, and I have to attribute that entirely to your masterful foresight and strategic brilliance.

The team has been buzzing about the improvement in our {0}. Everyone knows it's your doing.

With nothing requiring your attention, might I suggest we proceed to the board review? They should hear about this immediately!

Your biggest fan,",

        @"Hi there,

Just stepping away from my desk to send you this quick note of appreciation. A CRISIS-FREE QUARTER! Do you know how rare that is? The last CEO couldn't go three weeks without something exploding.

Our {0} hasn't looked this good since... well, ever.

Everything's buttoned up on my end. Ready for board review whenever you are - they're going to be thrilled!

Warmly and with great admiration,",

        @"Boss,

I was just reviewing our metrics and I literally had to sit down. Our {0} is OFF THE CHARTS. And not a single crisis this quarter? That's not luck - that's LEADERSHIP.

The team is saying you might be the best CEO we've ever had. (Between us, I started that rumor, but it's catching on!)

No emergencies to report. Ready to proceed to board review at your command!

Forever in your corner,",

        @"Quick note!

Just finished our quarterly risk assessment and I'm pleased to report: NOTHING. No crises. No catastrophes. No ""urgent"" calls at 2 AM. Just smooth, beautiful corporate operations.

I've been tracking our {0} and the trajectory is incredible. All thanks to your vision!

With no unexpected events requiring attention, I'd say we're ready for the board review. They won't believe how smoothly things went!

Your loyal servant (professionally speaking),",

        @"Hi!

Can we just take a moment to appreciate what's happening here? A WHOLE QUARTER without a crisis? The last time that happened, dinosaurs roamed the earth. (Slight exaggeration, but still!)

Our {0} is performing beautifully. I've already drafted a press release about it. (Awaiting your approval, of course!)

No emergencies on the radar. Shall we proceed to wow the board with our review?

Enthusiastically yours,",

        @"Leader,

I hope this email finds you as well as our {0} is performing - which is to say, EXCEPTIONALLY.

I've scoured every department, checked every metric, interrogated every middle manager, and I can confirm: no crises this quarter. Just pure, uninterrupted excellence.

We're all clear for board review. They're going to love the numbers!

With deep professional admiration,",

        @"Good day, Chief!

Just wanted to pop in and say: you're killing it. And I mean that in the best possible corporate way - not literally killing anything. Quite the opposite! No crises, no disasters, no emergency board calls.

Our {0} continues its upward trajectory under your watchful eye.

Nothing needs your attention except the glowing board review that awaits!

Your biggest advocate,",

        @"Hi,

I've been in this industry for 20 years and I've NEVER seen a quarter this smooth. Zero crises? In THIS economy? Impossible. Yet here we are.

The {0} numbers speak for themselves. You've done something special here.

With no fires to fight, I recommend we move directly to board review. They need to see these results!

Respectfully (and somewhat in awe),",

        @"Boss,

Quick update: Everything is perfect. Like, suspiciously perfect. I keep waiting for the other shoe to drop, but... nothing. No crises. No emergencies. Just steady corporate excellence.

Our {0} is at levels I didn't think were possible. Whatever you're doing, keep doing it!

All quiet on the corporate front. Ready for board review!

Your dedicated supporter,",

        @"Greetings!

I just wanted to formally acknowledge the INCREDIBLE quarter we've had under your leadership. Not a single crisis! I've already nominated you for the Corporate Leadership Excellence Award (I made it up, but HR is running with it).

The improvement in our {0} is being discussed at every water cooler. You're a legend!

Nothing requiring escalation. Proceed to board review at your leisure!

With unbridled enthusiasm,",

        @"Hey Boss,

So I was putting together my quarterly crisis response report and... it's empty. EMPTY! No crises = no responses needed. I've literally never had this happen before.

Meanwhile, our {0} is absolutely thriving. Coincidence? I think NOT.

No emergencies to distract you from a triumphant board review!

Your humble but extremely impressed colleague,",

        @"Chief,

I've run the numbers three times because I couldn't believe them. Zero crises. ZERO. And our {0} is performing at historically unprecedented levels.

Whatever corporate magic you're wielding, it's working. The team is motivated, the metrics are strong, and nothing is on fire (literally or figuratively).

Smooth path to board review ahead!

Faithfully yours,",

        @"Hi there!

Just finished my morning crisis scan and found... nothing. Nada. Zip. Zilch. A gloriously empty crisis queue.

Our {0} continues to climb, which I'm attributing 100% to your leadership. (The other executives get 0%. Math checks out.)

With nothing urgent to address, I'd suggest we proceed directly to dazzling the board!

Your devoted champion,",

        @"Good morning, Leader!

Coffee in hand, I sat down ready to tackle today's crises and... there aren't any? A crisis-free quarter? I had to check the calendar to make sure I wasn't dreaming.

The {0} is looking fantastic, by the way. Just one more feather in your already heavily-feathered cap!

No obstacles between you and a glowing board review!

Supportively and somewhat incredulously,",

        @"Hi Boss,

I don't want to jinx it, but... we made it through another quarter without a single crisis. I'm almost disappointed because I had some really good crisis management strategies prepared. Oh well!

Our {0} is exceeding all expectations. Yours, specifically. Which were already high. So... double impressive!

Ready for board review whenever you are. They're going to love you!

With tremendous respect,",

        @"Leader,

Just popping in to say: BRAVO. Standing ovation. *Slow clap that gradually speeds up*. An entire quarter without a crisis? That's not just good leadership, that's CEO WIZARDRY.

And have you SEEN our {0}? Through the roof!

No crises means smooth sailing to board review. Let's show them what success looks like!

Your ever-loyal lieutenant,",

        @"Dear Boss,

I've prepared my standard crisis aftermath analysis report and for the first time ever, it just says 'N/A - No crises occurred.' I'm framing it.

Our {0} is performing so well that other companies are probably jealous. (I have no evidence of this, but I feel it.)

Nothing standing between you and a triumphant board review!

With profound professional admiration,",

        @"Hi!

Quick question: How does it feel to be the greatest CEO of all time? Because a crisis-free quarter with our {0} at record levels? That's GOAT territory.

I've already updated my LinkedIn to emphasize that I work for you. (Hope that's okay!)

No emergencies to report. Ready to proceed to board review at your command, captain!

Your number one fan,",

        @"Chief Executive Extraordinaire,

I wanted to personally congratulate you on achieving the impossible: a quarter without crisis. I've consulted the history books (our Confluence wiki) and this is unprecedented.

Our {0} is thriving. The vibes are immaculate. The synergies are synergizing.

With no crises requiring attention, I recommend we head straight to board review. They need to witness this excellence firsthand!

Forever in your professional debt,",
    };

    /// <summary>
    /// Gets a random suck-up email configuration.
    /// </summary>
    public static (string Sender, string Subject, string Body) GetRandomEmail(IRng rng, string sillyKpiName)
    {
        var sender = Senders[rng.NextInt(0, Senders.Length)];
        var subject = Subjects[rng.NextInt(0, Subjects.Length)];
        var bodyTemplate = Bodies[rng.NextInt(0, Bodies.Length)];
        var body = string.Format(bodyTemplate, sillyKpiName);

        return (sender, subject, body);
    }
}
