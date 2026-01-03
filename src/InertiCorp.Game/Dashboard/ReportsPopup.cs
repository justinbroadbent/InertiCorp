using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Content;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InertiCorp.Game.Dashboard;

/// <summary>
/// Corporate reports popup with charts, graphs, and appropriately silly metrics.
/// "Because nothing says 'we're doing great' like a pie chart."
/// </summary>
public partial class ReportsPopup : Control
{
    private PanelContainer? _popup;
    private CEOState? _ceo;
    private OrgState? _org;
    private List<QuarterSnapshot> _history = new();
    private int _totalCardsPlayed;
    private Dictionary<int, int> _sillyKPIs = new();
    private Dictionary<int, int>? _liveKPIs; // Reference to dashboard's live KPI dictionary
    private GridContainer? _sillyKPIGrid;
    private double _kpiUpdateTimer = 0;
    private const double KPIUpdateInterval = 3.0; // Update every 3 seconds

    // Chart colors
    private static readonly Color ChartBlue = new(0.3f, 0.6f, 0.9f);
    private static readonly Color ChartGreen = new(0.4f, 0.8f, 0.4f);
    private static readonly Color ChartOrange = new(0.9f, 0.6f, 0.3f);
    private static readonly Color ChartPurple = new(0.7f, 0.5f, 0.9f);
    private static readonly Color ChartPink = new(0.9f, 0.5f, 0.7f);
    private static readonly Color ChartYellow = new(0.9f, 0.8f, 0.3f);
    private static readonly Color ChartTeal = new(0.3f, 0.8f, 0.8f);
    private static readonly Color ChartRed = new(0.9f, 0.4f, 0.4f);

    private static readonly Color[] PieColors = { ChartBlue, ChartGreen, ChartOrange, ChartPurple, ChartPink, ChartYellow, ChartTeal, ChartRed, new(0.6f, 0.6f, 0.6f), new(0.4f, 0.4f, 0.5f) };

    public override void _Ready()
    {
        BuildPopup();
    }

    private void BuildPopup()
    {
        _popup = new PanelContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(900, 650),
            MouseFilter = MouseFilterEnum.Stop
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f),
            BorderColor = new Color(0.2f, 0.3f, 0.5f),
            BorderWidthBottom = 2,
            BorderWidthTop = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 15,
            ContentMarginBottom = 15
        };
        _popup.AddThemeStyleboxOverride("panel", style);

        AddChild(_popup);
    }

    public void Show(QuarterHistory history, CEOState ceo, OrgState org, int totalCardsPlayed, Dictionary<int, int>? sillyKPIs = null)
    {
        if (_popup is null) return;

        _ceo = ceo;
        _org = org;
        _history = history.Snapshots.ToList();
        _totalCardsPlayed = totalCardsPlayed;
        _sillyKPIs = sillyKPIs != null ? new Dictionary<int, int>(sillyKPIs) : new Dictionary<int, int>();
        _liveKPIs = sillyKPIs; // Keep reference for live updates
        _kpiUpdateTimer = 0;

        // Clear and rebuild content
        foreach (var child in _popup.GetChildren())
        {
            child.QueueFree();
        }

        var content = BuildContent();
        _popup.AddChild(content);

        // Center popup
        var viewportSize = GetViewportRect().Size;
        _popup.Position = new Vector2(
            (viewportSize.X - 900) / 2,
            (viewportSize.Y - 650) / 2
        );

        // Animate in
        _popup.Visible = true;
        _popup.Modulate = new Color(1, 1, 1, 0);
        _popup.Scale = new Vector2(0.95f, 0.95f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_popup, "modulate:a", 1.0f, 0.2f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_popup, "scale", Vector2.One, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    public new void Hide()
    {
        if (_popup is null || !_popup.Visible) return;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_popup, "modulate:a", 0.0f, 0.15f)
            .SetEase(Tween.EaseType.In);
        tween.TweenProperty(_popup, "scale", new Vector2(0.95f, 0.95f), 0.15f)
            .SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            _popup.Visible = false;
        }));
    }

    public new bool IsVisible => _popup?.Visible ?? false;

    public override void _Process(double delta)
    {
        if (!IsVisible || _liveKPIs is null || _sillyKPIGrid is null) return;

        _kpiUpdateTimer += delta;
        if (_kpiUpdateTimer >= KPIUpdateInterval)
        {
            _kpiUpdateTimer = 0;
            UpdateSillyKPIValues();
        }
    }

    private void UpdateSillyKPIValues()
    {
        if (_sillyKPIGrid is null || _liveKPIs is null) return;

        // Get the current children (pairs of name + value labels)
        var children = _sillyKPIGrid.GetChildren();
        var kpiIndices = _liveKPIs.Keys.Take(8).ToList();

        for (int i = 0; i < Math.Min(kpiIndices.Count, 8); i++)
        {
            var idx = kpiIndices[i];
            var kpiDef = SillyKPIs.GetKPI(idx);
            var newValue = _liveKPIs.TryGetValue(idx, out var v) ? v : 50;
            var oldValue = _sillyKPIs.TryGetValue(idx, out var ov) ? ov : 50;

            // Only update if changed
            if (newValue != oldValue)
            {
                _sillyKPIs[idx] = newValue;

                // Get the value label (every second child in the grid)
                var valueIndex = i * 2 + 1;
                if (valueIndex < children.Count && children[valueIndex] is Label valueLabel)
                {
                    var change = newValue - oldValue;
                    var arrow = change > 0 ? "▲" : "▼";

                    // Animate the value change
                    valueLabel.Text = $"{newValue} {kpiDef.Unit} {arrow}";
                    valueLabel.AddThemeColorOverride("font_color", change > 0
                        ? new Color(0.4f, 0.9f, 0.4f)
                        : new Color(0.9f, 0.4f, 0.4f));

                    // Flash effect
                    var tween = CreateTween();
                    tween.TweenProperty(valueLabel, "modulate:a", 0.5f, 0.1f);
                    tween.TweenProperty(valueLabel, "modulate:a", 1.0f, 0.1f);

                    // Reset text after delay (remove arrow)
                    tween.TweenInterval(1.0);
                    var finalValue = newValue;
                    var unit = kpiDef.Unit;
                    tween.TweenCallback(Callable.From(() =>
                    {
                        if (valueLabel is not null)
                        {
                            valueLabel.Text = $"{finalValue} {unit}";
                            valueLabel.AddThemeColorOverride("font_color", GetKpiColor(finalValue));
                        }
                    }));
                }
            }
        }
    }

    private Control BuildContent()
    {
        var main = new VBoxContainer();
        main.AddThemeConstantOverride("separation", 15);

        // Header with title and close button
        main.AddChild(BuildHeader());

        // Two-column layout for charts
        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 20);
        columns.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Left column: Line chart + KPIs
        var leftCol = new VBoxContainer();
        leftCol.AddThemeConstantOverride("separation", 15);
        leftCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        leftCol.SizeFlagsStretchRatio = 1.2f;

        leftCol.AddChild(BuildLineChart());
        leftCol.AddChild(BuildKPIPanel());

        columns.AddChild(leftCol);

        // Right column: Pie charts + Silly KPIs
        var rightCol = new VBoxContainer();
        rightCol.AddThemeConstantOverride("separation", 15);
        rightCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        rightCol.AddChild(BuildPieChart("Time Allocation Analysis", GetTimeAllocationData()));
        rightCol.AddChild(BuildSillyKPIPanel());

        columns.AddChild(rightCol);

        main.AddChild(columns);

        // Footer with corporate disclaimer
        main.AddChild(BuildFooter());

        return main;
    }

    private Control BuildHeader()
    {
        var header = new HBoxContainer();
        header.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Title
        var title = new Label
        {
            Text = $"📊 {QuarterHistory.GetBuzzwordTitle(_ceo?.QuartersSurvived ?? 1)} - Q{_ceo?.QuartersSurvived ?? 1}",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        header.AddChild(title);

        // Logo in header (prominent)
        var logoTexture = GD.Load<Texture2D>("res://logo.png");
        if (logoTexture != null)
        {
            var logo = new TextureRect
            {
                Texture = logoTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                CustomMinimumSize = new Vector2(250, 65)
            };
            header.AddChild(logo);
        }

        // Close button
        var closeBtn = new Button
        {
            Text = "✕ Close",
            CustomMinimumSize = new Vector2(80, 32)
        };
        var btnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.5f, 0.2f, 0.2f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10
        };
        closeBtn.AddThemeStyleboxOverride("normal", btnStyle);
        var hoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.6f, 0.25f, 0.25f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10
        };
        var pressedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.4f, 0.15f, 0.15f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10
        };
        closeBtn.AddThemeStyleboxOverride("hover", hoverStyle);
        closeBtn.AddThemeStyleboxOverride("pressed", pressedStyle);
        closeBtn.Pressed += Hide;
        header.AddChild(closeBtn);

        return header;
    }

    private Control BuildLineChart()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 200)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            BorderColor = new Color(0.15f, 0.15f, 0.2f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 5);

        // Title
        var title = new Label { Text = "📈 Quarterly Performance Trajectory" };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        vbox.AddChild(title);

        // Chart area
        var chartArea = new LineChartControl(_history);
        chartArea.CustomMinimumSize = new Vector2(0, 150);
        chartArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(chartArea);

        panel.AddChild(vbox);
        return panel;
    }

    private Control BuildPieChart(string title, List<(string Label, float Value)> data)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 180)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            BorderColor = new Color(0.15f, 0.15f, 0.2f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 15);

        // Pie chart
        var pieControl = new PieChartControl(data);
        pieControl.CustomMinimumSize = new Vector2(120, 120);
        hbox.AddChild(pieControl);

        // Legend
        var legendBox = new VBoxContainer();
        legendBox.AddThemeConstantOverride("separation", 2);
        legendBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var titleLabel = new Label { Text = $"🥧 {title}" };
        titleLabel.AddThemeFontSizeOverride("font_size", 12);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        legendBox.AddChild(titleLabel);

        for (int i = 0; i < Math.Min(data.Count, 6); i++)
        {
            var item = data[i];
            var legendItem = new HBoxContainer();
            legendItem.AddThemeConstantOverride("separation", 5);

            var colorBox = new ColorRect
            {
                Color = PieColors[i % PieColors.Length],
                CustomMinimumSize = new Vector2(10, 10)
            };
            legendItem.AddChild(colorBox);

            var label = new Label { Text = $"{item.Label}: {item.Value:F0}%" };
            label.AddThemeFontSizeOverride("font_size", 10);
            label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
            legendItem.AddChild(label);

            legendBox.AddChild(legendItem);
        }

        if (data.Count > 6)
        {
            var moreLabel = new Label { Text = $"... and {data.Count - 6} more" };
            moreLabel.AddThemeFontSizeOverride("font_size", 9);
            moreLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
            legendBox.AddChild(moreLabel);
        }

        hbox.AddChild(legendBox);
        panel.AddChild(hbox);
        return panel;
    }

    private Control BuildKPIPanel()
    {
        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            BorderColor = new Color(0.15f, 0.15f, 0.2f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);

        var title = new Label { Text = "📊 Key Performance Indicators" };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        vbox.AddChild(title);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 5);

        var kpis = _ceo is not null && _org is not null
            ? QuarterHistory.GetKPIs(_ceo, _org, _totalCardsPlayed)
            : new List<(string, float, string)>();

        foreach (var kpi in kpis.Take(8))
        {
            var nameLabel = new Label { Text = kpi.Item1 };
            nameLabel.AddThemeFontSizeOverride("font_size", 11);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));

            var valueLabel = new Label { Text = $"{kpi.Item2:F1} {kpi.Item3}" };
            valueLabel.AddThemeFontSizeOverride("font_size", 11);
            valueLabel.AddThemeColorOverride("font_color", GetKpiColor(kpi.Item2));

            grid.AddChild(nameLabel);
            grid.AddChild(valueLabel);
        }

        vbox.AddChild(grid);
        panel.AddChild(vbox);
        return panel;
    }

    private static Color GetKpiColor(float value)
    {
        return value switch
        {
            >= 70 => new Color(0.4f, 0.9f, 0.4f),
            >= 50 => new Color(0.9f, 0.9f, 0.4f),
            >= 30 => new Color(0.9f, 0.6f, 0.3f),
            _ => new Color(0.9f, 0.4f, 0.4f)
        };
    }

    private Control BuildSillyKPIPanel()
    {
        var panel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.05f, 0.04f),
            BorderColor = new Color(0.3f, 0.25f, 0.15f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);

        var title = new Label { Text = "📈 Strategic Performance Metrics" };
        title.AddThemeFontSizeOverride("font_size", 13);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.5f));
        vbox.AddChild(title);

        var subtitle = new Label { Text = "Powered by Advanced Analytics™" };
        subtitle.AddThemeFontSizeOverride("font_size", 9);
        subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.45f, 0.35f));
        vbox.AddChild(subtitle);

        _sillyKPIGrid = new GridContainer { Columns = 2 };
        _sillyKPIGrid.AddThemeConstantOverride("h_separation", 15);
        _sillyKPIGrid.AddThemeConstantOverride("v_separation", 4);

        // Show 8 silly KPIs
        var kpiIndices = _sillyKPIs.Keys.Take(8).ToList();
        if (kpiIndices.Count == 0)
        {
            // Fallback: generate some random values
            var rng = new Random();
            for (int i = 0; i < 8; i++)
            {
                kpiIndices.Add(i);
                if (!_sillyKPIs.ContainsKey(i))
                    _sillyKPIs[i] = rng.Next(30, 90);
            }
        }

        foreach (var idx in kpiIndices)
        {
            var kpiDef = SillyKPIs.GetKPI(idx);
            var value = _sillyKPIs.TryGetValue(idx, out var v) ? v : 50;

            var nameLabel = new Label { Text = kpiDef.Name };
            nameLabel.AddThemeFontSizeOverride("font_size", 10);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.55f, 0.45f));

            var valueLabel = new Label { Text = $"{value} {kpiDef.Unit}" };
            valueLabel.AddThemeFontSizeOverride("font_size", 10);
            valueLabel.AddThemeColorOverride("font_color", GetKpiColor(value));

            _sillyKPIGrid.AddChild(nameLabel);
            _sillyKPIGrid.AddChild(valueLabel);
        }

        vbox.AddChild(_sillyKPIGrid);

        var disclaimer = new Label { Text = "* Metrics may not reflect actual business outcomes" };
        disclaimer.AddThemeFontSizeOverride("font_size", 8);
        disclaimer.AddThemeColorOverride("font_color", new Color(0.4f, 0.35f, 0.3f));
        vbox.AddChild(disclaimer);

        panel.AddChild(vbox);
        return panel;
    }

    private Control BuildFooter()
    {
        var footer = new Label
        {
            Text = "* All metrics are certified by the Department of Synergy. Past performance is definitely indicative of future results. Trust us.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        footer.AddThemeFontSizeOverride("font_size", 9);
        footer.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        return footer;
    }

    private List<(string, float)> GetTimeAllocationData()
    {
        var quarters = _ceo?.QuartersSurvived ?? 1;
        return QuarterHistory.GetTimeAllocation(quarters)
            .Select(t => (t.Activity, t.Percentage))
            .ToList();
    }

    private List<(string, float)> GetStakeholderData()
    {
        if (_ceo is null) return new List<(string, float)>
        {
            ("Enthusiastic", 20), ("Cautiously Optimistic", 25), ("Neutral", 30),
            ("Mildly Concerned", 15), ("Actively Plotting", 10)
        };

        var fav = _ceo.BoardFavorability;
        var evil = _ceo.EvilScore;

        // Sentiment shifts based on actual metrics
        return new List<(string, float)>
        {
            ("Enthusiastic Supporters", Math.Max(5, fav * 0.3f)),
            ("Cautiously Optimistic", Math.Max(10, fav * 0.25f)),
            ("Neutral Observers", 25 - evil * 0.3f),
            ("Concerned Stakeholders", Math.Min(30, 15 + evil * 0.5f)),
            ("Actively Updating Resume", Math.Min(25, 10 + (100 - fav) * 0.15f + evil * 0.3f))
        };
    }
}

/// <summary>
/// Custom control for drawing a combined line and bar chart.
/// Lines: Profit (blue), Board Favorability (green)
/// Bars: Projects completed per quarter (purple)
/// </summary>
public partial class LineChartControl : Control
{
    private readonly List<QuarterSnapshot> _history;
    private static readonly Color LineColor = new(0.3f, 0.7f, 0.9f);
    private static readonly Color FavColor = new(0.4f, 0.9f, 0.4f);
    private static readonly Color ProjectsColor = new(0.7f, 0.5f, 0.9f); // Purple for projects
    private static readonly Color GridColor = new(0.15f, 0.15f, 0.2f);

    public LineChartControl(List<QuarterSnapshot> history)
    {
        _history = history;
    }

    public override void _Draw()
    {
        var rect = GetRect();
        var padding = 30f;
        var chartWidth = rect.Size.X - padding * 2;
        var chartHeight = rect.Size.Y - padding * 2;

        // Draw grid
        for (int i = 0; i <= 4; i++)
        {
            var y = padding + chartHeight * i / 4;
            DrawLine(new Vector2(padding, y), new Vector2(rect.Size.X - padding, y), GridColor, 1);
        }

        if (_history.Count < 2)
        {
            // Not enough data
            DrawString(ThemeDB.FallbackFont, new Vector2(rect.Size.X / 2 - 60, rect.Size.Y / 2),
                "Insufficient data for analysis", HorizontalAlignment.Left, -1, 12, new Color(0.5f, 0.5f, 0.6f));
            return;
        }

        // Draw projects bar chart first (behind the lines)
        // Max projects per quarter is typically 3
        var maxProjects = Math.Max(3, _history.Max(h => h.CardsPlayed));
        var barWidth = Math.Min(20f, chartWidth / _history.Count * 0.6f);

        for (int i = 0; i < _history.Count; i++)
        {
            var projects = _history[i].CardsPlayed;
            if (projects > 0)
            {
                var x = padding + chartWidth * i / (_history.Count - 1) - barWidth / 2;
                var barHeight = (float)projects / maxProjects * chartHeight * 0.4f; // Bars take up to 40% of chart height
                var y = padding + chartHeight - barHeight;

                // Draw bar with slight transparency
                var barColor = new Color(ProjectsColor.R, ProjectsColor.G, ProjectsColor.B, 0.5f);
                DrawRect(new Rect2(x, y, barWidth, barHeight), barColor);

                // Draw bar outline
                DrawRect(new Rect2(x, y, barWidth, barHeight), ProjectsColor, false, 1);

                // Draw project count on bar if tall enough
                if (barHeight > 15)
                {
                    DrawString(ThemeDB.FallbackFont, new Vector2(x + barWidth / 2 - 3, y + 12),
                        projects.ToString(), HorizontalAlignment.Left, -1, 9, Colors.White);
                }
            }
        }

        // Calculate ranges for profit line
        var maxProfit = _history.Max(h => Math.Abs(h.Profit)) + 10;
        var minProfit = -maxProfit;

        // Draw profit line
        var points = new List<Vector2>();
        for (int i = 0; i < _history.Count; i++)
        {
            var x = padding + chartWidth * i / (_history.Count - 1);
            var normalizedY = (float)(_history[i].Profit - minProfit) / (maxProfit - minProfit);
            var y = padding + chartHeight * (1 - normalizedY);
            points.Add(new Vector2(x, y));
        }

        // Draw line
        for (int i = 1; i < points.Count; i++)
        {
            DrawLine(points[i - 1], points[i], LineColor, 2);
        }

        // Draw points
        foreach (var point in points)
        {
            DrawCircle(point, 4, LineColor);
        }

        // Draw favorability line (secondary)
        var favPoints = new List<Vector2>();
        for (int i = 0; i < _history.Count; i++)
        {
            var x = padding + chartWidth * i / (_history.Count - 1);
            var normalizedY = _history[i].BoardFavorability / 100f;
            var y = padding + chartHeight * (1 - normalizedY);
            favPoints.Add(new Vector2(x, y));
        }

        for (int i = 1; i < favPoints.Count; i++)
        {
            DrawLine(favPoints[i - 1], favPoints[i], FavColor, 1.5f);
        }

        // Labels
        DrawString(ThemeDB.FallbackFont, new Vector2(5, padding - 5), "Profit", HorizontalAlignment.Left, -1, 10, LineColor);
        DrawString(ThemeDB.FallbackFont, new Vector2(5, padding + 15), "Fav %", HorizontalAlignment.Left, -1, 10, FavColor);
        DrawString(ThemeDB.FallbackFont, new Vector2(5, padding + 35), "Projects", HorizontalAlignment.Left, -1, 10, ProjectsColor);

        // X-axis labels
        for (int i = 0; i < _history.Count; i++)
        {
            var x = padding + chartWidth * i / (_history.Count - 1);
            DrawString(ThemeDB.FallbackFont, new Vector2(x - 10, rect.Size.Y - 5),
                QuarterState.FormatQuarter(_history[i].QuarterNumber), HorizontalAlignment.Left, -1, 8, new Color(0.5f, 0.5f, 0.6f));
        }
    }
}

/// <summary>
/// Custom control for drawing a pie chart.
/// </summary>
public partial class PieChartControl : Control
{
    private readonly List<(string Label, float Value)> _data;
    private static readonly Color[] Colors = {
        new(0.3f, 0.6f, 0.9f), new(0.4f, 0.8f, 0.4f), new(0.9f, 0.6f, 0.3f),
        new(0.7f, 0.5f, 0.9f), new(0.9f, 0.5f, 0.7f), new(0.9f, 0.8f, 0.3f),
        new(0.3f, 0.8f, 0.8f), new(0.9f, 0.4f, 0.4f), new(0.6f, 0.6f, 0.6f),
        new(0.4f, 0.4f, 0.5f)
    };

    public PieChartControl(List<(string Label, float Value)> data)
    {
        _data = data;
    }

    public override void _Draw()
    {
        var center = Size / 2;
        var radius = Math.Min(Size.X, Size.Y) / 2 - 5;

        var total = _data.Sum(d => d.Value);
        if (total <= 0) return;

        var startAngle = -Mathf.Pi / 2; // Start from top

        for (int i = 0; i < _data.Count; i++)
        {
            var sweepAngle = _data[i].Value / total * Mathf.Pi * 2;
            var color = Colors[i % Colors.Length];

            // Draw pie slice using polygon approximation
            var points = new List<Vector2> { center };
            var segments = Math.Max(3, (int)(sweepAngle * 20));

            for (int j = 0; j <= segments; j++)
            {
                var angle = startAngle + sweepAngle * j / segments;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            DrawPolygon(points.ToArray(), new[] { color });

            startAngle += sweepAngle;
        }

        // Draw border
        DrawArc(center, radius, 0, Mathf.Pi * 2, 32, new Color(0.2f, 0.2f, 0.25f), 2);
    }
}
