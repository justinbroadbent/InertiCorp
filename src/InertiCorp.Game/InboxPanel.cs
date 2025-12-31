using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Email;

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

        // Header
        _headerLabel = new Label
        {
            Text = "INBOX (0)",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 14);
        _headerLabel.Modulate = new Color(0.8f, 0.8f, 0.8f);
        mainVbox.AddChild(_headerLabel);

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

    private void UpdateDisplay()
    {
        if (_gameManager?.CurrentState is null || _threadsContainer is null) return;

        ClearThreads();

        var inbox = _gameManager.CurrentState.Inbox;
        var unreadCount = inbox.UnreadThreadCount;

        // Update header
        if (_headerLabel is not null)
        {
            _headerLabel.Text = unreadCount > 0
                ? $"INBOX ({unreadCount} unread)"
                : $"INBOX ({inbox.ThreadCount})";
        }

        // Show top threads - high priority unread pop to top, then normal ordering
        var threads = inbox.TopThreads
            .OrderByDescending(t => t.IsHighPriority && !t.IsFullyRead)  // High priority unread first (response required)
            .ThenByDescending(t => !t.IsFullyRead)  // Then other unread
            .ThenByDescending(t => t.SequenceNumber)  // Then by most recent activity
            .ToList();

        if (threads.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No messages",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 11);
            emptyLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
            _threadsContainer.AddChild(emptyLabel);
            return;
        }

        foreach (var thread in threads)
        {
            var threadRow = CreateThreadRow(thread);
            _threadsContainer.AddChild(threadRow);
        }
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

        var thread = _gameManager.CurrentState.Inbox.GetThread(threadId);
        if (thread is null) return;

        _selectedThread = thread;
        ShowThreadDetail(thread);

        // Mark thread as read
        _gameManager.MarkThreadRead(threadId);
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

        // Header with close button
        var headerRow = new HBoxContainer();
        _threadDetailContainer.AddChild(headerRow);

        var subjectLabel = new Label
        {
            Text = thread.Subject,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        subjectLabel.AddThemeFontSizeOverride("font_size", 12);
        headerRow.AddChild(subjectLabel);

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

        // From/To header
        var fromLabel = new Label
        {
            Text = $"From: {GetSenderName(message.From)} -> {GetSenderName(message.To)}"
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

        // Message body
        var bodyLabel = new Label
        {
            Text = message.Body,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        bodyLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(bodyLabel);

        return panel;
    }

    private void ClearThreads()
    {
        if (_threadsContainer is null) return;
        foreach (var child in _threadsContainer.GetChildren())
        {
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
}
