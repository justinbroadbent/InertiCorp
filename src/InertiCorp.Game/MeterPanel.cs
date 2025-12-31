using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying all 5 organization meters with descriptions.
/// </summary>
public partial class MeterPanel : PanelContainer
{
    private GameManager? _gameManager;
    private VBoxContainer? _container;

    private static readonly Dictionary<Meter, (string Name, string Desc, Color Color)> MeterInfo = new()
    {
        { Meter.Delivery, ("Delivery", "Product execution speed", new Color(0.3f, 0.7f, 0.9f)) },
        { Meter.Morale, ("Morale", "Team happiness", new Color(0.9f, 0.7f, 0.3f)) },
        { Meter.Governance, ("Governance", "Process & compliance", new Color(0.7f, 0.5f, 0.8f)) },
        { Meter.Alignment, ("Alignment", "Board confidence (affects outcomes)", new Color(0.5f, 0.8f, 0.5f)) },
        { Meter.Runway, ("Runway", "Financial health", new Color(0.9f, 0.4f, 0.4f)) }
    };

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
        _container.AddThemeConstantOverride("separation", 6);
        AddChild(_container);

        var title = new Label
        {
            Text = "COMPANY HEALTH",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.Modulate = new Color(0.8f, 0.8f, 0.8f);
        _container.AddChild(title);

        var sep = new HSeparator();
        _container.AddChild(sep);

        // Connect to state changes
        _gameManager.StateChanged += OnStateChanged;

        // Initial update
        UpdateMeters();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged() => UpdateMeters();

    private void UpdateMeters()
    {
        if (_gameManager?.CurrentState is null || _container is null) return;

        // Clear existing meters (after title and separator)
        while (_container.GetChildCount() > 2)
        {
            var child = _container.GetChild(2);
            _container.RemoveChild(child);
            child.QueueFree();
        }

        var org = _gameManager.CurrentState.Org;
        var directive = _gameManager.CurrentState.CurrentDirective;

        CreateMeterRow(Meter.Delivery, org.Delivery, directive);
        CreateMeterRow(Meter.Morale, org.Morale, directive);
        CreateMeterRow(Meter.Governance, org.Governance, directive);
        CreateMeterRow(Meter.Alignment, org.Alignment, directive);
        CreateMeterRow(Meter.Runway, org.Runway, directive);
    }

    private void CreateMeterRow(Meter meter, int value, BoardDirective? _directive)
    {
        if (_container is null) return;
        _ = _directive; // BoardDirective tracks profit, not meters

        var info = MeterInfo[meter];

        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 2);

        // Name and description
        var nameRow = new HBoxContainer();

        var nameLabel = new Label
        {
            Text = info.Name,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameRow.AddChild(nameLabel);

        var valueLabel = new Label
        {
            Text = value.ToString()
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 12);
        valueLabel.Modulate = GetValueColor(value);
        nameRow.AddChild(valueLabel);

        row.AddChild(nameRow);

        // Progress bar
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 8),
            MaxValue = 100,
            Value = value,
            ShowPercentage = false
        };

        // Style the bar
        var barStyle = new StyleBoxFlat
        {
            BgColor = info.Color * 0.3f
        };
        barStyle.CornerRadiusTopLeft = 3;
        barStyle.CornerRadiusTopRight = 3;
        barStyle.CornerRadiusBottomLeft = 3;
        barStyle.CornerRadiusBottomRight = 3;
        bar.AddThemeStyleboxOverride("background", barStyle);

        var fillStyle = new StyleBoxFlat
        {
            BgColor = info.Color
        };
        fillStyle.CornerRadiusTopLeft = 3;
        fillStyle.CornerRadiusTopRight = 3;
        fillStyle.CornerRadiusBottomLeft = 3;
        fillStyle.CornerRadiusBottomRight = 3;
        bar.AddThemeStyleboxOverride("fill", fillStyle);

        row.AddChild(bar);

        // Description (smaller, dimmed)
        var descLabel = new Label
        {
            Text = info.Desc
        };
        descLabel.AddThemeFontSizeOverride("font_size", 9);
        descLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
        row.AddChild(descLabel);

        _container.AddChild(row);
    }

    private static Color GetValueColor(int value)
    {
        if (value >= 60) return new Color(0.4f, 0.9f, 0.4f);
        if (value >= 40) return new Color(0.9f, 0.9f, 0.4f);
        if (value >= 20) return new Color(0.9f, 0.6f, 0.3f);
        return new Color(0.9f, 0.3f, 0.3f);
    }
}
