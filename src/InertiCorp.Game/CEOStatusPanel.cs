using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying CEO survival stats with clear descriptions.
/// </summary>
public partial class CEOStatusPanel : PanelContainer
{
    private GameManager? _gameManager;
    private VBoxContainer? _container;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style the panel
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

        _container = new VBoxContainer();
        _container.AddThemeConstantOverride("separation", 8);
        AddChild(_container);

        var title = new Label
        {
            Text = "YOUR POSITION",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.Modulate = new Color(0.8f, 0.8f, 0.8f);
        _container.AddChild(title);

        var sep = new HSeparator();
        _container.AddChild(sep);

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
        if (_gameManager?.CurrentState is null || _container is null) return;

        // Clear existing stats (after title and separator)
        while (_container.GetChildCount() > 2)
        {
            var child = _container.GetChild(2);
            _container.RemoveChild(child);
            child.QueueFree();
        }

        var state = _gameManager.CurrentState;
        var ceo = state.CEO;

        // Board Favorability - most important
        CreateStatRow(
            "Board Favor",
            $"{ceo.BoardFavorability}%",
            GetFavorabilityDesc(ceo.BoardFavorability),
            GetFavorabilityColor(ceo.BoardFavorability),
            showBar: true,
            barValue: ceo.BoardFavorability
        );

        // Pressure Level
        CreateStatRow(
            "Pressure",
            $"Level {ceo.BoardPressureLevel}",
            GetPressureDesc(ceo.BoardPressureLevel),
            GetPressureColor(ceo.BoardPressureLevel)
        );

        // Profit
        var profitColor = ceo.LastQuarterProfit >= 0
            ? new Color(0.5f, 0.9f, 0.5f)
            : new Color(0.9f, 0.5f, 0.5f);
        CreateStatRow(
            "Profit",
            $"${ceo.LastQuarterProfit}M (Total: ${ceo.TotalProfit}M)",
            "Last quarter earnings",
            profitColor
        );

        // Evil Score (only show if > 0)
        if (ceo.EvilScore > 0)
        {
            CreateStatRow(
                "Corporate Evil",
                ceo.EvilScore.ToString(),
                "From corporate BS cards",
                new Color(0.9f, 0.4f, 0.4f)
            );
        }

        // Risk Level
        var riskLevel = CalculateCurrentRisk(state);
        CreateStatRow(
            "Outcome Risk",
            riskLevel.ToString(),
            GetRiskDesc(riskLevel),
            GetRiskColor(riskLevel)
        );
    }

    private void CreateStatRow(string name, string value, string desc, Color valueColor, bool showBar = false, int barValue = 0)
    {
        if (_container is null) return;

        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 2);

        // Name + value
        var nameRow = new HBoxContainer();

        var nameLabel = new Label
        {
            Text = name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameRow.AddChild(nameLabel);

        var valueLabel = new Label
        {
            Text = value
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 12);
        valueLabel.Modulate = valueColor;
        nameRow.AddChild(valueLabel);

        row.AddChild(nameRow);

        // Optional progress bar
        if (showBar)
        {
            var bar = new ProgressBar
            {
                CustomMinimumSize = new Vector2(0, 6),
                MaxValue = 100,
                Value = barValue,
                ShowPercentage = false
            };
            var bgStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.2f, 0.2f, 0.25f)
            };
            bgStyle.CornerRadiusTopLeft = 3;
            bgStyle.CornerRadiusTopRight = 3;
            bgStyle.CornerRadiusBottomLeft = 3;
            bgStyle.CornerRadiusBottomRight = 3;
            bar.AddThemeStyleboxOverride("background", bgStyle);

            var fillStyle = new StyleBoxFlat
            {
                BgColor = valueColor
            };
            fillStyle.CornerRadiusTopLeft = 3;
            fillStyle.CornerRadiusTopRight = 3;
            fillStyle.CornerRadiusBottomLeft = 3;
            fillStyle.CornerRadiusBottomRight = 3;
            bar.AddThemeStyleboxOverride("fill", fillStyle);

            row.AddChild(bar);
        }

        // Description
        var descLabel = new Label
        {
            Text = desc
        };
        descLabel.AddThemeFontSizeOverride("font_size", 9);
        descLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
        row.AddChild(descLabel);

        _container.AddChild(row);
    }

    private static RiskLevel CalculateCurrentRisk(QuarterGameState state)
    {
        int alignment = state.Org.Alignment;
        int pressure = state.CEO.BoardPressureLevel;

        if (alignment >= 60 && pressure <= 2)
            return RiskLevel.Low;
        if (alignment < 40 || pressure >= 5)
            return RiskLevel.High;
        return RiskLevel.Medium;
    }

    private static string GetFavorabilityDesc(int fav)
    {
        if (fav >= 60) return "Board supports you";
        if (fav >= 40) return "Board is watching";
        if (fav >= 20) return "Board is concerned";
        return "Ouster vote imminent!";
    }

    private static string GetPressureDesc(int pressure)
    {
        if (pressure <= 2) return "Normal expectations";
        if (pressure <= 4) return "Heightened scrutiny";
        return "Maximum pressure!";
    }

    private static string GetRiskDesc(RiskLevel risk)
    {
        return risk switch
        {
            RiskLevel.Low => "Favorable outcomes likely",
            RiskLevel.Medium => "Unpredictable outcomes",
            RiskLevel.High => "Poor outcomes likely",
            _ => ""
        };
    }

    private static Color GetFavorabilityColor(int favorability)
    {
        if (favorability >= 60)
            return new Color(0.3f, 0.8f, 0.3f);
        if (favorability >= 40)
            return new Color(0.9f, 0.8f, 0.2f);
        if (favorability >= 20)
            return new Color(0.9f, 0.5f, 0.2f);
        return new Color(0.9f, 0.3f, 0.3f);
    }

    private static Color GetPressureColor(int pressure)
    {
        if (pressure <= 2)
            return new Color(0.7f, 0.7f, 0.7f);
        if (pressure <= 4)
            return new Color(0.9f, 0.8f, 0.2f);
        return new Color(0.9f, 0.3f, 0.3f);
    }

    private static Color GetRiskColor(RiskLevel risk)
    {
        return risk switch
        {
            RiskLevel.Low => new Color(0.3f, 0.8f, 0.3f),
            RiskLevel.Medium => new Color(0.9f, 0.8f, 0.2f),
            RiskLevel.High => new Color(0.9f, 0.3f, 0.3f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }
}
