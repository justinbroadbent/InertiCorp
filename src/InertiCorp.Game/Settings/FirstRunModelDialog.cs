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
    private Label? _pleaseHoldLabel;
    private ProgressBar? _progressBar;
    private Button? _downloadButton;
    private Button? _skipButton;
    private Button? _cancelButton;
    private Button? _continueButton;
    private Button? _muteButton;
    private bool _downloadComplete;
    private CancellationTokenSource? _downloadCts;

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

        vbox.AddChild(new HSeparator());

        // Explanation - corporate CEO pitch
        var explanationLabel = new Label
        {
            Text = "The InertiCorp Executive Training Simulator leverages cutting-edge " +
                   "Artificial Intelligence to deliver a truly immersive leadership experience.\n\n" +
                   "Our proprietary Cognitive Synergy Engine™ generates dynamic, context-aware " +
                   "communications that respond to YOUR strategic decisions in real-time. " +
                   "This is the same technology used by Fortune 500 companies to develop " +
                   "their next generation of C-suite executives.\n\n" +
                   "Download typically takes 5-10 minutes. The model runs 100% locally - " +
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

        // Buttons
        var buttonContainer = new HBoxContainer();
        buttonContainer.AddThemeConstantOverride("separation", 16);
        var buttonWrapper = new CenterContainer();
        buttonWrapper.AddChild(buttonContainer);
        vbox.AddChild(buttonWrapper);

        _downloadButton = new Button
        {
            Text = "Enable AI (2.3 GB)",
            CustomMinimumSize = new Vector2(180, 45),
            TooltipText = "Download the AI model for dynamic, personalized emails"
        };
        _downloadButton.AddThemeFontSizeOverride("font_size", 16);
        _downloadButton.Pressed += OnDownloadPressed;
        buttonContainer.AddChild(_downloadButton);

        _skipButton = new Button
        {
            Text = "Use Classic Mode",
            CustomMinimumSize = new Vector2(140, 45),
            TooltipText = "Use pre-written corporate templates (no download required)"
        };
        _skipButton.AddThemeFontSizeOverride("font_size", 14);
        _skipButton.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        _skipButton.Pressed += OnSkipPressed;

        _cancelButton = new Button
        {
            Text = "Cancel Download",
            CustomMinimumSize = new Vector2(140, 45),
            Visible = false
        };
        _cancelButton.AddThemeFontSizeOverride("font_size", 14);
        _cancelButton.AddThemeColorOverride("font_color", new Color(0.9f, 0.5f, 0.5f));
        _cancelButton.Pressed += OnCancelPressed;
        buttonContainer.AddChild(_skipButton);
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

    private Control CreateModelInfoBox()
    {
        var modelInfo = ModelCatalog.Default;

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

        // Model details
        var infoVbox = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        infoVbox.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(infoVbox);

        var nameLabel = new Label { Text = modelInfo.Name };
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.7f));
        infoVbox.AddChild(nameLabel);

        var descLabel = new Label { Text = modelInfo.Description };
        descLabel.AddThemeFontSizeOverride("font_size", 13);
        descLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        infoVbox.AddChild(descLabel);

        var sizeLabel = new Label { Text = $"Size: {FormatSize(modelInfo.SizeBytes)}" };
        sizeLabel.AddThemeFontSizeOverride("font_size", 12);
        sizeLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.55f));
        infoVbox.AddChild(sizeLabel);

        return container;
    }

    private void OnModelStatusChanged(string modelId)
    {
        CallDeferred(nameof(UpdateUI));
    }

    private void UpdateUI()
    {
        if (_modelManager == null) return;

        var status = _modelManager.GetStatus(ModelCatalog.DefaultModelId);

        switch (status)
        {
            case ModelStatus.Downloading:
                _downloadButton!.Visible = false;
                _skipButton!.Visible = false;
                _progressBar!.Visible = true;
                _statusLabel!.Text = "Downloading...";
                _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.9f, 0.4f));
                break;

            case ModelStatus.Downloaded:
            case ModelStatus.Active:
                _downloadComplete = true;
                _downloadButton!.Visible = false;
                _skipButton!.Visible = false;
                _progressBar!.Visible = false;
                _muteButton!.Visible = false;
                _pleaseHoldLabel!.Visible = false;
                _continueButton!.Visible = true;
                _statusLabel!.Text = "Download complete! AI emails are now enabled.";
                _statusLabel.AddThemeColorOverride("font_color", new Color(0.4f, 1.0f, 0.4f));

                // Auto-activate the model
                if (_modelManager.ActiveModelId == null)
                {
                    _modelManager.SetActiveModel(ModelCatalog.DefaultModelId);
                }
                break;
        }
    }

    private async void OnDownloadPressed()
    {
        if (_modelManager == null) return;

        _downloadButton!.Visible = false;
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
            await _modelManager.DownloadAsync(ModelCatalog.DefaultModelId, progress, _downloadCts.Token);
            GD.Print("First-run model download complete");
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
        _downloadButton!.Visible = true;
        _downloadButton.Disabled = false;
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
        EmitSignal(SignalName.SetupComplete, true);
        QueueFree();
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
