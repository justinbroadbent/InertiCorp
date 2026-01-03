using Godot;
using InertiCorp.Core.Llm;
using InertiCorp.Game.Audio;

namespace InertiCorp.Game.Settings;

/// <summary>
/// First-run dialog that prompts the user to download an AI model for dynamic emails.
/// Appears on first game launch when no model is configured.
/// </summary>
public partial class FirstRunModelDialog : Control
{
    private ModelManager? _modelManager;
    private Label? _statusLabel;
    private Label? _hardwareLabel;
    private Label? _pleaseHoldLabel;
    private ProgressBar? _progressBar;
    private Button? _liteButton;      // TinyLlama - fast, CPU-friendly
    private Button? _standardButton;  // Phi-3 - higher quality, GPU recommended
    private Button? _skipButton;
    private Button? _cancelButton;
    private Button? _continueButton;
    private Button? _muteButton;
    private Label? _modelNameLabel;
    private Label? _modelDescLabel;
    private Label? _modelSizeLabel;
    private bool _downloadComplete;
    private CancellationTokenSource? _downloadCts;
    private string _selectedModelId = ModelCatalog.DefaultCpuModelId;  // Default to CPU-friendly
    private bool _gpuDetected;

    /// <summary>
    /// Whether the first-run dialog is currently visible.
    /// Used to pause game events during setup.
    /// </summary>
    public static bool IsActive { get; private set; }

    [Signal]
    public delegate void SetupCompleteEventHandler(bool modelDownloaded);

    public override void _Ready()
    {
        IsActive = true;
        _modelManager = new ModelManager();
        _modelManager.ModelStatusChanged += OnModelStatusChanged;

        // Initialize LLM diagnostics to detect GPU
        LlmDiagnostics.Initialize();
        _gpuDetected = LlmDiagnostics.GpuDetected;

        // Select recommended model based on hardware
        _selectedModelId = _gpuDetected ? ModelCatalog.DefaultGpuModelId : ModelCatalog.DefaultCpuModelId;

        SetupUI();
    }

    public override void _ExitTree()
    {
        IsActive = false;
        _modelManager?.Dispose();
    }

    private void SetupUI()
    {
        // Full-screen overlay
        SetAnchorsPreset(LayoutPreset.FullRect);
        Size = GetViewportRect().Size;

        // Dark overlay background
        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.9f),
            MouseFilter = MouseFilterEnum.Stop
        };
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(overlay);

        // Center container
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);

        // Main panel
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(550, 450)
        };
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.14f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderColor = new Color(0.3f, 0.35f, 0.4f),
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 25,
            ContentMarginBottom = 25
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
            Text = "WELCOME TO INERTICORP",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 26);
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

        // Logo - centered
        var logoTexture = GD.Load<Texture2D>("res://logo.png");
        if (logoTexture != null)
        {
            var logoContainer = new CenterContainer();
            var logo = new TextureRect
            {
                Texture = logoTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspect,
                CustomMinimumSize = new Vector2(280, 70)
            };
            logoContainer.AddChild(logo);
            vbox.AddChild(logoContainer);
        }

        vbox.AddChild(new HSeparator());

        // Hardware detection status
        var hardwareStatus = _gpuDetected
            ? $"NVIDIA GPU detected - Standard Mode recommended"
            : "No GPU detected - Lite Mode recommended for smooth gameplay";
        var hardwareColor = _gpuDetected
            ? new Color(0.4f, 1.0f, 0.6f)   // Green for GPU
            : new Color(1.0f, 0.8f, 0.4f);  // Orange for CPU-only

        _hardwareLabel = new Label
        {
            Text = hardwareStatus,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _hardwareLabel.AddThemeFontSizeOverride("font_size", 14);
        _hardwareLabel.AddThemeColorOverride("font_color", hardwareColor);
        vbox.AddChild(_hardwareLabel);

        // Explanation - corporate CEO pitch
        var explanationLabel = new Label
        {
            Text = "The InertiCorp Cognitive Synergy Engine™ generates dynamic, " +
                   "context-aware corporate communications in real-time.\n\n" +
                   "Choose your AI configuration below. Models run 100% locally - " +
                   "your strategic insights remain confidential.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        explanationLabel.AddThemeFontSizeOverride("font_size", 14);
        explanationLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        vbox.AddChild(explanationLabel);

        // Model info box
        var modelInfo = CreateModelInfoBox();
        vbox.AddChild(modelInfo);

        // Status label
        _statusLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_statusLabel);

        // "Please hold" label (initially hidden)
        _pleaseHoldLabel = new Label
        {
            Text = "♪ Please hold... Your download is important to us. ♪",
            HorizontalAlignment = HorizontalAlignment.Center,
            Visible = false
        };
        _pleaseHoldLabel.AddThemeFontSizeOverride("font_size", 13);
        _pleaseHoldLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.7f, 0.8f));
        vbox.AddChild(_pleaseHoldLabel);

        // Progress bar with mute button (initially hidden)
        var progressRow = new HBoxContainer();
        progressRow.AddThemeConstantOverride("separation", 10);
        var progressWrapper = new CenterContainer();
        progressWrapper.AddChild(progressRow);
        vbox.AddChild(progressWrapper);

        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            CustomMinimumSize = new Vector2(350, 20),
            Visible = false
        };
        progressRow.AddChild(_progressBar);

        _muteButton = new Button
        {
            Text = "🔊",
            CustomMinimumSize = new Vector2(40, 30),
            Visible = false,
            TooltipText = "Mute/unmute music"
        };
        _muteButton.AddThemeFontSizeOverride("font_size", 16);
        _muteButton.Pressed += OnMutePressed;
        progressRow.AddChild(_muteButton);

        // Spacer
        var spacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        vbox.AddChild(spacer);

        // Model selection buttons
        var modelButtonContainer = new HBoxContainer();
        modelButtonContainer.AddThemeConstantOverride("separation", 12);
        var modelButtonWrapper = new CenterContainer();
        modelButtonWrapper.AddChild(modelButtonContainer);
        vbox.AddChild(modelButtonWrapper);

        var liteModel = ModelCatalog.CpuDefault;
        var standardModel = ModelCatalog.GpuDefault;

        _liteButton = new Button
        {
            Text = $"Fast ({FormatSize(liteModel.SizeBytes)})",
            CustomMinimumSize = new Vector2(150, 45),
            TooltipText = $"{liteModel.Name}: {liteModel.Description}"
        };
        _liteButton.AddThemeFontSizeOverride("font_size", 14);
        _liteButton.Pressed += () => OnModelSelected(liteModel.Id);
        modelButtonContainer.AddChild(_liteButton);

        _standardButton = new Button
        {
            Text = $"Balanced ({FormatSize(standardModel.SizeBytes)})",
            CustomMinimumSize = new Vector2(160, 45),
            TooltipText = $"{standardModel.Name}: {standardModel.Description}"
        };
        _standardButton.AddThemeFontSizeOverride("font_size", 14);
        _standardButton.Pressed += () => OnModelSelected(standardModel.Id);
        modelButtonContainer.AddChild(_standardButton);

        // Highlight the recommended button based on GPU availability
        var recommendedButton = _gpuDetected ? _standardButton : _liteButton;
        recommendedButton.Text += " ✓";
        recommendedButton.AddThemeColorOverride("font_color", new Color(0.4f, 1.0f, 0.6f));

        // Other buttons
        var buttonContainer = new HBoxContainer();
        buttonContainer.AddThemeConstantOverride("separation", 16);
        var buttonWrapper = new CenterContainer();
        buttonWrapper.AddChild(buttonContainer);
        vbox.AddChild(buttonWrapper);

        _skipButton = new Button
        {
            Text = "No AI (Classic Mode)",
            CustomMinimumSize = new Vector2(150, 40),
            TooltipText = "Use pre-written corporate templates (no download required)"
        };
        _skipButton.AddThemeFontSizeOverride("font_size", 13);
        _skipButton.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        _skipButton.Pressed += OnSkipPressed;
        buttonContainer.AddChild(_skipButton);

        _cancelButton = new Button
        {
            Text = "Cancel Download",
            CustomMinimumSize = new Vector2(140, 40),
            Visible = false
        };
        _cancelButton.AddThemeFontSizeOverride("font_size", 13);
        _cancelButton.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 0.5f));
        _cancelButton.Pressed += OnCancelPressed;
        buttonContainer.AddChild(_cancelButton);

        _continueButton = new Button
        {
            Text = "Continue to Game",
            CustomMinimumSize = new Vector2(160, 45),
            Visible = false
        };
        _continueButton.AddThemeFontSizeOverride("font_size", 16);
        _continueButton.AddThemeColorOverride("font_color", new Color(0.4f, 1.0f, 0.4f));
        _continueButton.Pressed += OnContinuePressed;
        buttonContainer.AddChild(_continueButton);
    }

    private void OnModelSelected(string modelId)
    {
        _selectedModelId = modelId;
        StartDownload();
    }

    private Control CreateModelInfoBox()
    {
        var modelInfo = ModelCatalog.GetRecommendedDefault(_gpuDetected);

        var container = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.16f, 0.2f),
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = new Color(0.25f, 0.28f, 0.32f),
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        container.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        container.AddChild(hbox);

        // Left side: model icon placeholder
        var iconLabel = new Label
        {
            Text = "[AI]",
            CustomMinimumSize = new Vector2(50, 50),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconLabel.AddThemeFontSizeOverride("font_size", 18);
        iconLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f));
        hbox.AddChild(iconLabel);

        // Model details - stored for later updates
        var infoVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        infoVbox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(infoVbox);

        _modelNameLabel = new Label { Text = modelInfo.Name };
        _modelNameLabel.AddThemeFontSizeOverride("font_size", 16);
        _modelNameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        infoVbox.AddChild(_modelNameLabel);

        _modelDescLabel = new Label { Text = modelInfo.Description };
        _modelDescLabel.AddThemeFontSizeOverride("font_size", 13);
        _modelDescLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        infoVbox.AddChild(_modelDescLabel);

        _modelSizeLabel = new Label { Text = $"Size: {FormatSize(modelInfo.SizeBytes)}" };
        _modelSizeLabel.AddThemeFontSizeOverride("font_size", 12);
        _modelSizeLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        infoVbox.AddChild(_modelSizeLabel);

        return container;
    }

    private void UpdateModelInfoDisplay()
    {
        var modelInfo = ModelCatalog.GetById(_selectedModelId);
        if (modelInfo == null) return;

        if (_modelNameLabel != null) _modelNameLabel.Text = modelInfo.Name;
        if (_modelDescLabel != null) _modelDescLabel.Text = modelInfo.Description;
        if (_modelSizeLabel != null) _modelSizeLabel.Text = $"Size: {FormatSize(modelInfo.SizeBytes)}";
    }

    private void OnModelStatusChanged(string modelId)
    {
        CallDeferred(nameof(UpdateUI));
    }

    private void UpdateUI()
    {
        if (_modelManager == null) return;

        var status = _modelManager.GetStatus(_selectedModelId);

        switch (status)
        {
            case ModelStatus.Downloading:
                _liteButton!.Visible = false;
                _standardButton!.Visible = false;
                _skipButton!.Visible = false;
                _progressBar!.Visible = true;
                _statusLabel!.Text = "Downloading...";
                _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.4f));
                break;

            case ModelStatus.Downloaded:
            case ModelStatus.Active:
                _downloadComplete = true;
                _liteButton!.Visible = false;
                _standardButton!.Visible = false;
                _skipButton!.Visible = false;
                _progressBar!.Visible = false;
                _muteButton!.Visible = false;
                _pleaseHoldLabel!.Visible = false;
                _continueButton!.Visible = true;
                _continueButton.Text = "Restart Game";
                _statusLabel!.Text = "Download complete! Restart required for optimal GPU performance.";
                _statusLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1.0f, 0.4f));

                // Auto-activate the model
                if (_modelManager.ActiveModelId == null)
                {
                    _modelManager.SetActiveModel(_selectedModelId);
                }
                break;
        }
    }

    private async void StartDownload()
    {
        if (_modelManager == null) return;

        // Update model info display
        UpdateModelInfoDisplay();

        _liteButton!.Visible = false;
        _standardButton!.Visible = false;
        _skipButton!.Visible = false;
        _cancelButton!.Visible = true;
        _progressBar!.Visible = true;
        _muteButton!.Visible = true;
        _pleaseHoldLabel!.Visible = true;
        _statusLabel!.Text = "Starting download...";
        _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.4f));

        // Start the elevator music
        MusicManager.Instance?.Play();
        UpdateMuteButtonText();

        // Create cancellation token
        _downloadCts = new CancellationTokenSource();

        var progress = new Progress<DownloadProgress>(p =>
        {
            CallDeferred(nameof(UpdateProgress), p.BytesDownloaded, p.TotalBytes);
        });

        try
        {
            await _modelManager.DownloadAsync(_selectedModelId, progress, _downloadCts.Token);
            GD.Print($"First-run model download complete: {_selectedModelId}");
        }
        catch (OperationCanceledException)
        {
            _statusLabel!.Text = "Download cancelled. You can try again or use Classic Mode.";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.7f, 0.4f));
            ResetToInitialState();
        }
        catch (System.Exception ex)
        {
            _statusLabel!.Text = $"Download failed: {ex.Message}";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 0.5f));
            ResetToInitialState();
            GD.PrintErr($"First-run download failed: {ex}");
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private void OnCancelPressed()
    {
        _downloadCts?.Cancel();
        _cancelButton!.Disabled = true;
        _statusLabel!.Text = "Cancelling download...";
    }

    private void ResetToInitialState()
    {
        _liteButton!.Visible = true;
        _liteButton.Disabled = false;
        _standardButton!.Visible = true;
        _standardButton.Disabled = false;
        _skipButton!.Visible = true;
        _skipButton.Disabled = false;
        _cancelButton!.Visible = false;
        _cancelButton.Disabled = false;
        _progressBar!.Visible = false;
        _muteButton!.Visible = false;
        _pleaseHoldLabel!.Visible = false;
    }

    private void OnMutePressed()
    {
        MusicManager.Instance?.ToggleMute();
        UpdateMuteButtonText();
    }

    private void UpdateMuteButtonText()
    {
        if (_muteButton == null) return;
        var isMuted = MusicManager.Instance?.IsMuted ?? false;
        _muteButton.Text = isMuted ? "🔇" : "🔊";
    }

    private void UpdateProgress(long downloaded, long total)
    {
        if (_progressBar == null || _statusLabel == null) return;

        var percent = total > 0 ? (double)downloaded / total * 100 : 0;
        _progressBar.Value = percent;
        _statusLabel.Text = $"Downloading... {FormatSize(downloaded)} / {FormatSize(total)} ({percent:F0}%)";
    }

    private void OnSkipPressed()
    {
        MarkFirstRunComplete();
        EmitSignal(SignalName.SetupComplete, false);
        QueueFree();
    }

    private void OnContinuePressed()
    {
        MarkFirstRunComplete();

        if (_downloadComplete)
        {
            // Restart the game to ensure proper GPU initialization
            // Native libraries need a fresh start for optimal performance
            GetTree().ReloadCurrentScene();
        }
        else
        {
            EmitSignal(SignalName.SetupComplete, true);
            QueueFree();
        }
    }

    private void MarkFirstRunComplete()
    {
        // Save that first-run setup has been shown
        var config = new ConfigFile();
        config.SetValue("llm", "first_run_shown", true);
        config.Save("user://llm_settings.cfg");
    }

    /// <summary>
    /// Checks if first-run setup should be shown.
    /// </summary>
    public static bool ShouldShowFirstRun()
    {
        var config = new ConfigFile();
        var error = config.Load("user://llm_settings.cfg");

        if (error != Error.Ok)
        {
            return true; // No config = first run
        }

        return !(bool)config.GetValue("llm", "first_run_shown", false);
    }

    private static string FormatSize(long bytes)
    {
        const double GB = 1_000_000_000;
        const double MB = 1_000_000;

        if (bytes >= GB)
            return $"{bytes / GB:F1} GB";
        if (bytes >= MB)
            return $"{bytes / MB:F0} MB";
        return $"{bytes / 1000:F0} KB";
    }

}
