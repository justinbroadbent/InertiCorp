using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying the end game screen when CEO retires or is ousted.
/// </summary>
public partial class EndScreen : PanelContainer
{
    private GameManager? _gameManager;

    private Label? _titleLabel;
    private Label? _reasonLabel;
    private VBoxContainer? _statsContainer;
    private Button? _playAgainButton;
    private Button? _closeButton;
    private HBoxContainer? _buttonContainer;
    private bool _wasDismissed = false;

    /// <summary>
    /// Whether the end screen popup was dismissed (user closed it to read emails).
    /// </summary>
    public bool WasDismissed => _wasDismissed;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        _titleLabel = GetNode<Label>("VBox/Title");
        _reasonLabel = GetNode<Label>("VBox/Reason");
        _statsContainer = GetNode<VBoxContainer>("VBox/Objectives");

        // Replace the single button with a button container
        var oldButton = GetNode<Button>("VBox/PlayAgainButton");
        var vbox = GetNode<VBoxContainer>("VBox");
        var buttonIndex = oldButton.GetIndex();
        oldButton.QueueFree();

        _buttonContainer = new HBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_buttonContainer);
        vbox.MoveChild(_buttonContainer, buttonIndex);

        // Close button (to read emails)
        _closeButton = new Button { Text = "Close (Read Emails)" };
        _closeButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _closeButton.Pressed += OnClosePressed;
        _buttonContainer.AddChild(_closeButton);

        // New Game button
        _playAgainButton = new Button { Text = "New Game" };
        _playAgainButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _playAgainButton.Pressed += OnPlayAgainPressed;
        _buttonContainer.AddChild(_playAgainButton);

        // Set opaque dark background for readability
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 1.0f), // Fully opaque dark blue-gray
            BorderColor = new Color(0.6f, 0.3f, 0.3f, 1.0f), // Subtle red border
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        _gameManager.GameEnded += OnGameEnded;
        _gameManager.PhaseChanged += OnPhaseChanged;

        UpdateVisibility();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.GameEnded -= OnGameEnded;
            _gameManager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnGameEnded(bool isWon)
    {
        _wasDismissed = false; // Reset dismissal when game ends
        UpdateVisibility();
        UpdateDisplay();
    }

    private void OnPhaseChanged()
    {
        UpdateVisibility();
    }

    private void OnClosePressed()
    {
        _wasDismissed = true;
        Visible = false;
    }

    /// <summary>
    /// Shows the end screen popup again (if it was dismissed).
    /// </summary>
    public void ShowPopup()
    {
        if (_gameManager?.Phase == UIPhase.GameOver)
        {
            _wasDismissed = false;
            Visible = true;
        }
    }

    private void UpdateVisibility()
    {
        // Only auto-show if game is over and popup wasn't dismissed
        Visible = _gameManager?.Phase == UIPhase.GameOver && !_wasDismissed;
    }

    private void UpdateDisplay()
    {
        if (_gameManager?.CurrentState is null) return;

        var state = _gameManager.CurrentState;
        var ceo = state.CEO;

        // Set title and reason based on retirement vs ouster
        if (_titleLabel is not null)
        {
            if (ceo.HasRetired)
            {
                _titleLabel.Text = "GRACEFUL EXIT";
                _titleLabel.Modulate = new Color(0.4f, 1f, 0.5f); // Green for victory
            }
            else
            {
                _titleLabel.Text = "YOU'VE BEEN OUSTED";
                _titleLabel.Modulate = new Color(1f, 0.5f, 0.5f); // Red for loss
            }
        }

        if (_reasonLabel is not null)
        {
            if (ceo.HasRetired)
            {
                _reasonLabel.Text = $"After {ceo.QuartersSurvived} quarters, you've retired with your reputation\n(mostly) intact and your golden parachute secured.";
            }
            else if (ceo.BoardFavorability < 20)
            {
                _reasonLabel.Text = "The board lost all confidence in your leadership.";
            }
            else if (ceo.BoardFavorability < 40)
            {
                _reasonLabel.Text = "The board voted against you in a close decision.";
            }
            else
            {
                _reasonLabel.Text = "An unexpected board coup ended your tenure.";
            }
        }

        // Update panel border color based on outcome
        var borderColor = ceo.HasRetired
            ? new Color(0.3f, 0.6f, 0.3f, 1.0f) // Green border for victory
            : new Color(0.6f, 0.3f, 0.3f, 1.0f); // Red border for loss
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f, 1.0f),
            BorderColor = borderColor,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        AddThemeStyleboxOverride("panel", bgStyle);

        // Display final score breakdown
        UpdateStats(ceo, state.Resources);
    }

    private void UpdateStats(CEOState ceo, ResourceState resources)
    {
        ClearStats();

        if (_statsContainer is null) return;

        // Calculate score breakdown
        var breakdown = ScoreCalculator.GetScoreBreakdown(ceo, resources);

        // Final score - prominent display
        var scoreColor = ceo.HasRetired ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.85f, 0.3f);
        AddStatLabel("FINAL SCORE", breakdown.FinalScore.ToString(), scoreColor, fontSize: 24);

        // Divider
        AddStatLabel("", "═══════════════════════", new Color(0.5f, 0.5f, 0.5f));

        // Score breakdown - simplified formula
        AddStatLabel("Accumulated Bonus", $"${breakdown.AccumulatedBonus}M", new Color(0.8f, 0.8f, 0.8f));
        AddStatLabel("Golden Parachute", $"${breakdown.GoldenParachute}M", new Color(0.8f, 0.8f, 0.8f));
        AddStatLabel("PC Conversion", $"{breakdown.PoliticalCapital} × $5M = ${breakdown.PCConversion}M", new Color(0.8f, 0.8f, 0.8f));
        AddStatLabel("Projects Bonus", $"{breakdown.TotalProjects} × $1M = ${breakdown.ProjectsBonus}M", new Color(0.8f, 0.6f, 0.9f));

        // Subtotal
        AddStatLabel("", "───────────────────────", new Color(0.5f, 0.5f, 0.5f));
        AddStatLabel("Subtotal", $"${breakdown.Subtotal}M", new Color(0.8f, 0.8f, 0.8f));

        // Multiplier - color coded
        var multColor = ceo.HasRetired ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
        AddStatLabel($"{breakdown.MultiplierReason}", $"×{breakdown.Multiplier:F1}", multColor);

        // Parachute breakdown hint
        if (ceo.TotalCardsPlayed == 0)
        {
            AddStatLabel("", "(Minimal parachute - no initiatives)", new Color(0.6f, 0.4f, 0.4f));
        }
    }

    private void AddStatLabel(string label, string value, Color color, int fontSize = 0)
    {
        if (_statsContainer is null) return;

        var hbox = new HBoxContainer();

        if (!string.IsNullOrEmpty(label))
        {
            var labelNode = new Label
            {
                Text = label,
                HorizontalAlignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            if (fontSize > 0) labelNode.AddThemeFontSizeOverride("font_size", fontSize);
            hbox.AddChild(labelNode);

            var valueNode = new Label
            {
                Text = value,
                HorizontalAlignment = HorizontalAlignment.Right,
                Modulate = color
            };
            if (fontSize > 0) valueNode.AddThemeFontSizeOverride("font_size", fontSize);
            hbox.AddChild(valueNode);
        }
        else
        {
            // Just a centered label (for dividers)
            var centerLabel = new Label
            {
                Text = value,
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Modulate = color
            };
            hbox.AddChild(centerLabel);
        }

        _statsContainer.AddChild(hbox);
    }

    private void ClearStats()
    {
        if (_statsContainer is null) return;

        foreach (var child in _statsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnPlayAgainPressed()
    {
        _gameManager?.StartNewGame();
    }
}
