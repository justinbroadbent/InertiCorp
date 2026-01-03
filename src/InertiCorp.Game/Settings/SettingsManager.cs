using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game.Settings;

/// <summary>
/// Manages game settings persistence and application.
/// Settings are saved to user://settings.cfg
/// </summary>
public partial class SettingsManager : Node
{
    private const string SettingsPath = "user://settings.cfg";
    private const string SectionDisplay = "display";
    private const string SectionGameplay = "gameplay";

    private static SettingsManager? _instance;
    public static SettingsManager Instance => _instance!;

    // Display settings
    public WindowMode CurrentWindowMode { get; private set; } = WindowMode.Windowed;
    public Vector2I CurrentResolution { get; private set; } = new(1920, 1080);
    public float UIScale { get; private set; } = 1.0f;

    // Gameplay settings
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.TheNadella;

    // All resolution presets (16:9 aspect ratio)
    private static readonly Vector2I[] AllResolutionPresets = new[]
    {
        new Vector2I(1280, 720),   // 720p
        new Vector2I(1600, 900),   // 900p
        new Vector2I(1920, 1080),  // 1080p (default)
        new Vector2I(2560, 1440),  // 1440p
        new Vector2I(3840, 2160),  // 4K
    };

    /// <summary>
    /// Gets resolution presets that fit on the current screen.
    /// </summary>
    public static Vector2I[] ResolutionPresets
    {
        get
        {
            var screenSize = DisplayServer.ScreenGetSize();
            // Leave some room for window chrome (title bar, taskbar)
            var maxSize = screenSize - new Vector2I(0, 80);

            var validPresets = new System.Collections.Generic.List<Vector2I>();
            foreach (var preset in AllResolutionPresets)
            {
                if (preset.X <= maxSize.X && preset.Y <= maxSize.Y)
                {
                    validPresets.Add(preset);
                }
            }

            // Always have at least one option
            if (validPresets.Count == 0)
            {
                validPresets.Add(new Vector2I(1280, 720));
            }

            return validPresets.ToArray();
        }
    }

    public override void _EnterTree()
    {
        _instance = this;
    }

    public override void _Ready()
    {
        LoadSettings();
        ApplySettings();
    }

    public override void _Input(InputEvent @event)
    {
        // F10 opens settings from anywhere (emergency access)
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F10)
        {
            OpenSettings();
            GetViewport().SetInputAsHandled();
        }

        // F9 resets to safe defaults (emergency reset)
        if (@event is InputEventKey keyEvent2 && keyEvent2.Pressed && keyEvent2.Keycode == Key.F9)
        {
            ResetToDefaults();
            GetViewport().SetInputAsHandled();
        }
    }

    public void OpenSettings()
    {
        // Don't open if already open
        if (GetTree().Root.HasNode("GameMenu")) return;

        var gameMenu = new GameMenu();
        gameMenu.Name = "GameMenu";

        // Connect new game signal to restart
        gameMenu.NewGameRequested += OnNewGameRequested;

        GetTree().Root.AddChild(gameMenu);
    }

    private void OnNewGameRequested()
    {
        // Reload the main scene to start fresh
        GetTree().ReloadCurrentScene();
    }

    public void ResetToDefaults()
    {
        GD.Print("Resetting display settings to defaults...");
        CurrentWindowMode = WindowMode.Windowed;
        CurrentResolution = new Vector2I(1920, 1080);
        UIScale = 1.0f;
        CurrentDifficulty = Difficulty.TheNadella;
        ApplySettings();
        SaveSettings();
    }

    public void LoadSettings()
    {
        var config = new ConfigFile();
        var error = config.Load(SettingsPath);

        if (error != Error.Ok)
        {
            GD.Print("No settings file found, using defaults");
            return;
        }

        // Load display settings
        CurrentWindowMode = (WindowMode)(int)config.GetValue(SectionDisplay, "window_mode", (int)WindowMode.Windowed);
        var resX = (int)config.GetValue(SectionDisplay, "resolution_x", 1920);
        var resY = (int)config.GetValue(SectionDisplay, "resolution_y", 1080);
        CurrentResolution = new Vector2I(resX, resY);
        UIScale = (float)config.GetValue(SectionDisplay, "ui_scale", 1.0f);

        // Load gameplay settings
        CurrentDifficulty = (Difficulty)(int)config.GetValue(SectionGameplay, "difficulty", (int)Difficulty.TheNadella);
        DifficultySettings.Current = CurrentDifficulty;

        // Validate resolution fits on screen (prevent lockout)
        var screenSize = DisplayServer.ScreenGetSize();
        if (CurrentWindowMode == WindowMode.Windowed)
        {
            if (CurrentResolution.X > screenSize.X || CurrentResolution.Y > screenSize.Y - 80)
            {
                GD.Print($"Saved resolution {CurrentResolution} too large for screen {screenSize}, resetting to 1920x1080");
                CurrentResolution = new Vector2I(1920, 1080);

                // If even 1080p is too big, use 720p
                if (CurrentResolution.X > screenSize.X || CurrentResolution.Y > screenSize.Y - 80)
                {
                    CurrentResolution = new Vector2I(1280, 720);
                }
            }
        }

        GD.Print($"Loaded settings: {CurrentWindowMode}, {CurrentResolution}, Scale: {UIScale}");
    }

    public void SaveSettings()
    {
        var config = new ConfigFile();

        // Save display settings
        config.SetValue(SectionDisplay, "window_mode", (int)CurrentWindowMode);
        config.SetValue(SectionDisplay, "resolution_x", CurrentResolution.X);
        config.SetValue(SectionDisplay, "resolution_y", CurrentResolution.Y);
        config.SetValue(SectionDisplay, "ui_scale", UIScale);

        // Save gameplay settings
        config.SetValue(SectionGameplay, "difficulty", (int)CurrentDifficulty);

        var error = config.Save(SettingsPath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to save settings: {error}");
        }
        else
        {
            GD.Print("Settings saved");
        }
    }

    public void ApplySettings()
    {
        var window = GetWindow();
        if (window == null) return;

        switch (CurrentWindowMode)
        {
            case WindowMode.Fullscreen:
                window.Mode = Window.ModeEnum.Fullscreen;
                break;

            case WindowMode.BorderlessFullscreen:
                window.Mode = Window.ModeEnum.ExclusiveFullscreen;
                break;

            case WindowMode.Windowed:
            default:
                window.Mode = Window.ModeEnum.Windowed;
                window.Unresizable = true;
                window.Size = CurrentResolution;
                // Center the window
                var screenSize = DisplayServer.ScreenGetSize();
                window.Position = (screenSize - CurrentResolution) / 2;
                break;
        }

        // Apply UI scale via the viewport stretch
        GetTree().Root.ContentScaleFactor = UIScale;

        GD.Print($"Applied settings: {CurrentWindowMode}, {CurrentResolution}");
    }

    public void SetWindowMode(WindowMode mode)
    {
        CurrentWindowMode = mode;
        ApplySettings();
        SaveSettings();
    }

    public void SetResolution(Vector2I resolution)
    {
        CurrentResolution = resolution;
        if (CurrentWindowMode == WindowMode.Windowed)
        {
            ApplySettings();
        }
        SaveSettings();
    }

    public void SetUIScale(float scale)
    {
        UIScale = Mathf.Clamp(scale, 0.75f, 1.5f);
        ApplySettings();
        SaveSettings();
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        CurrentDifficulty = difficulty;
        DifficultySettings.Current = difficulty;
        SaveSettings();
        GD.Print($"Difficulty set to: {DifficultySettings.CurrentSettings.Name}");
    }

    public static string GetResolutionLabel(Vector2I resolution)
    {
        return resolution switch
        {
            { X: 1280, Y: 720 } => "1280 x 720 (720p)",
            { X: 1600, Y: 900 } => "1600 x 900",
            { X: 1920, Y: 1080 } => "1920 x 1080 (1080p)",
            { X: 2560, Y: 1440 } => "2560 x 1440 (1440p)",
            { X: 3840, Y: 2160 } => "3840 x 2160 (4K)",
            _ => $"{resolution.X} x {resolution.Y}"
        };
    }

    public static string GetWindowModeLabel(WindowMode mode)
    {
        return mode switch
        {
            WindowMode.Windowed => "Windowed",
            WindowMode.Fullscreen => "Fullscreen",
            WindowMode.BorderlessFullscreen => "Borderless Fullscreen",
            _ => mode.ToString()
        };
    }
}

public enum WindowMode
{
    Windowed = 0,
    Fullscreen = 1,
    BorderlessFullscreen = 2
}
