using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// Shows the current phase and what the player should do.
/// </summary>
public partial class PhaseIndicator : PanelContainer
{
    private GameManager? _gameManager;
    private Label? _phaseLabel;
    private Label? _instructionLabel;
    private Label? _directiveLabel;
    private Button? _continueButton;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style the panel
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = new Color(0.3f, 0.3f, 0.4f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
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

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        _phaseLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _phaseLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(_phaseLabel);

        _instructionLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _instructionLabel.AddThemeFontSizeOverride("font_size", 12);
        _instructionLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        vbox.AddChild(_instructionLabel);

        _directiveLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        _directiveLabel.AddThemeFontSizeOverride("font_size", 11);
        _directiveLabel.Modulate = new Color(0.9f, 0.7f, 0.3f);
        vbox.AddChild(_directiveLabel);

        _continueButton = new Button
        {
            Text = "CONTINUE",
            CustomMinimumSize = new Vector2(120, 0)
        };
        _continueButton.Pressed += OnContinuePressed;
        vbox.AddChild(_continueButton);

        // Center the button
        var buttonContainer = new CenterContainer();
        vbox.RemoveChild(_continueButton);
        buttonContainer.AddChild(_continueButton);
        vbox.AddChild(buttonContainer);

        _gameManager.PhaseChanged += OnPhaseChanged;
        _gameManager.StateChanged += OnStateChanged;

        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.PhaseChanged -= OnPhaseChanged;
            _gameManager.StateChanged -= OnStateChanged;
        }
    }

    private void OnPhaseChanged() => UpdateDisplay();
    private void OnStateChanged() => UpdateDisplay();

    private void UpdateDisplay()
    {
        if (_gameManager?.CurrentState is null) return;

        var state = _gameManager.CurrentState;
        var phase = _gameManager.Phase;

        // Phase name and instruction
        var (phaseName, instruction) = phase switch
        {
            UIPhase.ShowingBoardDemand => ("BOARD MEETING", "The board has demands..."),
            UIPhase.PlayingCards => ("YOUR TURN", $"Play up to {QuarterGameState.MaxCardsPerQuarter - state.CardsPlayedThisQuarter.Count} cards from your hand"),
            UIPhase.ShowingCrisis => ("SITUATION", "A situation requires your attention. Choose your response."),
            UIPhase.ShowingResolution => ("QUARTER END", "See the results of your choices"),
            UIPhase.GameOver => ("GAME OVER", state.CEO.IsOusted ? "You have been ousted by the board." : ""),
            _ => ("", "")
        };

        if (_phaseLabel is not null)
        {
            _phaseLabel.Text = $"{state.Quarter.FormattedQuarter} - {phaseName}";
        }

        if (_instructionLabel is not null)
        {
            _instructionLabel.Text = instruction;
        }

        // Show board directive if we have one
        if (_directiveLabel is not null)
        {
            if (state.CurrentDirective is not null)
            {
                var directive = state.CurrentDirective;
                _directiveLabel.Text = $"Board demands: {directive.GetDescription(state.CEO.BoardPressureLevel)}";
                _directiveLabel.Visible = true;
            }
            else
            {
                _directiveLabel.Visible = false;
            }
        }

        // Show continue button only for BoardDemand phase
        if (_continueButton is not null)
        {
            var showButton = phase == UIPhase.ShowingBoardDemand;
            // Get the parent container to show/hide
            var buttonParent = _continueButton.GetParent();
            if (buttonParent is Control parentControl)
            {
                parentControl.Visible = showButton;
            }
        }
    }

    private void OnContinuePressed()
    {
        if (_gameManager is null) return;

        if (_gameManager.Phase == UIPhase.ShowingBoardDemand)
        {
            _gameManager.AdvanceBoardDemand();
        }
    }
}
