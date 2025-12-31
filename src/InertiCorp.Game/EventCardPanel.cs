using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying the crisis event card during Crisis phase.
/// </summary>
public partial class EventCardPanel : PanelContainer
{
    private GameManager? _gameManager;

    private Label? _phaseLabel;
    private Label? _titleLabel;
    private Label? _descriptionLabel;
    private VBoxContainer? _choicesContainer;

    private bool _choiceMade;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Get UI elements
        _phaseLabel = GetNodeOrNull<Label>("VBox/Phase");
        _titleLabel = GetNode<Label>("VBox/Title");
        _descriptionLabel = GetNode<Label>("VBox/Description");
        _choicesContainer = GetNode<VBoxContainer>("VBox/Choices");

        _gameManager.StateChanged += OnStateChanged;
        _gameManager.PhaseChanged += OnPhaseChanged;

        UpdateVisibility();
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

    private void OnPhaseChanged()
    {
        _choiceMade = false;
        UpdateVisibility();
        UpdateDisplay();
    }

    private void UpdateVisibility()
    {
        // Only show during Crisis phase
        Visible = _gameManager?.Phase == UIPhase.ShowingCrisis;
    }

    private void UpdateDisplay()
    {
        if (_gameManager is null) return;

        var currentCrisis = _gameManager.CurrentCrisis;
        var gameState = _gameManager.CurrentState;

        if (currentCrisis is null || gameState is null)
        {
            if (_phaseLabel is not null) _phaseLabel.Text = "";
            if (_titleLabel is not null) _titleLabel.Text = "No Situation";
            if (_descriptionLabel is not null) _descriptionLabel.Text = "";
            ClearChoices();
            return;
        }

        // Show the current phase prominently
        if (_phaseLabel is not null)
        {
            _phaseLabel.Text = "SITUATION";
            _phaseLabel.Modulate = new Color(0.9f, 0.5f, 0.2f);
        }

        // Update title and description
        if (_titleLabel is not null) _titleLabel.Text = currentCrisis.Title;
        if (_descriptionLabel is not null) _descriptionLabel.Text = currentCrisis.Description;

        // Update choices with forecasts
        UpdateChoices(currentCrisis, gameState);
    }

    private void UpdateChoices(EventCard currentEvent, QuarterGameState state)
    {
        ClearChoices();

        if (_choicesContainer is null) return;

        foreach (var choice in currentEvent.Choices)
        {
            var choicePanel = new PanelContainer();

            // Style based on choice type
            var panelStyle = new StyleBoxFlat
            {
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6
            };
            panelStyle.CornerRadiusTopLeft = 4;
            panelStyle.CornerRadiusTopRight = 4;
            panelStyle.CornerRadiusBottomLeft = 4;
            panelStyle.CornerRadiusBottomRight = 4;

            if (choice.HasPCCost)
            {
                // PC option - blue tint (reliable)
                panelStyle.BgColor = new Color(0.1f, 0.15f, 0.2f);
                panelStyle.BorderColor = new Color(0.3f, 0.5f, 0.7f);
                panelStyle.BorderWidthLeft = 3;
            }
            else if (choice.IsCorporateChoice)
            {
                // Evil option - red/purple tint (scandal risk)
                panelStyle.BgColor = new Color(0.2f, 0.1f, 0.15f);
                panelStyle.BorderColor = new Color(0.7f, 0.3f, 0.5f);
                panelStyle.BorderWidthLeft = 3;
            }
            else
            {
                // Standard option - neutral
                panelStyle.BgColor = new Color(0.12f, 0.12f, 0.14f);
            }

            choicePanel.AddThemeStyleboxOverride("panel", panelStyle);

            var choiceContainer = new VBoxContainer();
            choiceContainer.AddThemeConstantOverride("separation", 4);
            choicePanel.AddChild(choiceContainer);

            // Choice type indicator with probabilities
            var typeLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            typeLabel.AddThemeFontSizeOverride("font_size", 10);

            if (choice.HasPCCost)
            {
                typeLabel.Text = $"[INVEST {choice.PCCost} PC] 70% Success / 10% Failure";
                typeLabel.Modulate = new Color(0.4f, 0.7f, 0.9f);

                // Check if player can afford it
                if (state.Resources.PoliticalCapital < choice.PCCost)
                {
                    typeLabel.Text += " (Can't Afford)";
                    typeLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }
            }
            else if (choice.IsCorporateChoice)
            {
                typeLabel.Text = $"[EVIL CEO] 70% Success / 20% Scandal Risk (+{choice.CorporateIntensityDelta} Evil)";
                typeLabel.Modulate = new Color(0.9f, 0.4f, 0.6f);
            }
            else
            {
                typeLabel.Text = "[SAFE] 20% Success / 70% Expected / 10% Failure";
                typeLabel.Modulate = new Color(0.6f, 0.7f, 0.6f);
            }
            choiceContainer.AddChild(typeLabel);

            // Main choice button
            var button = new Button
            {
                Text = choice.Label,
                Disabled = _choiceMade || (choice.HasPCCost && state.Resources.PoliticalCapital < choice.PCCost)
            };

            var choiceId = choice.ChoiceId;
            button.Pressed += () => OnChoicePressed(choiceId);
            choiceContainer.AddChild(button);

            // Expected outcome effects
            var forecastLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };
            forecastLabel.AddThemeFontSizeOverride("font_size", 10);

            if (choice.HasTieredOutcomes && choice.OutcomeProfile is not null)
            {
                var expectedEffects = choice.OutcomeProfile.Expected;
                if (expectedEffects.Count > 0)
                {
                    forecastLabel.Text = $"Expected: {DescribeEffects(expectedEffects)}";
                }
            }
            forecastLabel.Modulate = new Color(0.6f, 0.6f, 0.6f);
            choiceContainer.AddChild(forecastLabel);

            _choicesContainer.AddChild(choicePanel);
        }
    }

    private static string GetForecast(Choice choice, QuarterGameState state)
    {
        // Calculate risk level
        int alignment = state.Org.Alignment;
        int pressure = state.CEO.BoardPressureLevel;

        RiskLevel risk;
        if (alignment >= 60 && pressure <= 2)
            risk = RiskLevel.Low;
        else if (alignment < 40 || pressure >= 5)
            risk = RiskLevel.High;
        else
            risk = RiskLevel.Medium;

        // Get expected outcome description
        string expected = "Unknown effect";
        if (choice.HasTieredOutcomes && choice.OutcomeProfile is not null)
        {
            var expectedEffects = choice.OutcomeProfile.Expected;
            if (expectedEffects.Count > 0)
            {
                expected = DescribeEffects(expectedEffects);
            }
        }
        else if (choice.Effects.Count > 0)
        {
            expected = DescribeEffects(choice.Effects);
        }

        return $"Expected: {expected} | Risk: {risk}";
    }

    private static string DescribeEffects(IReadOnlyList<IEffect> effects)
    {
        var parts = new List<string>();
        foreach (var effect in effects)
        {
            if (effect is MeterEffect me)
            {
                var sign = me.Delta >= 0 ? "+" : "";
                var meterName = me.Meter switch
                {
                    Meter.Delivery => "Del",
                    Meter.Morale => "Mor",
                    Meter.Governance => "Gov",
                    Meter.Alignment => "Ali",
                    Meter.Runway => "Run",
                    _ => me.Meter.ToString()[..3]
                };
                parts.Add($"{meterName}{sign}{me.Delta}");
            }
        }
        return parts.Count > 0 ? string.Join(", ", parts) : "No change";
    }

    private void ClearChoices()
    {
        if (_choicesContainer is null) return;

        foreach (var child in _choicesContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnChoicePressed(string choiceId)
    {
        if (_choiceMade || _gameManager is null) return;

        _choiceMade = true;

        // Disable all buttons immediately
        if (_choicesContainer is not null)
        {
            foreach (var child in _choicesContainer.GetChildren())
            {
                if (child is VBoxContainer container)
                {
                    foreach (var subChild in container.GetChildren())
                    {
                        if (subChild is Button button)
                        {
                            button.Disabled = true;
                        }
                    }
                }
                else if (child is Button button)
                {
                    button.Disabled = true;
                }
            }
        }

        _gameManager.MakeCrisisChoice(choiceId);
    }
}
