using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game.Settings;

/// <summary>
/// Main game menu with Resume, New Game, Settings, Help, and Exit options.
/// Opens with Escape or the gear button.
/// </summary>
public partial class GameMenu : Control
{
    private Control? _mainPanel;
    private Control? _settingsPanel;
    private Control? _helpPanel;

    [Signal]
    public delegate void ResumeGameEventHandler();

    [Signal]
    public delegate void NewGameRequestedEventHandler();

    public override void _Ready()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Full-screen overlay
        SetAnchorsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        // Dark overlay background
        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.8f),
            MouseFilter = MouseFilterEnum.Stop
        };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        // Center container
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        // Main menu panel
        _mainPanel = CreateMainMenuPanel();
        centerContainer.AddChild(_mainPanel);

        // Settings panel (initially hidden)
        _settingsPanel = CreateSettingsPanel();
        _settingsPanel.Visible = false;
        centerContainer.AddChild(_settingsPanel);

        // Help panel (initially hidden)
        _helpPanel = CreateHelpPanel();
        _helpPanel.Visible = false;
        centerContainer.AddChild(_helpPanel);
    }

    private Control CreateMainMenuPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(400, 450)
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        // Title
        var title = new Label
        {
            Text = "INERTICORP",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 32);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        var subtitle = new Label
        {
            Text = "CEO Survival Simulator",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        vbox.AddChild(subtitle);

        // Separator
        vbox.AddChild(new HSeparator());

        // Spacer
        var spacer1 = new Control { CustomMinimumSize = new Vector2(0, 10) };
        vbox.AddChild(spacer1);

        // Menu buttons
        var resumeBtn = CreateMenuButton("Resume Game", "Return to the action");
        resumeBtn.Pressed += OnResumePressed;
        vbox.AddChild(resumeBtn);

        var newGameBtn = CreateMenuButton("New Game", "Start fresh with a new company");
        newGameBtn.Pressed += OnNewGamePressed;
        vbox.AddChild(newGameBtn);

        var settingsBtn = CreateMenuButton("Settings", "Display and UI options");
        settingsBtn.Pressed += OnSettingsPressed;
        vbox.AddChild(settingsBtn);

        var helpBtn = CreateMenuButton("How to Play", "Learn the ropes of corporate survival");
        helpBtn.Pressed += OnHelpPressed;
        vbox.AddChild(helpBtn);

        // Spacer
        var spacer2 = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(spacer2);

        // Exit button (styled differently)
        var exitBtn = CreateMenuButton("Exit to Desktop", "Abandon your corner office", true);
        exitBtn.Pressed += OnExitPressed;
        vbox.AddChild(exitBtn);

        // Version info
        var version = new Label
        {
            Text = "v0.1.0 - Press Escape to close",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        version.AddThemeFontSizeOverride("font_size", 12);
        version.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
        vbox.AddChild(version);

        return panel;
    }

    private Control CreateSettingsPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(550, 650)
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.SetHorizontalScrollMode(ScrollContainer.ScrollMode.Disabled);
        scroll.SetVerticalScrollMode(ScrollContainer.ScrollMode.Auto);
        panel.AddChild(scroll);

        var vbox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        vbox.AddThemeConstantOverride("separation", 16);
        scroll.AddChild(vbox);

        // Header with back button
        var header = new HBoxContainer();
        vbox.AddChild(header);

        var backBtn = new Button { Text = "< Back" };
        backBtn.Pressed += () => ShowPanel(_mainPanel!);
        header.AddChild(backBtn);

        var title = new Label
        {
            Text = "SETTINGS",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddChild(title);

        // Spacer for symmetry
        var spacer = new Control { CustomMinimumSize = new Vector2(60, 0) };
        header.AddChild(spacer);

        vbox.AddChild(new HSeparator());

        // Settings grid
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 16);
        vbox.AddChild(grid);

        // Window Mode
        AddSettingRow(grid, "Window Mode:", CreateWindowModeOption());

        // Resolution
        AddSettingRow(grid, "Resolution:", CreateResolutionOption());

        // UI Scale
        AddSettingRow(grid, "UI Scale:", CreateUIScaleControl());

        vbox.AddChild(new HSeparator());

        // Gameplay section header
        var gameplayHeader = new Label
        {
            Text = "GAMEPLAY",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        gameplayHeader.AddThemeFontSizeOverride("font_size", 18);
        gameplayHeader.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(gameplayHeader);

        // Difficulty selection
        vbox.AddChild(CreateDifficultyOption());

        // Spacer
        var spacer2 = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(spacer2);

        // Note
        var note = new Label
        {
            Text = "Difficulty takes effect on New Game",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        note.AddThemeFontSizeOverride("font_size", 13);
        note.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        vbox.AddChild(note);

        return panel;
    }

    private Control CreateHelpPanel()
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(600, 500)
        };
        panel.AddThemeStyleboxOverride("panel", CreatePanelStyle());

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        // Header with back button
        var header = new HBoxContainer();
        vbox.AddChild(header);

        var backBtn = new Button { Text = "< Back" };
        backBtn.Pressed += () => ShowPanel(_mainPanel!);
        header.AddChild(backBtn);

        var title = new Label
        {
            Text = "HOW TO PLAY",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        header.AddChild(title);

        var spacer = new Control { CustomMinimumSize = new Vector2(60, 0) };
        header.AddChild(spacer);

        vbox.AddChild(new HSeparator());

        // Help content in a scroll container
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        vbox.AddChild(scroll);

        var helpText = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(550, 0),
            Text = GetHelpText()
        };
        helpText.AddThemeFontSizeOverride("normal_font_size", 15);
        scroll.AddChild(helpText);

        return panel;
    }

    private static string GetHelpText()
    {
        return @"[b]OBJECTIVE[/b]
Accumulate $150M in bonuses to retire comfortably, or survive as long as you can before the board ousts you.

[b]GAME FLOW[/b]
Each quarter has 4 phases:
• [color=#6cb4d4]Board Demand[/color] - The board sets a profit target
• [color=#6cb4d4]Projects[/color] - Play up to 3 project cards
• [color=#6cb4d4]Situation[/color] - Handle crises and situations requiring your decision
• [color=#6cb4d4]Resolution[/color] - Receive your quarterly bonus (or get ousted)

[b]WINNING & LOSING[/b]
• [color=#4dff88]Victory[/color]: Accumulate $150M in bonuses, then choose to retire
• [color=#ff6b6b]Defeat[/color]: Get ousted by the board (low favorability, evil scandals, or meter collapse)

[b]PROJECTS[/b]
Play project cards to drive profits:
• Each card has potential Good, Expected, or Bad outcomes
• Cards cost Political Capital (0 for 1st, 1 for 2nd, 3 for 3rd)
• [color=#b388ff]Revenue cards[/color] generate profit but have diminishing returns if spammed
• Balance card types - playing only revenue cards hurts organizational health

[b]METERS[/b]
Keep these balanced (0-100):
• [color=#4db8ff]Delivery[/color] - Project velocity and execution
• [color=#ffdb4d]Morale[/color] - Team health and engagement
• [color=#ff6b6b]Governance[/color] - Compliance and process health
• [color=#9d6bff]Alignment[/color] - Strategic coherence
• [color=#4dff88]Runway[/color] - Financial reserves

[b]POLITICAL CAPITAL (PC)[/b]
Your currency for influence:
• Earned through restraint (playing fewer cards each quarter)
• Spent to play cards, boost outcomes, or handle crises
• High meters generate passive PC each quarter

[b]QUARTERLY BONUS[/b]
Your bonus depends on performance:
• Meeting board directives
• Profit growth vs previous quarter
• High organizational health (meters)
• Low evil score

[b]TIPS[/b]
• Don't play all 3 cards - restraint earns PC
• Watch card affinities - high meters boost outcomes
• Mix card types - revenue-only strategies have penalties
• The board scrutinizes high-evil CEOs more harshly
• Check your Reports for performance trends";
    }

    private void AddSettingRow(GridContainer grid, string label, Control control)
    {
        var labelNode = new Label { Text = label };
        labelNode.AddThemeFontSizeOverride("font_size", 16);
        grid.AddChild(labelNode);
        grid.AddChild(control);
    }

    private OptionButton CreateWindowModeOption()
    {
        var option = new OptionButton { CustomMinimumSize = new Vector2(220, 0) };
        option.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.Windowed), (int)WindowMode.Windowed);
        option.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.Fullscreen), (int)WindowMode.Fullscreen);
        option.AddItem(SettingsManager.GetWindowModeLabel(WindowMode.BorderlessFullscreen), (int)WindowMode.BorderlessFullscreen);

        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            option.Select((int)settings.CurrentWindowMode);
        }

        option.ItemSelected += (index) =>
        {
            SettingsManager.Instance?.SetWindowMode((WindowMode)index);
            // Refresh resolution options when mode changes
            RefreshSettingsPanel();
        };

        return option;
    }

    private OptionButton CreateResolutionOption()
    {
        var option = new OptionButton
        {
            CustomMinimumSize = new Vector2(220, 0),
            Name = "ResolutionOption"
        };

        var presets = SettingsManager.ResolutionPresets;
        foreach (var res in presets)
        {
            option.AddItem(SettingsManager.GetResolutionLabel(res));
        }

        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            var resIndex = System.Array.IndexOf(presets, settings.CurrentResolution);
            if (resIndex >= 0)
            {
                option.Select(resIndex);
            }

            // Disable if not windowed
            option.Disabled = settings.CurrentWindowMode != WindowMode.Windowed;
        }

        option.ItemSelected += (index) =>
        {
            var presets = SettingsManager.ResolutionPresets;
            if (index >= 0 && index < presets.Length)
            {
                SettingsManager.Instance?.SetResolution(presets[index]);
            }
        };

        return option;
    }

    private Control CreateUIScaleControl()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);

        var slider = new HSlider
        {
            MinValue = 0.75,
            MaxValue = 1.5,
            Step = 0.05,
            CustomMinimumSize = new Vector2(150, 0),
            Name = "UIScaleSlider"
        };

        var label = new Label
        {
            CustomMinimumSize = new Vector2(50, 0),
            Name = "UIScaleLabel"
        };

        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            slider.Value = settings.UIScale;
            label.Text = $"{(int)(settings.UIScale * 100)}%";
        }

        slider.ValueChanged += (value) =>
        {
            label.Text = $"{(int)(value * 100)}%";
            SettingsManager.Instance?.SetUIScale((float)value);
        };

        hbox.AddChild(slider);
        hbox.AddChild(label);

        return hbox;
    }

    private Control CreateDifficultyOption()
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 8);

        // Button group for radio-button behavior
        var buttonGroup = new ButtonGroup();

        var settings = SettingsManager.Instance;
        var currentDifficulty = settings?.CurrentDifficulty ?? Difficulty.TheNadella;

        foreach (var difficulty in DifficultySettings.AllDifficulties)
        {
            var diffSettings = DifficultySettings.ForDifficulty(difficulty);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 12);

            // Radio button
            var radio = new CheckBox
            {
                Text = diffSettings.Name,
                ButtonGroup = buttonGroup,
                ButtonPressed = difficulty == currentDifficulty,
                CustomMinimumSize = new Vector2(140, 0)
            };
            radio.AddThemeFontSizeOverride("font_size", 16);

            // Color based on difficulty
            var color = difficulty switch
            {
                Difficulty.TheWelch => new Color(0.4f, 0.8f, 0.4f),  // Green for easy
                Difficulty.TheNadella => new Color(0.9f, 0.85f, 0.7f), // Gold for regular
                Difficulty.TheIcahn => new Color(0.9f, 0.4f, 0.4f),  // Red for hard
                _ => new Color(1, 1, 1)
            };
            radio.AddThemeColorOverride("font_color", color);
            radio.AddThemeColorOverride("font_pressed_color", color);

            var capturedDifficulty = difficulty;
            radio.Toggled += (pressed) =>
            {
                if (pressed)
                {
                    SettingsManager.Instance?.SetDifficulty(capturedDifficulty);
                    RefreshDifficultyDescription();
                }
            };

            hbox.AddChild(radio);

            // Short description
            var desc = new Label
            {
                Text = diffSettings.Description.Replace(diffSettings.Name + " - ", ""),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            desc.AddThemeFontSizeOverride("font_size", 14);
            desc.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
            hbox.AddChild(desc);

            container.AddChild(hbox);
        }

        // Flavor text for selected difficulty
        var flavorLabel = new Label
        {
            Name = "DifficultyFlavorText",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(480, 60),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        flavorLabel.AddThemeFontSizeOverride("font_size", 13);
        flavorLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.55f, 0.6f));

        var currentSettings = DifficultySettings.ForDifficulty(currentDifficulty);
        flavorLabel.Text = currentSettings.FlavorText;

        container.AddChild(flavorLabel);

        return container;
    }

    private void RefreshDifficultyDescription()
    {
        var flavorLabel = _settingsPanel?.FindChild("DifficultyFlavorText", true, false) as Label;
        if (flavorLabel != null)
        {
            var settings = SettingsManager.Instance;
            var currentDifficulty = settings?.CurrentDifficulty ?? Difficulty.TheNadella;
            var diffSettings = DifficultySettings.ForDifficulty(currentDifficulty);
            flavorLabel.Text = diffSettings.FlavorText;
        }
    }

    private void RefreshSettingsPanel()
    {
        // Recreate settings panel to refresh resolution options
        if (_settingsPanel != null)
        {
            var parent = _settingsPanel.GetParent();
            var wasVisible = _settingsPanel.Visible;
            _settingsPanel.QueueFree();
            _settingsPanel = CreateSettingsPanel();
            _settingsPanel.Visible = wasVisible;
            parent?.AddChild(_settingsPanel);
        }
    }

    private Button CreateMenuButton(string text, string tooltip, bool danger = false)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(300, 45)
        };
        btn.AddThemeFontSizeOverride("font_size", 18);

        if (danger)
        {
            btn.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 0.5f));
            btn.AddThemeColorOverride("font_hover_color", new Color(1.0f, 0.6f, 0.6f));
        }

        return btn;
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.14f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderColor = new Color(0.25f, 0.28f, 0.32f),
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 25,
            ContentMarginRight = 25,
            ContentMarginTop = 20,
            ContentMarginBottom = 20
        };
    }

    private void ShowPanel(Control panel)
    {
        _mainPanel!.Visible = panel == _mainPanel;
        _settingsPanel!.Visible = panel == _settingsPanel;
        _helpPanel!.Visible = panel == _helpPanel;
    }

    private void OnResumePressed()
    {
        EmitSignal(SignalName.ResumeGame);
        QueueFree();
    }

    private void OnNewGamePressed()
    {
        EmitSignal(SignalName.NewGameRequested);
        QueueFree();
    }

    private void OnSettingsPressed()
    {
        ShowPanel(_settingsPanel!);
    }

    private void OnHelpPressed()
    {
        ShowPanel(_helpPanel!);
    }

    private void OnExitPressed()
    {
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            // If in a sub-panel, go back to main
            if (_settingsPanel!.Visible || _helpPanel!.Visible)
            {
                ShowPanel(_mainPanel!);
            }
            else
            {
                // Close menu
                OnResumePressed();
            }
            GetViewport().SetInputAsHandled();
        }
    }
}
