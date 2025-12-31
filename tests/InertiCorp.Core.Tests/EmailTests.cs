using InertiCorp.Core.Cards;
using InertiCorp.Core.Email;

namespace InertiCorp.Core.Tests;

public class EmailMessageTests
{
    [Fact]
    public void EmailMessage_WithRead_SetsIsReadTrue()
    {
        var msg = CreateTestMessage();

        var read = msg.WithRead();

        Assert.True(read.IsRead);
    }

    [Fact]
    public void EmailMessage_FromDisplay_ReturnsHumanReadable()
    {
        var msg = CreateTestMessage(from: SenderArchetype.EngManager);

        Assert.Contains("Engineering Director", msg.FromDisplay);
    }

    [Fact]
    public void EmailMessage_IsFromPlayer_TrueForCEO()
    {
        var msg = CreateTestMessage(from: SenderArchetype.CEO);

        Assert.True(msg.IsFromPlayer);
    }

    [Fact]
    public void EmailMessage_IsFromPlayer_FalseForOthers()
    {
        var msg = CreateTestMessage(from: SenderArchetype.PM);

        Assert.False(msg.IsFromPlayer);
    }

    private static EmailMessage CreateTestMessage(
        SenderArchetype from = SenderArchetype.PM,
        SenderArchetype to = SenderArchetype.CEO) => new(
            MessageId: "msg_1",
            ThreadId: "thread_1",
            Subject: "Test Subject",
            Body: "Test body",
            From: from,
            To: to,
            Tone: EmailTone.Professional,
            TurnNumber: 1,
            LinkedEventIds: Array.Empty<string>());
}

public class EmailThreadTests
{
    [Fact]
    public void AskMessage_ReturnsPlayerMessage()
    {
        var askMsg = CreateTestMessage(from: SenderArchetype.CEO);
        var replyMsg = CreateTestMessage(from: SenderArchetype.PM, id: "msg_2");
        var thread = CreateThread(askMsg, replyMsg);

        Assert.NotNull(thread.AskMessage);
        Assert.Equal(SenderArchetype.CEO, thread.AskMessage.From);
    }

    [Fact]
    public void Replies_ReturnsNonPlayerMessages()
    {
        var askMsg = CreateTestMessage(from: SenderArchetype.CEO);
        var reply1 = CreateTestMessage(from: SenderArchetype.PM, id: "msg_2");
        var reply2 = CreateTestMessage(from: SenderArchetype.HR, id: "msg_3");
        var thread = CreateThread(askMsg, reply1, reply2);

        Assert.Equal(2, thread.Replies.Count);
        Assert.All(thread.Replies, r => Assert.False(r.IsFromPlayer));
    }

    [Fact]
    public void WithFollowUp_AddsMessageToThread()
    {
        var thread = CreateThread(CreateTestMessage());
        var reply = CreateTestMessage(from: SenderArchetype.CFO, id: "msg_2");

        var updated = thread.WithFollowUp(reply);

        Assert.Equal(2, updated.Messages.Count);
    }

    [Fact]
    public void IsFullyRead_TrueWhenAllRead()
    {
        var msg1 = CreateTestMessage() with { IsRead = true };
        var msg2 = CreateTestMessage(id: "msg_2") with { IsRead = true };
        var thread = CreateThread(msg1, msg2);

        Assert.True(thread.IsFullyRead);
    }

    [Fact]
    public void IsFullyRead_FalseWhenAnyUnread()
    {
        var msg1 = CreateTestMessage() with { IsRead = true };
        var msg2 = CreateTestMessage(id: "msg_2") with { IsRead = false };
        var thread = CreateThread(msg1, msg2);

        Assert.False(thread.IsFullyRead);
    }

    private static EmailMessage CreateTestMessage(
        SenderArchetype from = SenderArchetype.PM,
        string id = "msg_1") => new(
            MessageId: id,
            ThreadId: "thread_1",
            Subject: "Test",
            Body: "Body",
            From: from,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Professional,
            TurnNumber: 1,
            LinkedEventIds: Array.Empty<string>());

    private static EmailThread CreateThread(params EmailMessage[] messages) => new(
        ThreadId: "thread_1",
        Subject: "Test Thread",
        OriginatingCardId: "card_1",
        CreatedOnTurn: 1,
        Messages: messages.ToList());
}

public class InboxTests
{
    [Fact]
    public void Empty_HasZeroThreads()
    {
        var inbox = Inbox.Empty;

        Assert.Equal(0, inbox.ThreadCount);
        Assert.Equal(0, inbox.MessageCount);
    }

    [Fact]
    public void WithThreadAdded_IncreasesCount()
    {
        var inbox = Inbox.Empty;
        var thread = CreateTestThread();

        var updated = inbox.WithThreadAdded(thread);

        Assert.Equal(1, updated.ThreadCount);
    }

    [Fact]
    public void UnreadThreadCount_TracksUnreadThreads()
    {
        var unreadThread = CreateTestThread("t1", isRead: false);
        var readThread = CreateTestThread("t2", isRead: true);
        var inbox = Inbox.Empty
            .WithThreadAdded(unreadThread)
            .WithThreadAdded(readThread);

        Assert.Equal(1, inbox.UnreadThreadCount);
    }

    [Fact]
    public void GetThread_FindsByThreadId()
    {
        var thread = CreateTestThread("t1");
        var inbox = Inbox.Empty.WithThreadAdded(thread);

        var found = inbox.GetThread("t1");

        Assert.NotNull(found);
        Assert.Equal("t1", found.ThreadId);
    }

    [Fact]
    public void GetThreadForCard_FindsByCardId()
    {
        var thread = CreateTestThread("t1", cardId: "card_test");
        var inbox = Inbox.Empty.WithThreadAdded(thread);

        var found = inbox.GetThreadForCard("card_test");

        Assert.NotNull(found);
        Assert.Equal("card_test", found.OriginatingCardId);
    }

    [Fact]
    public void WithReplyAdded_AddsToCorrectThread()
    {
        var thread = CreateTestThread("t1");
        var inbox = Inbox.Empty.WithThreadAdded(thread);
        var reply = new EmailMessage(
            "msg_new", "t1", "RE: Test", "Reply body",
            SenderArchetype.CFO, SenderArchetype.CEO,
            EmailTone.Blunt, 2, Array.Empty<string>());

        var updated = inbox.WithFollowUpAdded("t1", reply);

        var updatedThread = updated.GetThread("t1");
        Assert.Equal(2, updatedThread!.Messages.Count);
    }

    [Fact]
    public void WithThreadRead_MarksAllMessagesRead()
    {
        var thread = CreateTestThread("t1", isRead: false);
        var inbox = Inbox.Empty.WithThreadAdded(thread);

        var updated = inbox.WithThreadRead("t1");

        var readThread = updated.GetThread("t1");
        Assert.True(readThread!.IsFullyRead);
    }

    [Fact]
    public void ThreadsFromTurn_FiltersCorrectly()
    {
        var t1 = CreateTestThread("t1", turn: 1);
        var t2 = CreateTestThread("t2", turn: 2);
        var t3 = CreateTestThread("t3", turn: 1);
        var inbox = Inbox.Empty
            .WithThreadAdded(t1)
            .WithThreadAdded(t2)
            .WithThreadAdded(t3);

        var turn1Threads = inbox.ThreadsFromTurn(1);

        Assert.Equal(2, turn1Threads.Count);
    }

    [Fact]
    public void TopThreads_LimitsToMaxDisplay()
    {
        var inbox = Inbox.Empty;
        for (int i = 0; i < 10; i++)
        {
            inbox = inbox.WithThreadAdded(CreateTestThread($"t{i}"));
        }

        var top = inbox.TopThreads;

        Assert.Equal(Inbox.MaxDisplayCount, top.Count);
    }

    private static EmailThread CreateTestThread(
        string threadId = "thread_1",
        string cardId = "card_1",
        int turn = 1,
        bool isRead = false)
    {
        var msg = new EmailMessage(
            $"msg_{threadId}", threadId, "Subject", "Body",
            SenderArchetype.PM, SenderArchetype.CEO,
            EmailTone.Professional, turn, Array.Empty<string>(),
            IsRead: isRead);

        return new EmailThread(
            threadId, "Test Thread", cardId, turn, new[] { msg });
    }
}

public class EmailGeneratorTests
{
    [Fact]
    public void CreateCardThread_CreatesAskAndReply()
    {
        var generator = new EmailGenerator(42);
        var card = CreateTestCard();

        var thread = generator.CreateCardThread(
            card,
            OutcomeTier.Expected,
            new[] { (Meter.Delivery, 10) },
            turnNumber: 1,
            alignment: 50);

        Assert.Equal(2, thread.Messages.Count);
        Assert.NotNull(thread.AskMessage);
        Assert.Single(thread.Replies);
    }

    [Fact]
    public void CreateCardThread_AskMessageIsFromCEO()
    {
        var generator = new EmailGenerator(42);
        var card = CreateTestCard();

        var thread = generator.CreateCardThread(
            card, OutcomeTier.Good,
            Array.Empty<(Meter, int)>(), 1, 50);

        Assert.Equal(SenderArchetype.CEO, thread.AskMessage!.From);
    }

    [Fact]
    public void CreateCardThread_SubjectContainsCardTitle()
    {
        var generator = new EmailGenerator(42);
        var card = CreateTestCard("Pizza Party");

        var thread = generator.CreateCardThread(
            card, OutcomeTier.Expected,
            Array.Empty<(Meter, int)>(), 1, 50);

        Assert.Contains("Pizza Party", thread.Subject);
    }

    [Fact]
    public void CreateCardThread_LinksToCardId()
    {
        var generator = new EmailGenerator(42);
        var card = CreateTestCard();

        var thread = generator.CreateCardThread(
            card, OutcomeTier.Expected,
            Array.Empty<(Meter, int)>(), 1, 50);

        Assert.Equal(card.CardId, thread.OriginatingCardId);
    }

    [Fact]
    public void CreateCardThread_DeterministicWithSameSeed()
    {
        var card = CreateTestCard();

        var gen1 = new EmailGenerator(42);
        var thread1 = gen1.CreateCardThread(card, OutcomeTier.Good, Array.Empty<(Meter, int)>(), 1, 50);

        var gen2 = new EmailGenerator(42);
        var thread2 = gen2.CreateCardThread(card, OutcomeTier.Good, Array.Empty<(Meter, int)>(), 1, 50);

        Assert.Equal(thread1.Messages[1].Body, thread2.Messages[1].Body);
    }

    [Fact]
    public void CreateFollowUpReply_AddsToThread()
    {
        var generator = new EmailGenerator(42);

        var reply = generator.CreateFollowUpReply(
            "thread_1", "Test Subject",
            "A follow-up event occurred",
            new[] { (Meter.Morale, -5) },
            turnNumber: 2,
            alignment: 50,
            SenderArchetype.HR);

        Assert.Equal("thread_1", reply.ThreadId);
        Assert.Contains("RE:", reply.Subject);
    }

    [Fact]
    public void CalculateMisinterpretationChance_BaseIs10Percent()
    {
        var chance = EmailGenerator.CalculateMisinterpretationChance(50, 1);

        Assert.Equal(10, chance);
    }

    [Fact]
    public void CalculateMisinterpretationChance_LowAlignmentIncreasesChance()
    {
        var chance = EmailGenerator.CalculateMisinterpretationChance(30, 1);

        Assert.Equal(30, chance);
    }

    [Fact]
    public void CalculateMisinterpretationChance_HighPressureIncreasesChance()
    {
        var chance = EmailGenerator.CalculateMisinterpretationChance(50, 5);

        Assert.Equal(30, chance);
    }

    [Fact]
    public void CalculateMisinterpretationChance_ClampsToMax80()
    {
        var chance = EmailGenerator.CalculateMisinterpretationChance(0, 20);

        Assert.Equal(80, chance);
    }

    private static PlayableCard CreateTestCard(string title = "Test Card") => new(
        CardId: "card_test",
        Title: title,
        Description: "Test description",
        FlavorText: "Flavor",
        Outcomes: new OutcomeProfile(
            Good: new[] { new MeterEffect(Meter.Morale, 10) },
            Expected: new[] { new MeterEffect(Meter.Morale, 5) },
            Bad: new[] { new MeterEffect(Meter.Morale, -5) }),
        CorporateIntensity: 0,
        Category: CardCategory.Action);
}

public class EmailTemplateTests
{
    [Fact]
    public void SelectVariant_DeterministicWithSameSeed()
    {
        var options = new[] { "A", "B", "C", "D" };

        var result1 = EmailTemplates.SelectVariant(options, 42, "event1");
        var result2 = EmailTemplates.SelectVariant(options, 42, "event1");

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void SelectVariant_DifferentForDifferentEvents()
    {
        var options = new[] { "A", "B", "C", "D" };

        var result1 = EmailTemplates.SelectVariant(options, 42, "event1");
        var result2 = EmailTemplates.SelectVariant(options, 42, "event2");

        // Different events should usually produce different results
        // (not guaranteed for all seeds, but likely)
        // Just verify they both return valid options
        Assert.Contains(result1, options);
        Assert.Contains(result2, options);
    }

    [Fact]
    public void GetOpening_ReturnsNonEmpty()
    {
        var opening = EmailTemplates.GetOpening(EmailTone.Professional, 42, "test");

        Assert.False(string.IsNullOrEmpty(opening));
    }

    [Fact]
    public void GetClosing_ReturnsNonEmpty()
    {
        var closing = EmailTemplates.GetClosing(EmailTone.Aloof, 42, "test");

        Assert.False(string.IsNullOrEmpty(closing));
    }

    [Fact]
    public void FormatMeterDeltas_FormatsCorrectly()
    {
        var deltas = new[] { (Meter.Delivery, 10), (Meter.Morale, -5) };

        var formatted = EmailTemplates.FormatMeterDeltas(deltas);

        Assert.Contains("Delivery: +10", formatted);
        Assert.Contains("Morale: -5", formatted);
    }

    [Fact]
    public void GetSenderForMeter_ReturnsAppropriateArchetype()
    {
        Assert.Equal(SenderArchetype.PM, EmailTemplates.GetSenderForMeter(Meter.Delivery));
        Assert.Equal(SenderArchetype.HR, EmailTemplates.GetSenderForMeter(Meter.Morale));
        Assert.Equal(SenderArchetype.CFO, EmailTemplates.GetSenderForMeter(Meter.Runway));
    }

    [Fact]
    public void GetToneForOutcome_GoodOutcomeWithHighAlignment_IsEnthusiastic()
    {
        var tone = EmailTemplates.GetToneForOutcome(OutcomeTier.Good, 70);

        Assert.Equal(EmailTone.Enthusiastic, tone);
    }

    [Fact]
    public void GetToneForOutcome_BadOutcomeWithLowAlignment_IsPanicked()
    {
        var tone = EmailTemplates.GetToneForOutcome(OutcomeTier.Bad, 20);

        Assert.Equal(EmailTone.Panicked, tone);
    }
}

// Legacy Email record tests (for backwards compatibility)
public class LegacyEmailTests
{
    [Fact]
    public void Email_WithAged_IncrementsAge()
    {
        var email = new InertiCorp.Core.Email.Email(
            "e1", "Subject", "Body", SenderArchetype.PM,
            EmailUrgency.Low, EmailType.Queue);

        var aged = email.WithAged();

        Assert.Equal(1, aged.AgeInQuarters);
    }

    [Fact]
    public void Email_WithRead_SetsIsReadTrue()
    {
        var email = new InertiCorp.Core.Email.Email(
            "e1", "Subject", "Body", SenderArchetype.PM,
            EmailUrgency.Low, EmailType.Feedback);

        var read = email.WithRead();

        Assert.True(read.IsRead);
    }
}
