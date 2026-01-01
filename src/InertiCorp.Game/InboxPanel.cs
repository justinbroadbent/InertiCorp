using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Content;
using InertiCorp.Core.Email;
using InertiCorp.Game.Dashboard;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying the CEO's email inbox with threaded conversations.
/// Shows most recent threads first, with unread threads highlighted.
/// </summary>
public partial class InboxPanel : PanelContainer
{
    private GameManager? _gameManager;
    private VBoxContainer? _threadsContainer;
    private Label? _headerLabel;
    private EmailThread? _selectedThread;
    private PanelContainer? _threadDetailPanel;
    private VBoxContainer? _threadDetailContainer;
    private Button? _trashToggleButton;
    private Button? _composeButton;
    private bool _viewingTrash;

    private static readonly Dictionary<EmailTone, Color> ToneColors = new()
    {
        { EmailTone.Professional, new Color(0.7f, 0.7f, 0.7f) },
        { EmailTone.Aloof, new Color(0.6f, 0.6f, 0.7f) },
        { EmailTone.Panicked, new Color(0.9f, 0.4f, 0.4f) },
        { EmailTone.Obsequious, new Color(0.5f, 0.8f, 0.5f) },
        { EmailTone.Passive, new Color(0.7f, 0.7f, 0.5f) },
        { EmailTone.Enthusiastic, new Color(0.4f, 0.9f, 0.4f) },
        { EmailTone.Cryptic, new Color(0.6f, 0.5f, 0.8f) },
        { EmailTone.Blunt, new Color(0.9f, 0.6f, 0.3f) }
    };

    private static readonly Dictionary<SenderArchetype, string> SenderNames = new()
    {
        { SenderArchetype.CEO, "You" },
        { SenderArchetype.CFO, "CFO" },
        { SenderArchetype.HR, "HR Director" },
        { SenderArchetype.PM, "Product Manager" },
        { SenderArchetype.EngManager, "Engineering Director" },
        { SenderArchetype.Legal, "Legal Counsel" },
        { SenderArchetype.BoardMember, "Board Member" },
        { SenderArchetype.TechLead, "Tech Lead" },
        { SenderArchetype.Compliance, "Compliance Officer" }
    };

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style the main panel
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f, 0.95f),
            BorderColor = new Color(0.25f, 0.25f, 0.3f),
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        styleBox.BorderWidthTop = 1;
        styleBox.BorderWidthBottom = 1;
        styleBox.BorderWidthLeft = 1;
        styleBox.BorderWidthRight = 1;
        styleBox.CornerRadiusTopLeft = 5;
        styleBox.CornerRadiusTopRight = 5;
        styleBox.CornerRadiusBottomLeft = 5;
        styleBox.CornerRadiusBottomRight = 5;
        AddThemeStyleboxOverride("panel", styleBox);

        // Main layout: split between thread list and thread detail
        var mainVbox = new VBoxContainer();
        mainVbox.AddThemeConstantOverride("separation", 8);
        AddChild(mainVbox);

        // Header row with compose button, inbox label, and trash toggle
        var headerRow = new HBoxContainer();
        mainVbox.AddChild(headerRow);

        _composeButton = new Button
        {
            Text = "Compose",
            CustomMinimumSize = new Vector2(70, 24),
            TooltipText = "Write a freeform email"
        };
        _composeButton.AddThemeFontSizeOverride("font_size", 11);
        _composeButton.Pressed += OnComposePressed;
        headerRow.AddChild(_composeButton);

        _headerLabel = new Label
        {
            Text = "INBOX (0)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        _headerLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
        headerRow.AddChild(_headerLabel);

        _trashToggleButton = new Button
        {
            Text = "🗑",
            CustomMinimumSize = new Vector2(28, 22),
            TooltipText = "View Trash"
        };
        _trashToggleButton.AddThemeFontSizeOverride("font_size", 12);
        _trashToggleButton.Pressed += ToggleTrashView;
        headerRow.AddChild(_trashToggleButton);

        mainVbox.AddChild(new HSeparator());

        // Scrollable thread list
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        mainVbox.AddChild(scroll);

        _threadsContainer = new VBoxContainer();
        _threadsContainer.AddThemeConstantOverride("separation", 4);
        _threadsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_threadsContainer);

        // Thread detail panel (initially hidden)
        _threadDetailPanel = CreateThreadDetailPanel();
        _threadDetailPanel.Visible = false;
        mainVbox.AddChild(_threadDetailPanel);

        // Connect to state changes
        _gameManager.StateChanged += OnStateChanged;

        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged() => UpdateDisplay();

    private void ToggleTrashView()
    {
        _viewingTrash = !_viewingTrash;
        HideThreadDetail();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_gameManager?.CurrentState is null || _threadsContainer is null) return;

        ClearThreads();

        var inbox = _gameManager.CurrentState.Inbox;

        // Update header and button based on view mode
        if (_headerLabel is not null && _trashToggleButton is not null)
        {
            if (_viewingTrash)
            {
                _headerLabel.Text = $"TRASH ({inbox.TrashCount})";
                _headerLabel.Modulate = new Color(0.6f, 0.5f, 0.5f);
                _trashToggleButton.Text = "📥";
                _trashToggleButton.TooltipText = "Back to Inbox";
            }
            else
            {
                var unreadCount = inbox.UnreadThreadCount;
                _headerLabel.Text = unreadCount > 0
                    ? $"INBOX ({unreadCount} unread)"
                    : $"INBOX ({inbox.ThreadCount})";
                _headerLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
                _trashToggleButton.Text = inbox.TrashCount > 0 ? $"🗑({inbox.TrashCount})" : "🗑";
                _trashToggleButton.TooltipText = "View Trash";
            }
        }

        // Get threads based on view mode
        IReadOnlyList<EmailThread> threads;
        if (_viewingTrash)
        {
            threads = inbox.TrashThreads
                .OrderByDescending(t => t.SequenceNumber)
                .Take(Inbox.MaxDisplayCount)
                .ToList();
        }
        else
        {
            threads = inbox.TopThreads
                .OrderByDescending(t => t.IsHighPriority && !t.IsFullyRead)
                .ThenByDescending(t => !t.IsFullyRead)
                .ThenByDescending(t => t.SequenceNumber)
                .ToList();
        }

        if (threads.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = _viewingTrash ? "Trash is empty" : "No messages",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 11);
            emptyLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
            _threadsContainer.AddChild(emptyLabel);

            // Add "Empty Trash" button if in trash view (even when empty, for consistency)
            if (_viewingTrash && inbox.TrashCount > 0)
            {
                AddEmptyTrashButton();
            }
            return;
        }

        foreach (var thread in threads)
        {
            var threadRow = CreateThreadRow(thread);
            _threadsContainer.AddChild(threadRow);
        }

        // Add "Empty Trash" button at bottom when viewing trash
        if (_viewingTrash && inbox.TrashCount > 0)
        {
            AddEmptyTrashButton();
        }
    }

    private void AddEmptyTrashButton()
    {
        if (_threadsContainer is null) return;

        var emptyButton = new Button
        {
            Text = "Empty Trash",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(100, 25)
        };
        emptyButton.AddThemeFontSizeOverride("font_size", 10);
        emptyButton.Pressed += () =>
        {
            _gameManager?.EmptyTrash();
            UpdateDisplay();
        };
        _threadsContainer.AddChild(emptyButton);
    }

    private Control CreateThreadRow(EmailThread thread)
    {
        // Read emails get compact single-line treatment
        // Unread emails get full prominent display
        var isRead = thread.IsFullyRead;

        var button = new Button
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, isRead ? 24 : 50)
        };

        // Style the button - read emails are much more subtle
        var normalStyle = new StyleBoxFlat
        {
            BgColor = isRead
                ? new Color(0.09f, 0.09f, 0.1f)  // Very dark, almost invisible
                : new Color(0.18f, 0.18f, 0.25f),  // More prominent blue tint
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = isRead ? 2 : 4,
            ContentMarginBottom = isRead ? 2 : 4
        };
        normalStyle.CornerRadiusTopLeft = 3;
        normalStyle.CornerRadiusTopRight = 3;
        normalStyle.CornerRadiusBottomLeft = 3;
        normalStyle.CornerRadiusBottomRight = 3;

        // Unread emails get a left border highlight
        if (!isRead)
        {
            normalStyle.BorderColor = new Color(0.3f, 0.6f, 0.9f);
            normalStyle.BorderWidthLeft = 3;
        }

        button.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.25f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = isRead ? 2 : 4,
            ContentMarginBottom = isRead ? 2 : 4
        };
        hoverStyle.CornerRadiusTopLeft = 3;
        hoverStyle.CornerRadiusTopRight = 3;
        hoverStyle.CornerRadiusBottomLeft = 3;
        hoverStyle.CornerRadiusBottomRight = 3;
        button.AddThemeStyleboxOverride("hover", hoverStyle);

        if (isRead)
        {
            // Compact single-line layout for read emails
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 6);
            button.AddChild(hbox);

            var subjectLabel = new Label
            {
                Text = TruncateText(thread.Subject, 20),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            subjectLabel.AddThemeFontSizeOverride("font_size", 9);
            subjectLabel.Modulate = new Color(0.45f, 0.45f, 0.5f);  // Dim gray
            hbox.AddChild(subjectLabel);

            var countLabel = new Label
            {
                Text = $"({thread.Messages.Count})"
            };
            countLabel.AddThemeFontSizeOverride("font_size", 9);
            countLabel.Modulate = new Color(0.35f, 0.35f, 0.4f);
            hbox.AddChild(countLabel);
        }
        else
        {
            // Full prominent layout for unread emails
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 2);
            button.AddChild(vbox);

            // Subject line
            var subjectRow = new HBoxContainer();
            vbox.AddChild(subjectRow);

            var unreadDot = new Label
            {
                Text = "\u2022"  // Bullet character
            };
            unreadDot.AddThemeFontSizeOverride("font_size", 14);
            unreadDot.Modulate = new Color(0.3f, 0.7f, 0.9f);
            subjectRow.AddChild(unreadDot);

            var subjectLabel = new Label
            {
                Text = TruncateText(thread.Subject, 25),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            subjectLabel.AddThemeFontSizeOverride("font_size", 11);
            subjectLabel.Modulate = Colors.White;
            subjectRow.AddChild(subjectLabel);

            // Message count
            var countLabel = new Label
            {
                Text = $"({thread.Messages.Count})"
            };
            countLabel.AddThemeFontSizeOverride("font_size", 10);
            countLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
            subjectRow.AddChild(countLabel);

            // Latest message preview
            var latestMsg = thread.LatestMessage;
            if (latestMsg is not null)
            {
                var previewLabel = new Label
                {
                    Text = $"{GetSenderName(latestMsg.From)}: {TruncateText(GetFirstLine(latestMsg.Body), 30)}"
                };
                previewLabel.AddThemeFontSizeOverride("font_size", 9);
                previewLabel.Modulate = ToneColors.GetValueOrDefault(latestMsg.Tone, Colors.Gray);
                vbox.AddChild(previewLabel);
            }
        }

        // Click handler
        var threadId = thread.ThreadId;
        button.Pressed += () => OnThreadClicked(threadId);

        return button;
    }

    private void OnThreadClicked(string threadId)
    {
        if (_gameManager?.CurrentState is null) return;

        var inbox = _gameManager.CurrentState.Inbox;
        var thread = _viewingTrash
            ? inbox.TrashThreads.FirstOrDefault(t => t.ThreadId == threadId)
            : inbox.GetThread(threadId);

        if (thread is null) return;

        _selectedThread = thread;
        ShowThreadDetail(thread);

        // Mark thread as read (only for inbox, not trash)
        if (!_viewingTrash)
        {
            _gameManager.MarkThreadRead(threadId);
        }
    }

    private PanelContainer CreateThreadDetailPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 200)
        };

        var detailStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.12f),
            BorderColor = new Color(0.3f, 0.3f, 0.35f),
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        detailStyle.BorderWidthTop = 1;
        detailStyle.BorderWidthBottom = 1;
        detailStyle.BorderWidthLeft = 1;
        detailStyle.BorderWidthRight = 1;
        panel.AddThemeStyleboxOverride("panel", detailStyle);

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        panel.AddChild(scroll);

        _threadDetailContainer = new VBoxContainer();
        _threadDetailContainer.AddThemeConstantOverride("separation", 10);
        _threadDetailContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_threadDetailContainer);

        return panel;
    }

    private void ShowThreadDetail(EmailThread thread)
    {
        if (_threadDetailPanel is null || _threadDetailContainer is null) return;

        // Clear previous detail
        foreach (var child in _threadDetailContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Header with action buttons
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 4);
        _threadDetailContainer.AddChild(headerRow);

        var subjectLabel = new Label
        {
            Text = thread.Subject,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        subjectLabel.AddThemeFontSizeOverride("font_size", 12);
        headerRow.AddChild(subjectLabel);

        // Delete or Restore button depending on view mode
        var threadId = thread.ThreadId;
        if (_viewingTrash)
        {
            var restoreButton = new Button
            {
                Text = "↩",
                TooltipText = "Restore to Inbox",
                CustomMinimumSize = new Vector2(25, 25)
            };
            restoreButton.Pressed += () =>
            {
                _gameManager?.RestoreThread(threadId);
                HideThreadDetail();
            };
            headerRow.AddChild(restoreButton);
        }
        else
        {
            var deleteButton = new Button
            {
                Text = "🗑",
                TooltipText = "Move to Trash",
                CustomMinimumSize = new Vector2(25, 25)
            };
            deleteButton.Pressed += () =>
            {
                _gameManager?.TrashThread(threadId);
                HideThreadDetail();
            };
            headerRow.AddChild(deleteButton);
        }

        var closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(25, 25)
        };
        closeButton.Pressed += HideThreadDetail;
        headerRow.AddChild(closeButton);

        _threadDetailContainer.AddChild(new HSeparator());

        // Display all messages in the thread
        foreach (var message in thread.Messages)
        {
            var messagePanel = CreateMessagePanel(message);
            _threadDetailContainer.AddChild(messagePanel);
        }

        _threadDetailPanel.Visible = true;
    }

    private void HideThreadDetail()
    {
        if (_threadDetailPanel is null) return;
        _threadDetailPanel.Visible = false;
        _selectedThread = null;
    }

    private Control CreateMessagePanel(EmailMessage message)
    {
        var panel = new PanelContainer();

        var msgStyle = new StyleBoxFlat
        {
            BgColor = message.IsFromPlayer
                ? new Color(0.12f, 0.15f, 0.18f)
                : new Color(0.14f, 0.12f, 0.14f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        msgStyle.CornerRadiusTopLeft = 4;
        msgStyle.CornerRadiusTopRight = 4;
        msgStyle.CornerRadiusBottomLeft = 4;
        msgStyle.CornerRadiusBottomRight = 4;
        panel.AddThemeStyleboxOverride("panel", msgStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        // From/To header - use full name from company directory for non-CEO senders
        var fromDisplay = message.IsFromPlayer
            ? "You (CEO)"
            : CompanyDirectory.GetEmployeeForEvent(message.From, message.MessageId).Name;
        var toDisplay = message.To == SenderArchetype.CEO
            ? "CEO"
            : CompanyDirectory.GetEmployeeForEvent(message.To, message.MessageId).Name;

        var fromLabel = new Label
        {
            Text = $"From: {fromDisplay} -> {toDisplay}"
        };
        fromLabel.AddThemeFontSizeOverride("font_size", 10);
        fromLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
        vbox.AddChild(fromLabel);

        // Tone indicator
        var toneLabel = new Label
        {
            Text = $"[{message.Tone}]"
        };
        toneLabel.AddThemeFontSizeOverride("font_size", 9);
        toneLabel.Modulate = ToneColors.GetValueOrDefault(message.Tone, Colors.Gray);
        vbox.AddChild(toneLabel);

        // Message body with signature block for non-CEO messages
        var bodyText = message.Body;
        if (!message.IsFromPlayer)
        {
            // Append formal signature block
            var signature = CompanyDirectory.GenerateSignature(message.From, message.MessageId, message.Tone);
            bodyText = $"{bodyText}\n\n---\n{signature}";
        }

        var bodyLabel = new Label
        {
            Text = bodyText,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        bodyLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(bodyLabel);

        return panel;
    }

    private void ClearThreads()
    {
        if (_threadsContainer is null) return;

        // Get children into a list first (since we're modifying the collection)
        var children = _threadsContainer.GetChildren().ToList();
        foreach (var child in children)
        {
            // Remove from parent immediately to prevent overlap
            _threadsContainer.RemoveChild(child);
            // Then queue for deletion
            child.QueueFree();
        }
    }

    private static string GetSenderName(SenderArchetype sender) =>
        SenderNames.GetValueOrDefault(sender, sender.ToString());

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }

    private static string GetFirstLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var idx = text.IndexOf('\n');
        return idx >= 0 ? text[..idx] : text;
    }

    private void OnComposePressed()
    {
        // Create and show the compose dialog
        var dialog = new ComposeEmailDialog();
        dialog.EmailSent += OnEmailSent;
        dialog.DialogClosed += () => GD.Print("[InboxPanel] Compose dialog closed");

        // Add to root so it overlays everything
        GetTree().Root.AddChild(dialog);
    }

    private void OnEmailSent(string subject, string body, string recipient)
    {
        GD.Print($"[InboxPanel] Email sent: {subject} to {recipient}");

        // Generate a unique request ID for this freeform email
        var requestId = $"freeform_{DateTime.Now.Ticks}";

        // Queue the AI request with Low priority (doesn't block projects)
        var context = new Dictionary<string, string>
        {
            { "recipient", recipient }
        };

        BackgroundEmailProcessor.QueueExternalAiRequest(
            requestId,
            subject,  // Title = subject
            body,     // Description = CEO's message
            AiPromptType.FreeformEmail,
            AiPriority.Low,
            aiResponse => OnFreeformAiResponse(requestId, subject, body, recipient, aiResponse),
            context);
    }

    private void OnFreeformAiResponse(string requestId, string subject, string ceoMessage, string recipient, string aiResponse)
    {
        if (_gameManager?.CurrentState is null) return;

        GD.Print($"[InboxPanel] Freeform AI response received: {aiResponse[..Math.Min(50, aiResponse.Length)]}...");

        // Create the email thread with CEO's message and AI response
        var threadId = $"freeform_{Guid.NewGuid():N}";
        var turnNumber = _gameManager.CurrentState.Quarter.QuarterNumber;

        // Determine responder archetype based on recipient
        var responderArchetype = recipient switch
        {
            "Product Team" => SenderArchetype.PM,
            "Engineering" => SenderArchetype.EngManager,
            "Legal" => SenderArchetype.Legal,
            "HR" => SenderArchetype.HR,
            "Finance" => SenderArchetype.CFO,
            "Marketing" => SenderArchetype.Marketing,
            "The Board" => SenderArchetype.BoardMember,
            _ => SenderArchetype.EngManager  // Default for "All Staff"
        };

        // CEO's outgoing message
        var ceoMsg = new EmailMessage(
            MessageId: $"{threadId}_ceo",
            ThreadId: threadId,
            Subject: subject,
            Body: ceoMessage,
            From: SenderArchetype.CEO,
            To: responderArchetype,
            Tone: EmailTone.Professional,
            TurnNumber: turnNumber,
            LinkedEventIds: Array.Empty<string>(),
            IsRead: true);

        // Response from the corporate drone
        var responseBody = string.IsNullOrWhiteSpace(aiResponse)
            ? GetFallbackResponse(subject, recipient)
            : aiResponse;

        var responseMsg = new EmailMessage(
            MessageId: $"{threadId}_response",
            ThreadId: threadId,
            Subject: $"Re: {subject}",
            Body: responseBody,
            From: responderArchetype,
            To: SenderArchetype.CEO,
            Tone: EmailTone.Passive,  // Passive-aggressive is on brand
            TurnNumber: turnNumber,
            LinkedEventIds: Array.Empty<string>());

        var thread = new EmailThread(
            ThreadId: threadId,
            Subject: subject,
            OriginatingCardId: null,
            CreatedOnTurn: turnNumber,
            Messages: new[] { ceoMsg, responseMsg },
            ThreadType: EmailThreadType.Fluff);

        // Add to inbox
        var newInbox = _gameManager.CurrentState.Inbox.WithThreadAdded(thread);
        _gameManager.UpdateInbox(newInbox);

        GD.Print($"[InboxPanel] Freeform email thread created: {threadId}");
    }

    private static string GetFallbackResponse(string subject, string recipient)
    {
        // Fallback responses if AI fails
        var responses = new[]
        {
            "Per my last email, I believe we discussed this in Q2. Let me circle back with the team and provide a more comprehensive update by EOD Friday.",
            "Great question! I've looped in the relevant stakeholders and we're currently at 73.2% alignment on this initiative. More details to follow.",
            "Thanks for flagging this. We're seeing positive traction on our key metrics (NPS up 12 points, synergy index at 94.7%). Happy to schedule a deep-dive.",
            "This is definitely on our radar. I've added it to the backlog and we'll prioritize based on our current OKR framework.",
            "Appreciate you raising this. I'll take it offline with the cross-functional team and report back with actionable next steps."
        };
        return responses[Math.Abs(subject.GetHashCode()) % responses.Length];
    }
}
