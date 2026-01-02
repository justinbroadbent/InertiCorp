using Godot;

namespace InertiCorp.Game;

/// <summary>
/// Corporate-themed loading screen shown while LLM initializes.
/// Uses CanvasLayer to ensure it renders above everything.
/// </summary>
public partial class LoadingScreen : CanvasLayer
{
    private Label? _statusLabel;
    private Label? _subStatusLabel;
    private ProgressBar? _progressBar;
    private Control? _root;
    private float _messageTimer;
    private float _progressValue;
    private int _messageIndex;
    private bool _isComplete;
    private float _fadeTimer;
    private bool _signalEmitted;

    private static readonly string[] LoadingMessages =
    [
        "Initializing corporate synergy matrix...",
        "Calibrating stakeholder alignment protocols...",
        "Loading quarterly projection algorithms...",
        "Establishing executive decision framework...",
        "Synchronizing buzzword generation engine...",
        "Optimizing paradigm shift coefficients...",
        "Bootstrapping innovation pipeline...",
        "Configuring passive-aggressive email templates...",
        "Loading middle management simulation...",
        "Calculating optimal blame distribution...",
        "Initializing plausible deniability module...",
        "Warming up the corporate spin machine...",
        "Deploying strategic ambiguity protocols...",
        "Loading board meeting survival tactics...",
        "Establishing golden parachute parameters...",
        "Calibrating organizational restructuring engine...",
        "Syncing with quarterly earnings expectations...",
        "Preparing executive summary generator...",
        "Loading crisis management scenarios...",
        "Finalizing corporate doublespeak dictionary..."
    ];

    private static readonly string[] SubMessages =
    [
        "Please wait while we leverage your patience...",
        "Your time is valuable. Ours is billable.",
        "Aligning with best practices...",
        "This is a value-add experience.",
        "Thinking outside the box...",
        "Moving the needle...",
        "Boiling the ocean...",
        "Drinking from the firehose...",
        "Peeling back the onion...",
        "Taking this offline..."
    ];

    [Signal]
    public delegate void LoadingCompleteEventHandler();

    public override void _Ready()
    {
        GD.Print("[LoadingScreen] _Ready called - starting setup");

        try
        {
            // CanvasLayer renders on layer 100 (above everything)
            Layer = 100;

            // Setup UI immediately (don't defer - LLM loading may block)
            SetupUI();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[LoadingScreen] Exception in _Ready: {ex.Message}");
            GD.PrintErr(ex.StackTrace);
        }
    }

    private void SetupUI()
    {
        // Get viewport size from multiple sources
        var viewportSize = Vector2.Zero;

        // Try 1: DisplayServer window size (most reliable)
        var windowSize = DisplayServer.WindowGetSize();
        if (windowSize.X > 0 && windowSize.Y > 0)
        {
            viewportSize = windowSize;
            GD.Print($"[LoadingScreen] Using DisplayServer window size: {viewportSize}");
        }

        // Try 2: Viewport visible rect
        if (viewportSize == Vector2.Zero)
        {
            var viewport = GetViewport();
            if (viewport != null)
            {
                viewportSize = viewport.GetVisibleRect().Size;
                GD.Print($"[LoadingScreen] Using viewport rect: {viewportSize}");
            }
        }

        // Try 3: Root window size
        if (viewportSize == Vector2.Zero || viewportSize.X <= 0)
        {
            var root = GetTree()?.Root;
            if (root != null)
            {
                viewportSize = root.Size;
                GD.Print($"[LoadingScreen] Using root window size: {viewportSize}");
            }
        }

        // Fallback to common resolution
        if (viewportSize == Vector2.Zero || viewportSize.X <= 0)
        {
            viewportSize = new Vector2(1920, 1080);
            GD.Print("[LoadingScreen] Using fallback size 1920x1080");
        }

        GD.Print($"[LoadingScreen] FINAL viewport size: {viewportSize}");

        // Root control that fills the screen
        _root = new Control
        {
            Position = Vector2.Zero,
            Size = viewportSize,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddChild(_root);

        // Dark background
        var background = new ColorRect
        {
            Name = "Background",
            Color = new Color(0.08f, 0.09f, 0.12f), // Slightly lighter to distinguish from black
            Position = Vector2.Zero,
            Size = viewportSize
        };
        _root.AddChild(background);

        GD.Print($"[LoadingScreen] Background created: pos={background.Position}, size={background.Size}, color={background.Color}");

        // Center container for content
        var center = new CenterContainer
        {
            Name = "CenterContainer",
            Position = Vector2.Zero,
            Size = viewportSize
        };
        _root.AddChild(center);

        GD.Print("[LoadingScreen] CenterContainer created");

        // Main vertical container
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        center.AddChild(vbox);

        // Company logo/title
        var title = new Label
        {
            Text = "INERTICORP",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        vbox.AddChild(title);

        var subtitle = new Label
        {
            Text = "Enterprise Solutions for Tomorrow's Problems",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        subtitle.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        vbox.AddChild(subtitle);

        // Spacer
        var spacer1 = new Control { CustomMinimumSize = new Vector2(0, 40) };
        vbox.AddChild(spacer1);

        // Status message
        _statusLabel = new Label
        {
            Text = LoadingMessages[0],
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(500, 0)
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 18);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 0.9f));
        vbox.AddChild(_statusLabel);

        // Progress bar
        _progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(400, 8),
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = 100,
            Value = 0
        };

        // Style the progress bar
        var bgStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.16f, 0.2f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        var fillStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.6f, 0.9f),
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4
        };
        _progressBar.AddThemeStyleboxOverride("background", bgStyle);
        _progressBar.AddThemeStyleboxOverride("fill", fillStyle);

        var progressContainer = new CenterContainer();
        progressContainer.AddChild(_progressBar);
        vbox.AddChild(progressContainer);

        // Sub-status message
        _subStatusLabel = new Label
        {
            Text = SubMessages[0],
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _subStatusLabel.AddThemeFontSizeOverride("font_size", 14);
        _subStatusLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.45f));
        vbox.AddChild(_subStatusLabel);

        // Version/copyright at bottom - position explicitly
        var copyright = new Label
        {
            Text = "Copyright 2025 InertiCorp Holdings LLC. All rights reserved.\n\"Moving Forward by Standing Still\"",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(0, viewportSize.Y - 60),
            Size = new Vector2(viewportSize.X, 50)
        };
        copyright.AddThemeFontSizeOverride("font_size", 12);
        copyright.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.35f));
        _root.AddChild(copyright);

        GD.Print($"[LoadingScreen] UI created - root size: {_root.Size}, bg size: {background.Size}, children: {_root.GetChildCount()}");
    }

    public override void _Process(double delta)
    {
        if (_isComplete)
        {
            // Fade out
            _fadeTimer += (float)delta;
            var alpha = 1 - (_fadeTimer / 0.5f);

            if (_root != null)
            {
                _root.Modulate = new Color(1, 1, 1, Mathf.Max(0, alpha));
            }

            if (_fadeTimer >= 0.5f && !_signalEmitted)
            {
                _signalEmitted = true;
                GD.Print("[LoadingScreen] Fade complete, emitting LoadingComplete signal");
                EmitSignal(SignalName.LoadingComplete);
                CallDeferred(nameof(QueueFree));
            }
            return;
        }

        // Update message every 1.5 seconds
        _messageTimer += (float)delta;
        if (_messageTimer >= 1.5f)
        {
            _messageTimer = 0;
            _messageIndex = (_messageIndex + 1) % LoadingMessages.Length;

            if (_statusLabel != null)
            {
                _statusLabel.Text = LoadingMessages[_messageIndex];
            }

            // Update sub-message less frequently
            if (_messageIndex % 3 == 0 && _subStatusLabel != null)
            {
                var subIndex = (_messageIndex / 3) % SubMessages.Length;
                _subStatusLabel.Text = SubMessages[subIndex];
            }
        }

        // Slowly increment progress (fake progress while loading)
        if (_progressBar != null && _progressValue < 85)
        {
            _progressValue += (float)delta * 8; // ~10 seconds to reach 85%
            _progressBar.Value = _progressValue;
        }
    }

    /// <summary>
    /// Call this when LLM is ready to trigger fade out.
    /// </summary>
    public void OnLoadingComplete()
    {
        GD.Print("[LoadingScreen] OnLoadingComplete called");
        _isComplete = true;

        // Jump progress to 100%
        if (_progressBar != null)
        {
            _progressBar.Value = 100;
        }

        if (_statusLabel != null)
        {
            _statusLabel.Text = "Systems online. Welcome, CEO.";
        }

        if (_subStatusLabel != null)
        {
            _subStatusLabel.Text = "Your corner office awaits...";
        }
    }

    /// <summary>
    /// Call this if loading fails.
    /// </summary>
    public void OnLoadingFailed(string error)
    {
        GD.Print($"[LoadingScreen] OnLoadingFailed: {error}");

        if (_statusLabel != null)
        {
            _statusLabel.Text = "AI systems offline - using templates";
            _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.7f, 0.5f));
        }

        if (_subStatusLabel != null)
        {
            _subStatusLabel.Text = error;
        }

        // Still complete after a delay
        GD.Print("[LoadingScreen] Starting 2s timer before complete");
        GetTree().CreateTimer(2.0).Timeout += () => OnLoadingComplete();
    }
}
