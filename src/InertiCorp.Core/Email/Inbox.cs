namespace InertiCorp.Core.Email;

/// <summary>
/// The CEO's email inbox containing threaded conversations.
/// Each thread represents an Ask (from CEO) + Reply(s) (from NPCs).
/// Threads serve as the canonical event log for the game.
/// </summary>
public sealed record Inbox(
    IReadOnlyList<EmailThread> Threads,
    long NextSequenceNumber = 1,
    IReadOnlyList<EmailThread>? Trash = null)
{
    public const int MaxDisplayCount = 5;

    /// <summary>
    /// Trashed threads (recycle bin).
    /// </summary>
    public IReadOnlyList<EmailThread> TrashThreads => Trash ?? Array.Empty<EmailThread>();

    /// <summary>
    /// Number of threads in trash.
    /// </summary>
    public int TrashCount => TrashThreads.Count;

    /// <summary>
    /// Creates an empty inbox.
    /// </summary>
    public static Inbox Empty => new(Array.Empty<EmailThread>(), 1, Array.Empty<EmailThread>());

    /// <summary>
    /// Visible threads only (excludes hidden project threads awaiting AI content).
    /// </summary>
    private IEnumerable<EmailThread> VisibleThreads => Threads.Where(t => t.IsVisible);

    /// <summary>
    /// Total number of visible threads.
    /// </summary>
    public int ThreadCount => VisibleThreads.Count();

    /// <summary>
    /// Total number of messages across all visible threads.
    /// </summary>
    public int MessageCount => VisibleThreads.Sum(t => t.Messages.Count);

    /// <summary>
    /// Number of visible threads with unread messages.
    /// </summary>
    public int UnreadThreadCount => VisibleThreads.Count(t => !t.IsFullyRead);

    /// <summary>
    /// All messages across all visible threads, most recent first.
    /// </summary>
    public IReadOnlyList<EmailMessage> AllMessages => VisibleThreads
        .SelectMany(t => t.Messages)
        .OrderByDescending(m => m.TurnNumber)
        .ToList();

    /// <summary>
    /// Visible threads with unread messages, most recent first.
    /// </summary>
    public IReadOnlyList<EmailThread> UnreadThreads => VisibleThreads
        .Where(t => !t.IsFullyRead)
        .OrderByDescending(t => t.LatestMessage?.TurnNumber ?? t.CreatedOnTurn)
        .ToList();

    /// <summary>
    /// Top visible threads for display (most recent arrival first by sequence number).
    /// </summary>
    public IReadOnlyList<EmailThread> TopThreads => VisibleThreads
        .OrderByDescending(t => t.SequenceNumber)
        .Take(MaxDisplayCount)
        .ToList();

    /// <summary>
    /// All visible threads ordered by most recent first (no limit).
    /// </summary>
    public IReadOnlyList<EmailThread> AllThreadsOrdered => VisibleThreads
        .OrderByDescending(t => t.SequenceNumber)
        .ToList();

    /// <summary>
    /// Threads created on a specific turn (quarter).
    /// </summary>
    public IReadOnlyList<EmailThread> ThreadsFromTurn(int turnNumber) => Threads
        .Where(t => t.CreatedOnTurn == turnNumber)
        .ToList();

    /// <summary>
    /// Gets a thread by ID.
    /// </summary>
    public EmailThread? GetThread(string threadId) =>
        Threads.FirstOrDefault(t => t.ThreadId == threadId);

    /// <summary>
    /// Gets a thread by originating card ID.
    /// </summary>
    public EmailThread? GetThreadForCard(string cardId) =>
        Threads.FirstOrDefault(t => t.OriginatingCardId == cardId);

    /// <summary>
    /// Returns a new inbox with a thread added.
    /// Thread is assigned the next sequence number for proper ordering.
    /// </summary>
    public Inbox WithThreadAdded(EmailThread thread)
    {
        var threadWithSeq = thread with { SequenceNumber = NextSequenceNumber };
        return new Inbox(Threads.Append(threadWithSeq).ToList(), NextSequenceNumber + 1, Trash);
    }

    /// <summary>
    /// Returns a new inbox with a follow-up message added to an existing thread.
    /// Bumps the thread's sequence number so it appears at the top.
    /// </summary>
    public Inbox WithFollowUpAdded(string threadId, EmailMessage followUp)
    {
        // Bump sequence number so thread moves to top of inbox
        return new Inbox(Threads.Select(t =>
            t.ThreadId == threadId
                ? t.WithFollowUp(followUp) with { SequenceNumber = NextSequenceNumber }
                : t).ToList(), NextSequenceNumber + 1, Trash);
    }

    /// <summary>
    /// Returns a new inbox with all messages in a thread marked as read.
    /// </summary>
    public Inbox WithThreadRead(string threadId)
    {
        return new Inbox(Threads.Select(t =>
            t.ThreadId == threadId
                ? t with { Messages = t.Messages.Select(m => m.WithRead()).ToList() }
                : t).ToList(), NextSequenceNumber, Trash);
    }

    /// <summary>
    /// Returns a new inbox with a thread replaced (preserves its sequence number).
    /// </summary>
    public Inbox WithThreadReplaced(string threadId, EmailThread replacement)
    {
        // Preserve the original sequence number when replacing
        var originalSeq = Threads.FirstOrDefault(t => t.ThreadId == threadId)?.SequenceNumber ?? replacement.SequenceNumber;
        var replacementWithSeq = replacement with { SequenceNumber = originalSeq };
        return new Inbox(Threads.Select(t =>
            t.ThreadId == threadId ? replacementWithSeq : t).ToList(), NextSequenceNumber, Trash);
    }

    /// <summary>
    /// Returns a new inbox with a thread moved to trash.
    /// </summary>
    public Inbox WithThreadTrashed(string threadId)
    {
        var thread = Threads.FirstOrDefault(t => t.ThreadId == threadId);
        if (thread == null) return this;

        return new Inbox(
            Threads.Where(t => t.ThreadId != threadId).ToList(),
            NextSequenceNumber,
            TrashThreads.Append(thread).ToList());
    }

    /// <summary>
    /// Returns a new inbox with a thread restored from trash.
    /// </summary>
    public Inbox WithThreadRestored(string threadId)
    {
        var thread = TrashThreads.FirstOrDefault(t => t.ThreadId == threadId);
        if (thread == null) return this;

        // Restore with new sequence number so it appears at top
        var restoredThread = thread with { SequenceNumber = NextSequenceNumber };
        return new Inbox(
            Threads.Append(restoredThread).ToList(),
            NextSequenceNumber + 1,
            TrashThreads.Where(t => t.ThreadId != threadId).ToList());
    }

    /// <summary>
    /// Returns a new inbox with trash emptied.
    /// </summary>
    public Inbox WithTrashEmptied()
    {
        return new Inbox(Threads, NextSequenceNumber, Array.Empty<EmailThread>());
    }

    /// <summary>
    /// Upgrades a thread to Crisis type (when a project triggers a situation).
    /// This makes the thread high-priority and ties it to the crisis event.
    /// </summary>
    public Inbox WithThreadUpgradedToCrisis(string threadId, string crisisEventId)
    {
        return new Inbox(Threads.Select(t =>
            t.ThreadId == threadId
                ? t with { ThreadType = EmailThreadType.Crisis, OriginatingCardId = crisisEventId }
                : t).ToList(), NextSequenceNumber, Trash);
    }

    /// <summary>
    /// Returns a new inbox with a specific message marked as read.
    /// </summary>
    public Inbox WithMessageRead(string messageId)
    {
        return new Inbox(Threads.Select(t =>
            t with { Messages = t.Messages.Select(m =>
                m.MessageId == messageId ? m.WithRead() : m).ToList() }).ToList(), NextSequenceNumber, Trash);
    }

    // === Legacy compatibility layer ===

    /// <summary>
    /// Legacy: flat list of emails for backwards compatibility.
    /// </summary>
    public IReadOnlyList<Email> LegacyEmails { get; init; } = Array.Empty<Email>();

    /// <summary>
    /// Returns a new inbox with a legacy email added.
    /// </summary>
    public Inbox WithLegacyEmailAdded(Email email)
    {
        return this with { LegacyEmails = LegacyEmails.Append(email).ToList() };
    }

    /// <summary>
    /// Returns a new inbox with legacy emails aged.
    /// </summary>
    public Inbox WithLegacyEmailsAged()
    {
        return this with { LegacyEmails = LegacyEmails.Select(e =>
            e.Type == EmailType.Queue ? e.WithAged() : e).ToList() };
    }
}
