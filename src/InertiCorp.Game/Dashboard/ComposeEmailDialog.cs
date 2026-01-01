using Godot;
using System;
using System.Collections.Generic;
using InertiCorp.Core.Email;

namespace InertiCorp.Game.Dashboard;

/// <summary>
/// Dialog for composing freeform emails to get humorous corporate responses.
/// </summary>
public partial class ComposeEmailDialog : Control
{
    private const int MaxBodyLength = 200;

    private static readonly string[] SubjectOptions = new[]
    {
        "Quick Question",
        "Status Update Request",
        "Thoughts on This?",
        "Synergy Opportunity",
        "Urgent: Need Input",
        "Following Up",
        "Strategic Alignment Check",
        "Resource Request"
    };

    private static readonly string[] RecipientOptions = new[]
    {
        "All Staff",
        "Product Team",
        "Engineering",
        "Legal",
        "HR",
        "Finance",
        "Marketing",
        "The Board"
    };

    [Signal]
    public delegate void EmailSentEventHandler(string subject, string body, string recipient);

    [Signal]
    public delegate void DialogClosedEventHandler();

    private OptionButton? _subjectDropdown;
    private OptionButton? _recipientDropdown;
    private TextEdit? _bodyEdit;
    private Label? _charCountLabel;
    private Button? _sendButton;
    private Button? _cancelButton;
    private PanelContainer? _dialogPanel;

    public override void _Ready()
    {
        // Make this control fill the entire screen
        SetAnchorsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        // Create dark overlay
        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f)
        };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        // Create centered dialog panel
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        _dialogPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(500, 400)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f),
            BorderWidthTop = 2,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderColor = new Color(0.3f, 0.5f, 0.7f),
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        _dialogPanel.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(_dialogPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        _dialogPanel.AddChild(vbox);

        // Header
        var headerHbox = new HBoxContainer();
        vbox.AddChild(headerHbox);

        var titleLabel = new Label { Text = "Compose Email", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        titleLabel.Modulate = new Color(0.4f, 0.7f, 0.9f);
        headerHbox.AddChild(titleLabel);

        _cancelButton = new Button { Text = "X", CustomMinimumSize = new Vector2(30, 30) };
        _cancelButton.Pressed += OnCancelPressed;
        headerHbox.AddChild(_cancelButton);

        // Recipient dropdown
        var recipientHbox = new HBoxContainer();
        vbox.AddChild(recipientHbox);

        var toLabel = new Label { Text = "To:", CustomMinimumSize = new Vector2(60, 0) };
        toLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        recipientHbox.AddChild(toLabel);

        _recipientDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var recipient in RecipientOptions)
        {
            _recipientDropdown.AddItem(recipient);
        }
        _recipientDropdown.Selected = 0;
        recipientHbox.AddChild(_recipientDropdown);

        // Subject dropdown
        var subjectHbox = new HBoxContainer();
        vbox.AddChild(subjectHbox);

        var subjectLabel = new Label { Text = "Subject:", CustomMinimumSize = new Vector2(60, 0) };
        subjectLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        subjectHbox.AddChild(subjectLabel);

        _subjectDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var subject in SubjectOptions)
        {
            _subjectDropdown.AddItem(subject);
        }
        _subjectDropdown.Selected = 0;
        subjectHbox.AddChild(_subjectDropdown);

        // Body label
        var bodyLabel = new Label { Text = "Message:" };
        bodyLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        vbox.AddChild(bodyLabel);

        // Body text edit
        _bodyEdit = new TextEdit
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            PlaceholderText = "Type your message here... (keep it corporate!)",
            WrapMode = TextEdit.LineWrappingMode.Boundary
        };
        _bodyEdit.AddThemeFontSizeOverride("font_size", 13);
        _bodyEdit.TextChanged += OnBodyTextChanged;
        vbox.AddChild(_bodyEdit);

        // Character count
        _charCountLabel = new Label
        {
            Text = $"0/{MaxBodyLength}",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _charCountLabel.AddThemeFontSizeOverride("font_size", 11);
        _charCountLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
        vbox.AddChild(_charCountLabel);

        // Buttons
        var buttonHbox = new HBoxContainer();
        buttonHbox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(buttonHbox);

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buttonHbox.AddChild(spacer);

        var discardBtn = new Button { Text = "Discard" };
        discardBtn.Pressed += OnCancelPressed;
        buttonHbox.AddChild(discardBtn);

        _sendButton = new Button { Text = "Send Email" };
        _sendButton.Disabled = true;
        var sendStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.5f, 0.3f),
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _sendButton.AddThemeStyleboxOverride("normal", sendStyle);
        _sendButton.Pressed += OnSendPressed;
        buttonHbox.AddChild(_sendButton);

        // Disclaimer
        var disclaimer = new Label
        {
            Text = "Your message will be processed by InertiCorp AI Analytics",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        disclaimer.AddThemeFontSizeOverride("font_size", 10);
        disclaimer.Modulate = new Color(0.4f, 0.4f, 0.45f);
        vbox.AddChild(disclaimer);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            OnCancelPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnBodyTextChanged()
    {
        if (_bodyEdit is null || _charCountLabel is null || _sendButton is null) return;

        var text = _bodyEdit.Text;
        var length = text.Length;

        // Enforce max length
        if (length > MaxBodyLength)
        {
            _bodyEdit.Text = text[..MaxBodyLength];
            length = MaxBodyLength;
            _bodyEdit.SetCaretColumn(MaxBodyLength);
        }

        // Update character count
        _charCountLabel.Text = $"{length}/{MaxBodyLength}";
        _charCountLabel.Modulate = length > MaxBodyLength * 0.9f
            ? new Color(0.9f, 0.5f, 0.4f)
            : new Color(0.5f, 0.5f, 0.55f);

        // Enable send if we have content
        _sendButton.Disabled = length < 5;
    }

    private void OnSendPressed()
    {
        if (_subjectDropdown is null || _recipientDropdown is null || _bodyEdit is null) return;

        var subject = SubjectOptions[_subjectDropdown.Selected];
        var recipient = RecipientOptions[_recipientDropdown.Selected];
        var body = _bodyEdit.Text.Trim();

        if (string.IsNullOrWhiteSpace(body)) return;

        GD.Print($"[ComposeEmail] Sending: {subject} to {recipient}");
        EmitSignal(SignalName.EmailSent, subject, body, recipient);
        QueueFree();
    }

    private void OnCancelPressed()
    {
        EmitSignal(SignalName.DialogClosed);
        QueueFree();
    }
}
