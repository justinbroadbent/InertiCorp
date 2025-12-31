using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Cards;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying the player's hand of playable cards at the bottom of the screen.
/// </summary>
public partial class HandPanel : PanelContainer
{
    private GameManager? _gameManager;
    private HBoxContainer? _cardsContainer;
    private Label? _infoLabel;
    private Button? _endTurnButton;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style the panel
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.12f, 0.95f),
            BorderColor = new Color(0.3f, 0.3f, 0.35f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        styleBox.BorderWidthTop = 1;
        styleBox.BorderWidthBottom = 1;
        styleBox.BorderWidthLeft = 1;
        styleBox.BorderWidthRight = 1;
        AddThemeStyleboxOverride("panel", styleBox);

        // Create layout
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        AddChild(vbox);

        // Header row with info and end turn button
        var headerRow = new HBoxContainer();
        vbox.AddChild(headerRow);

        _infoLabel = new Label
        {
            Text = "YOUR HAND",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        headerRow.AddChild(_infoLabel);

        _endTurnButton = new Button
        {
            Text = "END TURN",
            CustomMinimumSize = new Vector2(100, 0)
        };
        _endTurnButton.Pressed += OnEndTurnPressed;
        headerRow.AddChild(_endTurnButton);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 140),
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        vbox.AddChild(scroll);

        _cardsContainer = new HBoxContainer();
        _cardsContainer.AddThemeConstantOverride("separation", 12);
        scroll.AddChild(_cardsContainer);

        _gameManager.StateChanged += OnStateChanged;
        _gameManager.PhaseChanged += OnPhaseChanged;

        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= OnStateChanged;
            _gameManager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnStateChanged() => UpdateDisplay();
    private void OnPhaseChanged() => UpdateDisplay();

    private void UpdateDisplay()
    {
        ClearCards();

        if (_gameManager?.CurrentState is null || _cardsContainer is null) return;

        var state = _gameManager.CurrentState;
        var hand = state.Hand;
        var isPlayPhase = state.Quarter.Phase == GamePhase.PlayCards;
        var canPlay = state.CanPlayCard && isPlayPhase;

        // Show/hide based on phase (always visible but dimmed when not your turn)
        Visible = _gameManager.Phase != UIPhase.GameOver;

        // Update info label
        if (_infoLabel is not null)
        {
            var playedCount = state.CardsPlayedThisQuarter.Count;
            var remaining = QuarterGameState.MaxCardsPerQuarter - playedCount;

            if (isPlayPhase)
            {
                _infoLabel.Text = $"YOUR HAND ({hand.Count} cards) - Can play {remaining} more";
                _infoLabel.Modulate = Colors.White;
            }
            else
            {
                _infoLabel.Text = $"YOUR HAND ({hand.Count} cards)";
                _infoLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        // Update end turn button
        if (_endTurnButton is not null)
        {
            _endTurnButton.Visible = isPlayPhase;
            _endTurnButton.Disabled = !isPlayPhase;
        }

        // Create card buttons
        foreach (var card in hand.Cards)
        {
            var cardPanel = CreateCardPanel(card, canPlay);
            _cardsContainer.AddChild(cardPanel);
        }
    }

    private Control CreateCardPanel(PlayableCard card, bool canPlay)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(150, 130)
        };

        var cardStyle = new StyleBoxFlat
        {
            BgColor = card.IsCorporate
                ? new Color(0.3f, 0.15f, 0.15f)
                : new Color(0.15f, 0.15f, 0.2f),
            BorderColor = card.IsCorporate
                ? new Color(0.6f, 0.3f, 0.3f)
                : new Color(0.3f, 0.3f, 0.4f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        };
        cardStyle.BorderWidthTop = 2;
        cardStyle.BorderWidthBottom = 2;
        cardStyle.BorderWidthLeft = 2;
        cardStyle.BorderWidthRight = 2;
        cardStyle.CornerRadiusTopLeft = 5;
        cardStyle.CornerRadiusTopRight = 5;
        cardStyle.CornerRadiusBottomLeft = 5;
        cardStyle.CornerRadiusBottomRight = 5;
        panel.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 3);
        panel.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = card.Title,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        title.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(title);

        // Category tag
        var category = new Label
        {
            Text = $"[{card.Category}]"
        };
        category.AddThemeFontSizeOverride("font_size", 10);
        category.Modulate = new Color(0.7f, 0.7f, 0.7f);
        vbox.AddChild(category);

        // Affinity indicator (if card has meter affinity)
        if (card.HasMeterAffinity && _gameManager?.CurrentState is not null)
        {
            var affinityLabel = CreateAffinityLabel(card, _gameManager.CurrentState.Org);
            vbox.AddChild(affinityLabel);
        }

        // Zero-meter warning (if card would reduce meters at/near zero)
        if (_gameManager?.CurrentState is not null)
        {
            var warningText = card.GetZeroMeterWarningText(_gameManager.CurrentState.Org);
            if (!string.IsNullOrEmpty(warningText))
            {
                var warningLabel = new Label
                {
                    Text = warningText,
                    TooltipText = "WARNING: This card may reduce metrics that are already critical.\nThe negative effects won't apply (meters can't go below 0),\nbut you should prioritize fixing these metrics first."
                };
                warningLabel.AddThemeFontSizeOverride("font_size", 9);
                warningLabel.Modulate = new Color(1.0f, 0.6f, 0.2f); // Orange warning color
                warningLabel.MouseFilter = Control.MouseFilterEnum.Stop;
                vbox.AddChild(warningLabel);
            }
        }

        // Forecast
        var forecast = new Label
        {
            Text = card.GetForecastSummary(),
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        forecast.AddThemeFontSizeOverride("font_size", 10);
        forecast.Modulate = new Color(0.5f, 0.8f, 0.5f);
        vbox.AddChild(forecast);

        // Corporate indicator
        if (card.IsCorporate)
        {
            var corp = new Label
            {
                Text = $"Corporate +{card.CorporateIntensity}"
            };
            corp.AddThemeFontSizeOverride("font_size", 9);
            corp.Modulate = new Color(0.9f, 0.5f, 0.5f);
            vbox.AddChild(corp);
        }

        // Play button
        var button = new Button
        {
            Text = "PLAY",
            Disabled = !canPlay
        };
        var cardId = card.CardId;
        button.Pressed += () => OnPlayCard(cardId);
        vbox.AddChild(button);

        return panel;
    }

    private Label CreateAffinityLabel(PlayableCard card, OrgState org)
    {
        var meterName = card.GetAffinityDisplay();
        var modifier = card.GetAffinityModifier(org);

        // Get current meter value for display
        var meterValue = card.MeterAffinity!.Value switch
        {
            Meter.Delivery => org.Delivery,
            Meter.Morale => org.Morale,
            Meter.Governance => org.Governance,
            Meter.Alignment => org.Alignment,
            Meter.Runway => org.Runway,
            _ => 50
        };

        // Determine status text and color
        string statusText;
        Color statusColor;

        if (modifier >= 15)
        {
            statusText = $"Affinity: {meterName} (Strong+)";
            statusColor = new Color(0.3f, 0.9f, 0.3f); // Bright green
        }
        else if (modifier > 0)
        {
            statusText = $"Affinity: {meterName} (Bonus)";
            statusColor = new Color(0.5f, 0.8f, 0.5f); // Green
        }
        else if (modifier <= -15)
        {
            statusText = $"Affinity: {meterName} (Risky!)";
            statusColor = new Color(0.9f, 0.3f, 0.3f); // Bright red
        }
        else if (modifier < 0)
        {
            statusText = $"Affinity: {meterName} (Penalty)";
            statusColor = new Color(0.9f, 0.6f, 0.3f); // Orange
        }
        else
        {
            statusText = $"Affinity: {meterName}";
            statusColor = new Color(0.6f, 0.6f, 0.8f); // Neutral blue
        }

        var label = new Label
        {
            Text = statusText,
            TooltipText = GetAffinityTooltip(meterName, meterValue, modifier)
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        label.Modulate = statusColor;
        label.MouseFilter = Control.MouseFilterEnum.Stop; // Enable tooltip on hover

        return label;
    }

    private static string GetAffinityTooltip(string meterName, int meterValue, int modifier)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"AFFINITY: {meterName}");
        sb.AppendLine($"Current {meterName}: {meterValue}");
        sb.AppendLine();
        sb.AppendLine("How Affinity Works:");
        sb.AppendLine("• High meter (70+): Strong bonus to good outcomes");
        sb.AppendLine("• Good meter (60-69): Moderate bonus");
        sb.AppendLine("• Neutral (40-59): No effect");
        sb.AppendLine("• Low meter (25-39): Moderate penalty");
        sb.AppendLine("• Very low (<25): Strong penalty to outcomes");
        sb.AppendLine();

        if (modifier >= 15)
            sb.AppendLine($"Status: EXCELLENT (+15% toward good outcome)");
        else if (modifier > 0)
            sb.AppendLine($"Status: GOOD (+8% toward good outcome)");
        else if (modifier <= -15)
            sb.AppendLine($"Status: DANGEROUS (-15% toward bad outcome)");
        else if (modifier < 0)
            sb.AppendLine($"Status: RISKY (-8% toward bad outcome)");
        else
            sb.AppendLine("Status: NEUTRAL (no modifier)");

        return sb.ToString();
    }

    private void OnPlayCard(string cardId)
    {
        _gameManager?.PlayCard(cardId);
    }

    private void OnEndTurnPressed()
    {
        _gameManager?.EndPlayCardsPhase();
    }

    private void ClearCards()
    {
        if (_cardsContainer is null) return;
        foreach (var child in _cardsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
}
