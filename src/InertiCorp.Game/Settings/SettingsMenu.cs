using Godot;

namespace InertiCorp.Game.Settings;

/// <summary>
/// Settings menu UI for display configuration.
/// Overlay panel that can be opened from the main game.
/// </summary>
public partial class SettingsMenu : Control
{
    private OptionButton? _windowModeOption;
    private OptionButton? _resolutionOption;
    private HSlider? _uiScaleSlider;
    private Label? _uiScaleLabel;
    private Button? _closeButton;

    [Signal]
    public delegate void ClosedEventHandler();

    public override void _Ready()
    {
        SetupUI();
        LoadCurrentSettings();
    }

    private void SetupUI()
    {
        // Full-screen semi-transparent overlay
        SetAnchorsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        // Dark overlay background that also centers content
        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
            MouseFilter = MouseFilterEnum.Stop
        };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        // Center container to properly center the panel
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        // Settings panel
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(500, 400)
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.14f, 0.18f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderColor = new Color(0.3f, 0.35f, 0.4f),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        centerContainer.AddChild(panel);

        // Main container
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "SETTINGS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        // Separator
        var sep = new HSeparator();
        vbox.AddChild(sep);

        // Settings grid
        var grid = new GridContainer
        {
            Columns = 2
        };
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 12);
        vbox.AddChild(grid);

        // Window Mode
        var windowModeLabel = new Label { Text = "Window Mode:" };
        windowModeLabel.AddThemeFontSizeOverride("font_size", 18);
        grid.AddChild(windowModeLabel);

        _windowModeOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(250, 0)
        };
        _windowModeOption.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.Windowed), (int)WindowMode.Windowed);
        _windowModeOption.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.Fullscreen), (int)WindowMode.Fullscreen);
        _windowModeOption.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.BorderlessFullscreen), (int)WindowMode.BorderlessFullscreen);
        _windowModeOption.ItemSelected += OnWindowModeChanged;
        grid.AddChild(_windowModeOption);

        // Resolution
        var resolutionLabel = new Label { Text = "Resolution:" };
        resolutionLabel.AddThemeFontSizeOverride("font_size", 18);
        grid.AddChild(resolutionLabel);

        _resolutionOption = new OptionButton
        {
            CustomMinimumSize = new Vector2(250, 0)
        };
        foreach (var res in SettingsManager.ResolutionPresets)
        {
            _resolutionOption.AddItem(SettingsManager.GetResolutionLabel(res));
        }
        _resolutionOption.ItemSelected += OnResolutionChanged;
        grid.AddChild(_resolutionOption);

        // UI Scale
        var scaleLabel = new Label { Text = "UI Scale:" };
        scaleLabel.AddThemeFontSizeOverride("font_size", 18);
        grid.AddChild(scaleLabel);

        var scaleContainer = new HBoxContainer();
        scaleContainer.AddThemeConstantOverride("separation", 10);
        grid.AddChild(scaleContainer);

        _uiScaleSlider = new HSlider
        {
            MinValue = 0.75,
            MaxValue = 1.5,
            Step = 0.05,
            CustomMinimumSize = new Vector2(180, 0)
        };
        _uiScaleSlider.ValueChanged += OnUIScaleChanged;
        scaleContainer.AddChild(_uiScaleSlider);

        _uiScaleLabel = new Label { Text = "100%" };
        _uiScaleLabel.CustomMinimumSize = new Vector2(50, 0);
        scaleContainer.AddChild(_uiScaleLabel);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(spacer);

        // Note about resolution
        var noteLabel = new Label
        {
            Text = "Note: Resolution only applies in Windowed mode.",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        noteLabel.AddThemeFontSizeOverride("font_size", 14);
        noteLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        vbox.AddChild(noteLabel);

        // Close button
        _closeButton = new Button
        {
            Text = "Close",
            CustomMinimumSize = new Vector2(120, 40)
        };
        _closeButton.Pressed += OnClosePressed;

        var buttonContainer = new CenterContainer();
        buttonContainer.AddChild(_closeButton);
        vbox.AddChild(buttonContainer);
    }

    private void LoadCurrentSettings()
    {
        var settings = SettingsManager.Instance;
        if (settings == null) return;

        // Window mode
        _windowModeOption?.Select((int)settings.CurrentWindowMode);

        // Resolution
        var resIndex = System.Array.IndexOf(SettingsManager.ResolutionPresets, settings.CurrentResolution);
        if (resIndex >= 0)
        {
            _resolutionOption?.Select(resIndex);
        }
        else
        {
            _resolutionOption?.Select(2); // Default to 1080p
        }

        // UI Scale
        if (_uiScaleSlider != null)
        {
            _uiScaleSlider.Value = settings.UIScale;
        }
        UpdateUIScaleLabel(settings.UIScale);

        // Update resolution option enabled state
        UpdateResolutionEnabled();
    }

    private void UpdateResolutionEnabled()
    {
        if (_resolutionOption == null || _windowModeOption == null) return;

        var isWindowed = _windowModeOption.Selected == (int)WindowMode.Windowed;
        _resolutionOption.Disabled = !isWindowed;
    }

    private void UpdateUIScaleLabel(double scale)
    {
        if (_uiScaleLabel != null)
        {
            _uiScaleLabel.Text = $"{(int)(scale * 100)}%";
        }
    }

    private void OnWindowModeChanged(long index)
    {
        SettingsManager.Instance?.SetWindowMode((WindowMode)index);
        UpdateResolutionEnabled();
    }

    private void OnResolutionChanged(long index)
    {
        if (index >= 0 && index < SettingsManager.ResolutionPresets.Length)
        {
            SettingsManager.Instance?.SetResolution(SettingsManager.ResolutionPresets[index]);
        }
    }

    private void OnUIScaleChanged(double value)
    {
        UpdateUIScaleLabel(value);
        SettingsManager.Instance?.SetUIScale((float)value);
    }

    private void OnClosePressed()
    {
        EmitSignal(SignalName.Closed);
        QueueFree();
    }

    public override void _Input(InputEvent @event)
    {
        // Close on Escape key
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            OnClosePressed();
            GetViewport().SetInputAsHandled();
        }
    }
}
