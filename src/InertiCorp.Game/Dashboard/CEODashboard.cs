using Godot;
using InertiCorp.Core;
using InertiCorp.Core.Cards;
using InertiCorp.Core.Content;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Email;
using InertiCorp.Core.Quarter;
using InertiCorp.Game.Settings;

namespace InertiCorp.Game.Dashboard;

/// <summary>
/// Main CEO Dashboard UI - an interactive executive command center.
/// Displays all key information in a clean, professional layout.
/// </summary>
public partial class CEODashboard : Control
{
    private GameManager? _gameManager;

    // Header section
    private Label? _quarterLabel;
    private Label? _phaseLabel;
    private Label? _pcLabel;
    private Label? _evilLabel;
    private Label? _bonusLabel;
    private Label? _favorLabel;
    private Label? _projectsLabel;

    // Left panel - Company metrics
    private VBoxContainer? _metricsContainer;

    // Center panel - Main action area
    private VBoxContainer? _actionContainer;
    private Label? _actionHeader;
    private RichTextLabel? _actionDescription;
    private VBoxContainer? _choicesContainer;

    // Right panel - Status/Info
    private VBoxContainer? _statusContainer;

    // Center inbox panel
    private VBoxContainer? _inboxContainer;
    private ScrollContainer? _inboxScroll;
    private VBoxContainer? _emailDetailContainer;
    private EmailThread? _selectedThread;
    private bool _isShowingEmailThread; // Re-entrance guard

    // Bottom panel - Hand
    private PanelContainer? _handPanel;
    private HBoxContainer? _handContainer;
    private Label? _handInfo;
    private bool _handPanelVisible = false;
    private Tween? _handPanelTween;

    // Card detail popup
    private PanelContainer? _cardDetailPopup;
    private Label? _cardDetailTitle;
    private RichTextLabel? _cardDetailDescription;
    private RichTextLabel? _cardDetailOutcomes;
    private PlayableCard? _hoveredCard;
    private bool _hoveredCardCanPlay;
    private Button? _cardPlayButton;
    private Button? _cardCloseButton;

    // Animation state
    private int _lastPCValue = -1;
    private Tween? _pcAnimationTween;

    // Reports popup and history tracking
    private ReportsPopup? _reportsPopup;
    private readonly QuarterHistory _quarterHistory = new();
    private int _totalCardsPlayed = 0;
    private int _lastTrackedQuarter = 0;

    // News ticker and silly KPIs
    private PanelContainer? _tickerPanel;
    private Control? _tickerScrollContainer;
    private Label? _tickerLabel;
    private Tween? _tickerTween;
    private readonly Dictionary<int, int> _sillyKPIs = new();
    private readonly List<string> _tickerQueue = new();
    private int _tickerIndex = 0;
    private bool _tickerScrolling = false;

    // Background crisis timer (occasional interrupts for tension)
    private double _crisisTimer = 0;
    private const double CrisisCheckInterval = 45.0; // Check every 45 seconds
    private const int CrisisChance = 8; // 8% chance per check (~10% per minute)

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        // Style tooltips to be more readable (opaque background)
        SetupTooltipStyle();

        BuildDashboard();

        if (_gameManager is not null)
        {
            _gameManager.StateChanged += OnStateChanged;
            _gameManager.PhaseChanged += OnPhaseChanged;
        }

        UpdateDashboard();
    }

    private void SetupTooltipStyle()
    {
        // Create a custom theme for opaque tooltips
        var theme = new Theme();

        var tooltipStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.12f, 1.0f), // Fully opaque dark background
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        tooltipStyle.BorderWidthTop = 1;
        tooltipStyle.BorderWidthBottom = 1;
        tooltipStyle.BorderWidthLeft = 1;
        tooltipStyle.BorderWidthRight = 1;
        tooltipStyle.BorderColor = new Color(0.3f, 0.3f, 0.35f);
        tooltipStyle.CornerRadiusTopLeft = 4;
        tooltipStyle.CornerRadiusTopRight = 4;
        tooltipStyle.CornerRadiusBottomLeft = 4;
        tooltipStyle.CornerRadiusBottomRight = 4;

        theme.SetStylebox("panel", "TooltipPanel", tooltipStyle);

        // Set tooltip label color
        theme.SetColor("font_color", "TooltipLabel", new Color(0.9f, 0.9f, 0.9f));
        theme.SetFontSize("font_size", "TooltipLabel", 13);

        Theme = theme;
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= OnStateChanged;
            _gameManager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnStateChanged() => UpdateDashboard();

    private void OnPhaseChanged()
    {
        // Track snapshot when entering Projects phase (start of new quarter)
        if (_gameManager?.CurrentState?.Quarter.Phase == GamePhase.BoardDemand)
        {
            TrackQuarterSnapshot();
        }

        // Close card popup if we leave PlayCards phase
        if (_gameManager?.CurrentState?.Quarter.Phase != GamePhase.PlayCards && _cardDetailPopup?.Visible == true)
        {
            HideCardDetail();
        }

        UpdateDashboard();
    }

    private void BuildDashboard()
    {
        // Main container with dark theme
        var mainStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        };

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", mainStyle);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 0);
        panel.AddChild(vbox);

        // Header bar
        BuildHeaderBar(vbox);

        // Main content area (3-column layout)
        var contentHBox = new HBoxContainer();
        contentHBox.AddThemeConstantOverride("separation", 0);
        contentHBox.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(contentHBox);

        // Left panel - Metrics (narrow, 180px)
        BuildMetricsPanel(contentHBox);

        // Center panel - EMAIL INBOX (expand) - this is the main focus
        BuildInboxCenterPanel(contentHBox);

        // Right panel - Status + Action buttons (narrow, 220px)
        BuildStatusPanel(contentHBox);

        // News ticker (Fox News style scrolling headlines)
        BuildTickerPanel(vbox);

        // Bottom panel - Hand
        BuildHandPanel(vbox);

        // Card detail popup (floating, initially hidden)
        BuildCardDetailPopup(panel);
    }

    private void BuildCardDetailPopup(Control parent)
    {
        // Outer frame - like a Magic card border
        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.12f, 0.08f), // Bronze/gold frame
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        frameStyle.BorderWidthTop = 3;
        frameStyle.BorderWidthBottom = 3;
        frameStyle.BorderWidthLeft = 3;
        frameStyle.BorderWidthRight = 3;
        frameStyle.BorderColor = new Color(0.6f, 0.5f, 0.3f);
        frameStyle.CornerRadiusTopLeft = 12;
        frameStyle.CornerRadiusTopRight = 12;
        frameStyle.CornerRadiusBottomLeft = 12;
        frameStyle.CornerRadiusBottomRight = 12;
        frameStyle.ShadowColor = new Color(0, 0, 0, 0.7f);
        frameStyle.ShadowSize = 15;

        _cardDetailPopup = new PanelContainer
        {
            CustomMinimumSize = new Vector2(340, 480),  // Fixed Magic card size
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop  // Block clicks behind popup
        };
        _cardDetailPopup.AddThemeStyleboxOverride("panel", frameStyle);
        parent.AddChild(_cardDetailPopup);

        // Make it not expand - use exact size
        _cardDetailPopup.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _cardDetailPopup.SizeFlagsVertical = SizeFlags.ShrinkCenter;

        var innerPanel = new PanelContainer();
        var innerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.08f),
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        };
        innerStyle.CornerRadiusTopLeft = 8;
        innerStyle.CornerRadiusTopRight = 8;
        innerStyle.CornerRadiusBottomLeft = 8;
        innerStyle.CornerRadiusBottomRight = 8;
        innerPanel.AddThemeStyleboxOverride("panel", innerStyle);
        _cardDetailPopup.AddChild(innerPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 0);
        innerPanel.AddChild(vbox);

        // Title bar (like card name banner)
        var titleBar = new PanelContainer();
        var titleStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.08f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        titleStyle.CornerRadiusTopLeft = 8;
        titleStyle.CornerRadiusTopRight = 8;
        titleBar.AddThemeStyleboxOverride("panel", titleStyle);
        vbox.AddChild(titleBar);

        var titleRow = new HBoxContainer();
        titleBar.AddChild(titleRow);

        _cardDetailTitle = new Label { Text = "Card Title", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _cardDetailTitle.AddThemeFontSizeOverride("font_size", 16);
        _cardDetailTitle.Modulate = new Color(0.95f, 0.9f, 0.8f);
        titleRow.AddChild(_cardDetailTitle);

        // Category in title bar
        var categoryLabel = new Label { Name = "CategoryLabel", Text = "[Action]" };
        categoryLabel.AddThemeFontSizeOverride("font_size", 11);
        categoryLabel.Modulate = new Color(0.6f, 0.6f, 0.65f);
        titleRow.AddChild(categoryLabel);

        // "Art" area - colored banner based on card type
        var artArea = new PanelContainer { Name = "ArtArea", CustomMinimumSize = new Vector2(0, 60) };
        var artStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.2f, 0.3f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        artArea.AddThemeStyleboxOverride("panel", artStyle);
        vbox.AddChild(artArea);

        // Flavor text in art area
        var flavorLabel = new Label
        {
            Name = "FlavorText",
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        flavorLabel.AddThemeFontSizeOverride("font_size", 12);
        flavorLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        artArea.AddChild(flavorLabel);

        // Text box area
        var textBox = new PanelContainer();
        var textStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        textBox.AddThemeStyleboxOverride("panel", textStyle);
        vbox.AddChild(textBox);

        var textContent = new VBoxContainer();
        textContent.AddThemeConstantOverride("separation", 10);
        textBox.AddChild(textContent);

        // Extended description
        _cardDetailDescription = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(0, 60)
        };
        _cardDetailDescription.AddThemeFontSizeOverride("normal_font_size", 11);
        textContent.AddChild(_cardDetailDescription);

        // Outcomes box
        var outcomesBox = new PanelContainer();
        var outcomesStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        outcomesStyle.CornerRadiusTopLeft = 4;
        outcomesStyle.CornerRadiusTopRight = 4;
        outcomesStyle.CornerRadiusBottomLeft = 4;
        outcomesStyle.CornerRadiusBottomRight = 4;
        outcomesStyle.BorderWidthTop = 1;
        outcomesStyle.BorderWidthBottom = 1;
        outcomesStyle.BorderWidthLeft = 1;
        outcomesStyle.BorderWidthRight = 1;
        outcomesStyle.BorderColor = new Color(0.2f, 0.2f, 0.25f);
        outcomesBox.AddThemeStyleboxOverride("panel", outcomesStyle);
        textContent.AddChild(outcomesBox);

        var outcomesContent = new VBoxContainer();
        outcomesContent.AddThemeConstantOverride("separation", 6);
        outcomesBox.AddChild(outcomesContent);

        var outcomesHeader = new Label { Text = "POTENTIAL OUTCOMES" };
        outcomesHeader.AddThemeFontSizeOverride("font_size", 10);
        outcomesHeader.Modulate = new Color(0.5f, 0.5f, 0.55f);
        outcomesContent.AddChild(outcomesHeader);

        _cardDetailOutcomes = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(0, 50)
        };
        _cardDetailOutcomes.AddThemeFontSizeOverride("normal_font_size", 10);
        outcomesContent.AddChild(_cardDetailOutcomes);

        // Bottom bar with buttons
        var bottomBar = new PanelContainer();
        var bottomStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.06f),
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        bottomStyle.CornerRadiusBottomLeft = 8;
        bottomStyle.CornerRadiusBottomRight = 8;
        bottomBar.AddThemeStyleboxOverride("panel", bottomStyle);
        vbox.AddChild(bottomBar);

        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 12);
        bottomBar.AddChild(buttonRow);

        // Close button
        _cardCloseButton = new Button
        {
            Text = "✕ CLOSE",
            CustomMinimumSize = new Vector2(120, 36),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var closeBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.15f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        closeBtnStyle.BorderWidthTop = 1;
        closeBtnStyle.BorderWidthBottom = 1;
        closeBtnStyle.BorderWidthLeft = 1;
        closeBtnStyle.BorderWidthRight = 1;
        closeBtnStyle.BorderColor = new Color(0.5f, 0.3f, 0.3f);
        closeBtnStyle.CornerRadiusTopLeft = 6;
        closeBtnStyle.CornerRadiusTopRight = 6;
        closeBtnStyle.CornerRadiusBottomLeft = 6;
        closeBtnStyle.CornerRadiusBottomRight = 6;
        _cardCloseButton.AddThemeStyleboxOverride("normal", closeBtnStyle);

        var closeBtnHover = new StyleBoxFlat
        {
            BgColor = new Color(0.3f, 0.2f, 0.2f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        closeBtnHover.BorderWidthTop = 1;
        closeBtnHover.BorderWidthBottom = 1;
        closeBtnHover.BorderWidthLeft = 1;
        closeBtnHover.BorderWidthRight = 1;
        closeBtnHover.BorderColor = new Color(0.7f, 0.4f, 0.4f);
        closeBtnHover.CornerRadiusTopLeft = 6;
        closeBtnHover.CornerRadiusTopRight = 6;
        closeBtnHover.CornerRadiusBottomLeft = 6;
        closeBtnHover.CornerRadiusBottomRight = 6;
        _cardCloseButton.AddThemeStyleboxOverride("hover", closeBtnHover);
        _cardCloseButton.AddThemeStyleboxOverride("pressed", closeBtnHover);
        _cardCloseButton.Pressed += HideCardDetail;
        buttonRow.AddChild(_cardCloseButton);

        // Play button
        _cardPlayButton = new Button
        {
            Text = "▶ EXECUTE PROJECT",
            CustomMinimumSize = new Vector2(160, 36),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var playBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.35f, 0.2f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        playBtnStyle.BorderWidthTop = 1;
        playBtnStyle.BorderWidthBottom = 1;
        playBtnStyle.BorderWidthLeft = 1;
        playBtnStyle.BorderWidthRight = 1;
        playBtnStyle.BorderColor = new Color(0.3f, 0.6f, 0.4f);
        playBtnStyle.CornerRadiusTopLeft = 6;
        playBtnStyle.CornerRadiusTopRight = 6;
        playBtnStyle.CornerRadiusBottomLeft = 6;
        playBtnStyle.CornerRadiusBottomRight = 6;
        _cardPlayButton.AddThemeStyleboxOverride("normal", playBtnStyle);

        var playBtnHover = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.5f, 0.3f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        playBtnHover.BorderWidthTop = 1;
        playBtnHover.BorderWidthBottom = 1;
        playBtnHover.BorderWidthLeft = 1;
        playBtnHover.BorderWidthRight = 1;
        playBtnHover.BorderColor = new Color(0.4f, 0.8f, 0.5f);
        playBtnHover.CornerRadiusTopLeft = 6;
        playBtnHover.CornerRadiusTopRight = 6;
        playBtnHover.CornerRadiusBottomLeft = 6;
        playBtnHover.CornerRadiusBottomRight = 6;
        _cardPlayButton.AddThemeStyleboxOverride("hover", playBtnHover);
        _cardPlayButton.AddThemeStyleboxOverride("pressed", playBtnHover);

        var playBtnDisabled = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        playBtnDisabled.BorderWidthTop = 1;
        playBtnDisabled.BorderWidthBottom = 1;
        playBtnDisabled.BorderWidthLeft = 1;
        playBtnDisabled.BorderWidthRight = 1;
        playBtnDisabled.BorderColor = new Color(0.2f, 0.2f, 0.25f);
        playBtnDisabled.CornerRadiusTopLeft = 6;
        playBtnDisabled.CornerRadiusTopRight = 6;
        playBtnDisabled.CornerRadiusBottomLeft = 6;
        playBtnDisabled.CornerRadiusBottomRight = 6;
        _cardPlayButton.AddThemeStyleboxOverride("disabled", playBtnDisabled);

        _cardPlayButton.Pressed += OnCardPlayButtonPressed;
        buttonRow.AddChild(_cardPlayButton);
    }

    private void OnCardPlayButtonPressed()
    {
        if (_hoveredCard is null || !_hoveredCardCanPlay) return;

        var cardId = _hoveredCard.CardId;
        HideCardDetail();
        PlayCardImmediately(cardId);
    }

    private void ShowCardDetail(PlayableCard card, bool canPlay = false)
    {
        if (_cardDetailPopup is null || _cardDetailTitle is null ||
            _cardDetailDescription is null || _cardDetailOutcomes is null) return;

        // Only show card popups during PlayCards phase
        if (_gameManager?.CurrentState?.Quarter.Phase != GamePhase.PlayCards) return;

        _hoveredCard = card;
        _hoveredCardCanPlay = canPlay;
        _cardDetailTitle.Text = card.Title;

        // Configure play button based on canPlay
        if (_cardPlayButton is not null)
        {
            _cardPlayButton.Disabled = !canPlay;
            _cardPlayButton.Text = canPlay ? "▶ EXECUTE PROJECT" : "▶ LOCKED";
            _cardPlayButton.Modulate = canPlay ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
        }

        // Set category label with risk level
        var categoryLabel = _cardDetailPopup.FindChild("CategoryLabel", true, false) as Label;
        if (categoryLabel is not null)
        {
            var riskColorHex = card.RiskLevel switch
            {
                1 => "#66bb66",  // Green for SAFE
                3 => "#dd6666",  // Red for VOLATILE
                _ => "#ddbb55"   // Yellow for MODERATE
            };
            categoryLabel.Text = $"[{card.Category}] [{card.RiskLabel}]";
            categoryLabel.Modulate = card.RiskLevel switch
            {
                1 => new Color(0.4f, 0.75f, 0.4f),
                3 => new Color(0.85f, 0.4f, 0.4f),
                _ => new Color(0.85f, 0.75f, 0.3f)
            };
        }

        // Set art area color based on meter affinity
        var artArea = _cardDetailPopup.FindChild("ArtArea", true, false) as PanelContainer;
        if (artArea is not null)
        {
            var meterColor = GetMeterColor(card.MeterAffinity);
            var artColor = new Color(meterColor.R * 0.4f, meterColor.G * 0.4f, meterColor.B * 0.4f);
            var artStyle = new StyleBoxFlat
            {
                BgColor = artColor,
                ContentMarginLeft = 15,
                ContentMarginRight = 15,
                ContentMarginTop = 10,
                ContentMarginBottom = 10
            };
            artStyle.BorderWidthTop = 2;
            artStyle.BorderWidthBottom = 2;
            artStyle.BorderColor = meterColor * 0.6f;
            artArea.AddThemeStyleboxOverride("panel", artStyle);
        }

        // Set flavor text
        var flavorLabel = _cardDetailPopup.FindChild("FlavorText", true, false) as Label;
        if (flavorLabel is not null)
        {
            flavorLabel.Text = card.FlavorText;
        }

        // Set extended description
        var desc = card.ExtendedDescription ?? card.Description;
        _cardDetailDescription.Text = $"[color=#ccccdd]{desc}[/color]";

        // Set outcomes with color coding
        var outcomeText = new System.Text.StringBuilder();

        // Get scaling parameters for revenue cards
        int? targetAmount = null;
        int? delivery = null;
        if (card.Category == InertiCorp.Core.Cards.CardCategory.Revenue && _gameManager?.CurrentState is not null)
        {
            var state = _gameManager.CurrentState;
            targetAmount = BoardDirective.ProfitIncrease.GetRequiredAmount(state.CEO.BoardPressureLevel);
            delivery = state.Org.Delivery;
            var revenueProjection = card.GetScaledRevenueProjection(targetAmount.Value, delivery.Value);
            if (!string.IsNullOrEmpty(revenueProjection))
            {
                outcomeText.AppendLine($"[color=#ffd940][b]PROJECTED REVENUE:[/b] {revenueProjection}[/color]");
                outcomeText.AppendLine();
            }
        }

        // Zero-meter warning at the top if applicable
        if (_gameManager?.CurrentState?.Org is OrgState orgState)
        {
            var warningText = card.GetZeroMeterWarningText(orgState);
            if (!string.IsNullOrEmpty(warningText))
            {
                outcomeText.AppendLine($"[color=#ff9933][b]{warningText}[/b][/color]");
                outcomeText.AppendLine("[color=#cc7722][i]Negative effects on critical metrics won't apply[/i][/color]");
                outcomeText.AppendLine();
            }
        }

        outcomeText.AppendLine($"[color=#66dd66]Best:[/color] {FormatCardOutcome(card, card.Outcomes.Good, targetAmount, delivery)}");
        outcomeText.AppendLine($"[color=#dddd66]Expected:[/color] {FormatCardOutcome(card, card.Outcomes.Expected, targetAmount, delivery)}");
        outcomeText.AppendLine($"[color=#dd6666]Worst:[/color] {FormatCardOutcome(card, card.Outcomes.Bad, targetAmount, delivery)}");
        if (card.IsCorporate)
        {
            outcomeText.AppendLine($"[color=#dd8866]Evil Score: +{card.CorporateIntensity}[/color]");
        }
        if (card.HasMeterAffinity && _gameManager?.CurrentState?.Org is OrgState org)
        {
            var affinityMod = card.GetAffinityModifier(org);
            var meterName = card.MeterAffinity!.Value.ToString();
            var modColor = affinityMod > 0 ? "#66dd88" : affinityMod < 0 ? "#dd6666" : "#aaaaaa";
            var modText = affinityMod > 0 ? $"-{affinityMod}% risk" : affinityMod < 0 ? $"+{-affinityMod}% risk" : "neutral";
            outcomeText.AppendLine($"[color={modColor}]{meterName} Affinity: {modText}[/color]");
        }
        _cardDetailOutcomes.Text = outcomeText.ToString().Trim();

        // Position popup in center using fixed size
        var popupSize = new Vector2(340, 480);
        _cardDetailPopup.Size = popupSize;
        _cardDetailPopup.Position = new Vector2(
            (GetViewportRect().Size.X - popupSize.X) / 2,
            (GetViewportRect().Size.Y - popupSize.Y) / 2
        );

        // Animate popup appearing
        _cardDetailPopup.Modulate = new Color(1, 1, 1, 0);
        _cardDetailPopup.Scale = new Vector2(0.9f, 0.9f);
        _cardDetailPopup.PivotOffset = popupSize / 2;  // Scale from center
        _cardDetailPopup.Visible = true;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_cardDetailPopup, "modulate:a", 1.0f, 0.15f)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_cardDetailPopup, "scale", Vector2.One, 0.15f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void HideCardDetail()
    {
        if (_cardDetailPopup is not null && _cardDetailPopup.Visible)
        {
            // Animate popup closing
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(_cardDetailPopup, "modulate:a", 0.0f, 0.1f)
                .SetEase(Tween.EaseType.In);
            tween.TweenProperty(_cardDetailPopup, "scale", new Vector2(0.95f, 0.95f), 0.1f)
                .SetEase(Tween.EaseType.In);
            tween.Chain().TweenCallback(Callable.From(() =>
            {
                if (_cardDetailPopup is not null)
                    _cardDetailPopup.Visible = false;
            }));

            _hoveredCard = null;
        }
    }

    private void BuildHeaderBar(VBoxContainer parent)
    {
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f),
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        headerStyle.BorderWidthBottom = 2;
        headerStyle.BorderColor = new Color(0.15f, 0.15f, 0.2f);

        var header = new PanelContainer();
        header.AddThemeStyleboxOverride("panel", headerStyle);
        parent.AddChild(header);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 30);
        header.AddChild(hbox);

        // Title
        var title = new Label { Text = "INERTICORP" };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.Modulate = new Color(0.9f, 0.9f, 0.95f);
        hbox.AddChild(title);

        var subtitle = new Label { Text = "CEO Command Center" };
        subtitle.AddThemeFontSizeOverride("font_size", 14);
        subtitle.Modulate = new Color(0.5f, 0.5f, 0.55f);
        subtitle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        hbox.AddChild(subtitle);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        hbox.AddChild(spacer);

        // Quarter indicator
        _quarterLabel = new Label { Text = "Y1Q1" };
        _quarterLabel.AddThemeFontSizeOverride("font_size", 20);
        _quarterLabel.Modulate = new Color(0.4f, 0.8f, 0.9f);
        hbox.AddChild(_quarterLabel);

        // Phase indicator
        _phaseLabel = new Label { Text = "PROJECTS PHASE" };
        _phaseLabel.AddThemeFontSizeOverride("font_size", 14);
        _phaseLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
        hbox.AddChild(_phaseLabel);

        // Political Capital - prominent display
        var pcBox = new PanelContainer();
        var pcStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.12f, 0.05f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        pcStyle.BorderWidthTop = 1;
        pcStyle.BorderWidthBottom = 1;
        pcStyle.BorderWidthLeft = 1;
        pcStyle.BorderWidthRight = 1;
        pcStyle.BorderColor = new Color(0.4f, 0.35f, 0.2f);
        pcStyle.CornerRadiusTopLeft = 6;
        pcStyle.CornerRadiusTopRight = 6;
        pcStyle.CornerRadiusBottomLeft = 6;
        pcStyle.CornerRadiusBottomRight = 6;
        pcBox.AddThemeStyleboxOverride("panel", pcStyle);
        hbox.AddChild(pcBox);

        var pcHbox = new HBoxContainer();
        pcHbox.AddThemeConstantOverride("separation", 6);
        pcBox.AddChild(pcHbox);

        var pcIcon = new Label { Text = "⭐ PC:" };
        pcIcon.AddThemeFontSizeOverride("font_size", 14);
        pcIcon.Modulate = new Color(0.9f, 0.8f, 0.5f);
        pcHbox.AddChild(pcIcon);

        _pcLabel = new Label { Text = "3" };
        _pcLabel.AddThemeFontSizeOverride("font_size", 20);
        _pcLabel.Modulate = new Color(1.0f, 0.9f, 0.4f);
        pcHbox.AddChild(_pcLabel);

        // Add tooltip explaining Political Capital
        pcBox.TooltipText = "Political Capital (PC)\n" +
            "━━━━━━━━━━━━━━━━━━━━\n" +
            "Your corporate influence currency.\n\n" +
            "EARN PC:\n" +
            "• High Governance/Alignment (+1 each)\n" +
            "• Restraint bonus (fewer cards played)\n\n" +
            "SPEND PC:\n" +
            "• Boost any metric +5 (1 PC)\n" +
            "• Schmooze the board (2 PC)\n" +
            "• Re-org hand for new cards (3 PC)\n" +
            "• Rehabilitate image (2 PC = -1 Evil)\n" +
            "• 2nd card costs 1 PC (+10% risk)\n" +
            "• 3rd card costs 2 PC (+20% risk)\n\n" +
            "Max: 20 PC | Decays above 10";

        // Evil Score - tracks corporate misdeeds
        var evilBox = new PanelContainer();
        var evilStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.05f, 0.08f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        evilStyle.BorderWidthTop = 1;
        evilStyle.BorderWidthBottom = 1;
        evilStyle.BorderWidthLeft = 1;
        evilStyle.BorderWidthRight = 1;
        evilStyle.BorderColor = new Color(0.4f, 0.2f, 0.2f);
        evilStyle.CornerRadiusTopLeft = 6;
        evilStyle.CornerRadiusTopRight = 6;
        evilStyle.CornerRadiusBottomLeft = 6;
        evilStyle.CornerRadiusBottomRight = 6;
        evilBox.AddThemeStyleboxOverride("panel", evilStyle);
        hbox.AddChild(evilBox);

        var evilHbox = new HBoxContainer();
        evilHbox.AddThemeConstantOverride("separation", 6);
        evilBox.AddChild(evilHbox);

        var evilIcon = new Label { Text = "😈 Evil:" };
        evilIcon.AddThemeFontSizeOverride("font_size", 14);
        evilIcon.Modulate = new Color(0.9f, 0.5f, 0.5f);
        evilHbox.AddChild(evilIcon);

        _evilLabel = new Label { Text = "0" };
        _evilLabel.AddThemeFontSizeOverride("font_size", 20);
        _evilLabel.Modulate = new Color(1.0f, 0.4f, 0.4f);
        evilHbox.AddChild(_evilLabel);

        // Add tooltip explaining Evil Score
        evilBox.TooltipText = "Evil Score\n" +
            "━━━━━━━━━━━━━━━━━━━━\n" +
            "Tracks your corporate misdeeds.\n\n" +
            "GAIN EVIL:\n" +
            "• Playing 'Corporate' cards\n" +
            "• Layoffs, cost cutting, etc.\n\n" +
            "CONSEQUENCES:\n" +
            "• Board tolerates evil if profits are high\n" +
            "• High evil + low profits = scrutiny\n" +
            "• Very high evil = reputational risk\n\n" +
            "REDEMPTION:\n" +
            "• Spend 2 PC to reduce by 1\n" +
            "• (PR campaigns, charity galas, etc.)";

        // Accumulated Bonus - tracks progress toward retirement
        var bonusBox = new PanelContainer();
        var bonusStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.12f, 0.08f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        bonusStyle.BorderWidthTop = 1;
        bonusStyle.BorderWidthBottom = 1;
        bonusStyle.BorderWidthLeft = 1;
        bonusStyle.BorderWidthRight = 1;
        bonusStyle.BorderColor = new Color(0.2f, 0.4f, 0.3f);
        bonusStyle.CornerRadiusTopLeft = 6;
        bonusStyle.CornerRadiusTopRight = 6;
        bonusStyle.CornerRadiusBottomLeft = 6;
        bonusStyle.CornerRadiusBottomRight = 6;
        bonusBox.AddThemeStyleboxOverride("panel", bonusStyle);
        hbox.AddChild(bonusBox);

        var bonusHbox = new HBoxContainer();
        bonusHbox.AddThemeConstantOverride("separation", 6);
        bonusBox.AddChild(bonusHbox);

        var bonusIcon = new Label { Text = "💰" };
        bonusIcon.AddThemeFontSizeOverride("font_size", 14);
        bonusIcon.Modulate = new Color(0.5f, 0.9f, 0.6f);
        bonusHbox.AddChild(bonusIcon);

        _bonusLabel = new Label { Text = "$0M" };
        _bonusLabel.AddThemeFontSizeOverride("font_size", 18);
        _bonusLabel.Modulate = new Color(0.4f, 1.0f, 0.5f);
        bonusHbox.AddChild(_bonusLabel);

        // Add tooltip explaining Accumulated Bonus
        bonusBox.TooltipText = "Accumulated Bonus\n" +
            "━━━━━━━━━━━━━━━━━━━━\n" +
            "Your golden path to retirement.\n\n" +
            "EARNING BONUS:\n" +
            "• Meet board directives (+$5M)\n" +
            "• Grow quarterly profit (+$2M)\n" +
            "• Keep all metrics healthy (+$3M)\n" +
            "• High board favorability (+$2M)\n" +
            "• Maintain ethics (+$1M)\n\n" +
            "RETIREMENT:\n" +
            $"• Need ${CEOState.RetirementThreshold}M to retire\n" +
            "• Retire = 2x final score multiplier\n" +
            "• Ousted = 0.5x score multiplier\n\n" +
            "Stay longer for higher score,\n" +
            "but risk getting fired!";

        // Board Favor - tracks the board's confidence in you
        var favorBox = new PanelContainer();
        var favorStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        favorStyle.BorderWidthTop = 1;
        favorStyle.BorderWidthBottom = 1;
        favorStyle.BorderWidthLeft = 1;
        favorStyle.BorderWidthRight = 1;
        favorStyle.BorderColor = new Color(0.2f, 0.3f, 0.5f);
        favorStyle.CornerRadiusTopLeft = 6;
        favorStyle.CornerRadiusTopRight = 6;
        favorStyle.CornerRadiusBottomLeft = 6;
        favorStyle.CornerRadiusBottomRight = 6;
        favorBox.AddThemeStyleboxOverride("panel", favorStyle);
        hbox.AddChild(favorBox);

        var favorHbox = new HBoxContainer();
        favorHbox.AddThemeConstantOverride("separation", 6);
        favorBox.AddChild(favorHbox);

        var favorIcon = new Label { Text = "📊 Favor:" };
        favorIcon.AddThemeFontSizeOverride("font_size", 14);
        favorIcon.Modulate = new Color(0.6f, 0.7f, 0.9f);
        favorHbox.AddChild(favorIcon);

        _favorLabel = new Label { Text = "50" };
        _favorLabel.AddThemeFontSizeOverride("font_size", 20);
        _favorLabel.Modulate = new Color(0.5f, 0.7f, 1.0f);
        favorHbox.AddChild(_favorLabel);

        // Add tooltip explaining Board Favor
        favorBox.TooltipText = "Board Favorability\n" +
            "━━━━━━━━━━━━━━━━━━━━\n" +
            "How much the board trusts you.\n\n" +
            "GAINING FAVOR:\n" +
            "• Meet quarterly directives\n" +
            "• Grow profits consistently\n" +
            "• Execute strategic initiatives\n\n" +
            "LOSING FAVOR:\n" +
            "• Miss directives\n" +
            "• Declining profits\n" +
            "• Consecutive weak quarters\n" +
            "• High evil with poor results\n\n" +
            "DANGER ZONES:\n" +
            "• Below 40: Ouster risk increases\n" +
            "• Below 20: Likely to be fired\n\n" +
            "Keep the board happy to survive!";

        // Projects Implemented - tracks total initiatives executed
        var projectsBox = new PanelContainer();
        var projectsStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        projectsStyle.BorderWidthTop = 1;
        projectsStyle.BorderWidthBottom = 1;
        projectsStyle.BorderWidthLeft = 1;
        projectsStyle.BorderWidthRight = 1;
        projectsStyle.BorderColor = new Color(0.4f, 0.3f, 0.5f);
        projectsStyle.CornerRadiusTopLeft = 6;
        projectsStyle.CornerRadiusTopRight = 6;
        projectsStyle.CornerRadiusBottomLeft = 6;
        projectsStyle.CornerRadiusBottomRight = 6;
        projectsBox.AddThemeStyleboxOverride("panel", projectsStyle);
        hbox.AddChild(projectsBox);

        var projectsHbox = new HBoxContainer();
        projectsHbox.AddThemeConstantOverride("separation", 6);
        projectsBox.AddChild(projectsHbox);

        var projectsIcon = new Label { Text = "📋 Projects:" };
        projectsIcon.AddThemeFontSizeOverride("font_size", 14);
        projectsIcon.Modulate = new Color(0.8f, 0.6f, 0.9f);
        projectsHbox.AddChild(projectsIcon);

        _projectsLabel = new Label { Text = "0" };
        _projectsLabel.AddThemeFontSizeOverride("font_size", 20);
        _projectsLabel.Modulate = new Color(0.9f, 0.7f, 1.0f);
        projectsHbox.AddChild(_projectsLabel);

        // Add tooltip explaining Projects
        projectsBox.TooltipText = "Projects Implemented\n" +
            "━━━━━━━━━━━━━━━━━━━━\n" +
            "Total strategic initiatives you've executed.\n\n" +
            "BENEFITS:\n" +
            "• +1 Board Favor per card/quarter (max +3)\n" +
            "• +$1M score bonus per project at game end\n" +
            "• Active CEOs are harder to oust\n\n" +
            "INACTIVITY RISK:\n" +
            "• 0 cards = no quarterly bonus\n" +
            "• Consecutive weak quarters = ouster risk\n" +
            "• No projects = minimal golden parachute\n\n" +
            "The board expects active leadership!";

        // Reports button
        var reportsButton = new Button
        {
            Text = "Reports",
            TooltipText = "View Corporate Performance Reports",
            CustomMinimumSize = new Vector2(80, 40)
        };
        reportsButton.AddThemeFontSizeOverride("font_size", 14);
        reportsButton.Pressed += OnReportsPressed;
        hbox.AddChild(reportsButton);

        // Settings button
        var settingsButton = new Button
        {
            Text = "⚙",
            TooltipText = "Settings",
            CustomMinimumSize = new Vector2(40, 40)
        };
        settingsButton.AddThemeFontSizeOverride("font_size", 20);
        settingsButton.Pressed += OnSettingsPressed;
        hbox.AddChild(settingsButton);
    }

    private void OnSettingsPressed()
    {
        SettingsManager.Instance?.OpenSettings();
    }

    private void OnReportsPressed()
    {
        if (_gameManager is null) return;

        var state = _gameManager.CurrentState;
        if (state is null) return;

        // Create popup if needed
        if (_reportsPopup is null || !IsInstanceValid(_reportsPopup))
        {
            _reportsPopup = new ReportsPopup();
            AddChild(_reportsPopup);
        }

        // Show the reports popup with current history and silly KPIs
        _reportsPopup.Show(_quarterHistory, state.CEO, state.Org, _totalCardsPlayed, _sillyKPIs);
    }

    private void TrackQuarterSnapshot()
    {
        if (_gameManager is null) return;

        var state = _gameManager.CurrentState;
        if (state is null) return;

        // Only track once per quarter, at the start of a new quarter
        var currentQuarter = state.CEO.QuartersSurvived;
        if (currentQuarter <= _lastTrackedQuarter || currentQuarter == 0) return;

        _lastTrackedQuarter = currentQuarter;

        // Get directive status from the last resolution
        var directiveMet = state.Quarter.Phase == GamePhase.BoardDemand
            && state.CEO.QuartersSurvived > 0;

        var snapshot = QuarterHistory.CreateSnapshot(
            currentQuarter,
            state.CEO,
            state.Org,
            directiveMet,
            _totalCardsPlayed);

        _quarterHistory.AddSnapshot(snapshot);
    }

    public override void _Input(InputEvent @event)
    {
        // Escape opens the game menu
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            SettingsManager.Instance?.OpenSettings();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildMetricsPanel(HBoxContainer parent)
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        panelStyle.BorderWidthRight = 1;
        panelStyle.BorderColor = new Color(0.12f, 0.12f, 0.15f);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(180, 0) };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(panel);

        _metricsContainer = new VBoxContainer();
        _metricsContainer.AddThemeConstantOverride("separation", 10);
        panel.AddChild(_metricsContainer);

        var metricsTitle = new Label { Text = "COMPANY" };
        metricsTitle.AddThemeFontSizeOverride("font_size", 10);
        metricsTitle.Modulate = new Color(0.5f, 0.5f, 0.55f);
        _metricsContainer.AddChild(metricsTitle);

        _metricsContainer.AddChild(new HSeparator());
    }

    private void BuildInboxCenterPanel(HBoxContainer parent)
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.04f, 0.06f),
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        };

        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 0);
        hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        panel.AddChild(hbox);

        // Left side: Thread list (40% width, min 320px)
        var threadListPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(320, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.4f
        };
        var threadListStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        threadListStyle.BorderWidthRight = 2;
        threadListStyle.BorderColor = new Color(0.15f, 0.15f, 0.2f);
        threadListPanel.AddThemeStyleboxOverride("panel", threadListStyle);
        hbox.AddChild(threadListPanel);

        var threadListVbox = new VBoxContainer();
        threadListVbox.AddThemeConstantOverride("separation", 8);
        threadListVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        threadListVbox.SizeFlagsVertical = SizeFlags.ExpandFill;
        threadListPanel.AddChild(threadListVbox);

        // Inbox header with glow effect
        var inboxHeaderPanel = new PanelContainer();
        var inboxHeaderStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.15f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        inboxHeaderStyle.CornerRadiusTopLeft = 6;
        inboxHeaderStyle.CornerRadiusTopRight = 6;
        inboxHeaderStyle.CornerRadiusBottomLeft = 6;
        inboxHeaderStyle.CornerRadiusBottomRight = 6;
        inboxHeaderStyle.BorderWidthBottom = 2;
        inboxHeaderStyle.BorderColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);
        inboxHeaderPanel.AddThemeStyleboxOverride("panel", inboxHeaderStyle);
        threadListVbox.AddChild(inboxHeaderPanel);

        var inboxTitle = new Label { Text = "INBOX" };
        inboxTitle.AddThemeFontSizeOverride("font_size", 16);
        inboxTitle.Modulate = new Color(0.7f, 0.85f, 1.0f);
        inboxTitle.HorizontalAlignment = HorizontalAlignment.Center;
        inboxHeaderPanel.AddChild(inboxTitle);

        _inboxScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        threadListVbox.AddChild(_inboxScroll);

        _inboxContainer = new VBoxContainer();
        _inboxContainer.AddThemeConstantOverride("separation", 6);
        _inboxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _inboxScroll.AddChild(_inboxContainer);

        // Right side: Email detail view (60% width)
        var detailPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 0.6f
        };
        var detailStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f),
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        detailPanel.AddThemeStyleboxOverride("panel", detailStyle);
        hbox.AddChild(detailPanel);

        var detailScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        detailPanel.AddChild(detailScroll);

        _emailDetailContainer = new VBoxContainer();
        _emailDetailContainer.AddThemeConstantOverride("separation", 12);
        _emailDetailContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        detailScroll.AddChild(_emailDetailContainer);

        // Show welcome message initially
        ShowWelcomeMessage();

        // Also store action containers for compatibility
        _actionContainer = new VBoxContainer();
        _actionHeader = new Label();
        _actionDescription = new RichTextLabel { BbcodeEnabled = true };
        _choicesContainer = new VBoxContainer();
    }

    private void ShowWelcomeMessage()
    {
        if (_emailDetailContainer is null) return;

        foreach (var child in _emailDetailContainer.GetChildren())
            child.QueueFree();

        var welcomePanel = new PanelContainer();
        var welcomeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.1f),
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 40,
            ContentMarginBottom = 40
        };
        welcomeStyle.CornerRadiusTopLeft = 10;
        welcomeStyle.CornerRadiusTopRight = 10;
        welcomeStyle.CornerRadiusBottomLeft = 10;
        welcomeStyle.CornerRadiusBottomRight = 10;
        welcomePanel.AddThemeStyleboxOverride("panel", welcomeStyle);
        _emailDetailContainer.AddChild(welcomePanel);

        var welcomeVbox = new VBoxContainer();
        welcomeVbox.AddThemeConstantOverride("separation", 12);
        welcomePanel.AddChild(welcomeVbox);

        var icon = new Label { Text = "📬" };
        icon.AddThemeFontSizeOverride("font_size", 48);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        welcomeVbox.AddChild(icon);

        var title = new Label { Text = "Select an email to read" };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.Modulate = new Color(0.6f, 0.6f, 0.7f);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        welcomeVbox.AddChild(title);

        var hint = new Label
        {
            Text = "New messages appear when you execute projects\nor receive board directives and situations.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        hint.Modulate = new Color(0.4f, 0.4f, 0.5f);
        welcomeVbox.AddChild(hint);
    }

    private void ShowGameOverMessage()
    {
        if (_emailDetailContainer is null) return;

        foreach (var child in _emailDetailContainer.GetChildren())
            child.QueueFree();

        var ceo = _gameManager?.CurrentState?.CEO;
        var isRetirement = ceo?.HasRetired ?? false;
        var accentColor = isRetirement
            ? new Color(0.3f, 0.7f, 0.4f)  // Green for retirement
            : new Color(0.7f, 0.3f, 0.3f); // Red for ouster

        var gameOverPanel = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.08f),
            BorderColor = accentColor,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginLeft = 30,
            ContentMarginRight = 30,
            ContentMarginTop = 40,
            ContentMarginBottom = 40
        };
        panelStyle.CornerRadiusTopLeft = 10;
        panelStyle.CornerRadiusTopRight = 10;
        panelStyle.CornerRadiusBottomLeft = 10;
        panelStyle.CornerRadiusBottomRight = 10;
        gameOverPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _emailDetailContainer.AddChild(gameOverPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);
        gameOverPanel.AddChild(vbox);

        var icon = new Label { Text = isRetirement ? "🏆" : "📋" };
        icon.AddThemeFontSizeOverride("font_size", 48);
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(icon);

        var title = new Label { Text = isRetirement ? "You Retired Successfully!" : "Game Over" };
        title.AddThemeFontSizeOverride("font_size", 20);
        title.Modulate = accentColor;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var hint = new Label
        {
            Text = "Select an email from the inbox to read the details\nof your tenure, or start a new game.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        hint.Modulate = new Color(0.5f, 0.5f, 0.6f);
        vbox.AddChild(hint);

        // Add buttons
        var buttonContainer = new HBoxContainer();
        buttonContainer.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(buttonContainer);

        // Spacer
        var spacerLeft = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buttonContainer.AddChild(spacerLeft);

        // Show Results button
        var showResultsButton = new Button { Text = "Show Final Score" };
        showResultsButton.Pressed += () =>
        {
            var endScreen = GetNodeOrNull<EndScreen>("/root/Main/EndScreen");
            endScreen?.ShowPopup();
        };
        buttonContainer.AddChild(showResultsButton);

        // New Game button
        var newGameButton = new Button { Text = "New Game" };
        newGameButton.Pressed += () => _gameManager?.StartNewGame();
        buttonContainer.AddChild(newGameButton);

        // Spacer
        var spacerRight = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buttonContainer.AddChild(spacerRight);
    }

    private void BuildStatusPanel(HBoxContainer parent)
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 12,
            ContentMarginBottom = 12
        };
        panelStyle.BorderWidthLeft = 1;
        panelStyle.BorderColor = new Color(0.12f, 0.12f, 0.15f);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(200, 0) };
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(panel);

        var mainVbox = new VBoxContainer();
        mainVbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(mainVbox);

        // Status section
        _statusContainer = new VBoxContainer();
        _statusContainer.AddThemeConstantOverride("separation", 8);
        mainVbox.AddChild(_statusContainer);

        var statusTitle = new Label { Text = "STATUS" };
        statusTitle.AddThemeFontSizeOverride("font_size", 10);
        statusTitle.Modulate = new Color(0.5f, 0.5f, 0.55f);
        _statusContainer.AddChild(statusTitle);

        _statusContainer.AddChild(new HSeparator());

        // Action buttons section (below status)
        var actionsPanel = new PanelContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        var actionsStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        actionsStyle.BorderWidthTop = 1;
        actionsStyle.BorderColor = new Color(0.1f, 0.1f, 0.13f);
        actionsPanel.AddThemeStyleboxOverride("panel", actionsStyle);
        mainVbox.AddChild(actionsPanel);

        _choicesContainer = new VBoxContainer();
        _choicesContainer.AddThemeConstantOverride("separation", 6);
        actionsPanel.AddChild(_choicesContainer);
    }

    private void BuildTickerPanel(VBoxContainer parent)
    {
        _tickerPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 28),
            ClipContents = true
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.06f, 0.04f),
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        style.BorderWidthTop = 1;
        style.BorderWidthBottom = 1;
        style.BorderColor = new Color(0.6f, 0.5f, 0.2f, 0.5f);
        _tickerPanel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(_tickerPanel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 0);
        _tickerPanel.AddChild(hbox);

        // "BREAKING" badge with background
        var badgePanel = new PanelContainer();
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.6f, 0.15f, 0.1f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 0,
            ContentMarginBottom = 0
        };
        badgePanel.AddThemeStyleboxOverride("panel", badgeStyle);
        hbox.AddChild(badgePanel);

        var badge = new Label
        {
            Text = "▶ BREAKING",
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.AddThemeFontSizeOverride("font_size", 10);
        badge.Modulate = new Color(1.0f, 1.0f, 1.0f);
        badgePanel.AddChild(badge);

        // Scroll container - clips content and contains the scrolling label
        _tickerScrollContainer = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, 20)
        };
        hbox.AddChild(_tickerScrollContainer);

        // Scrolling ticker label - positioned absolutely, starts off-screen right
        _tickerLabel = new Label
        {
            Text = "Initializing corporate metrics...",
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = false,
            AutowrapMode = TextServer.AutowrapMode.Off
        };
        _tickerLabel.AddThemeFontSizeOverride("font_size", 11);
        _tickerLabel.Modulate = new Color(0.95f, 0.85f, 0.6f);
        _tickerScrollContainer.AddChild(_tickerLabel);

        // Initialize silly KPIs
        InitializeSillyKPIs();

        // Start ticker updates
        QueueTickerEvents(5);

        // Start the first scroll after a short delay
        CallDeferred(nameof(StartTickerScroll));
    }

    private void StartTickerScroll()
    {
        if (_tickerLabel is null || _tickerScrollContainer is null) return;

        // Position label at right edge
        var containerWidth = _tickerScrollContainer.Size.X;
        _tickerLabel.Position = new Vector2(containerWidth + 20, 0);

        // Start scrolling the first headline
        UpdateTicker();
    }

    private void InitializeSillyKPIs()
    {
        var rng = new SeededRng(DateTime.Now.Millisecond);
        for (int i = 0; i < SillyKPIs.KPIDefinitions.Length; i++)
        {
            _sillyKPIs[i] = rng.NextInt(40, 80);
        }
    }

    private void QueueTickerEvents(int count)
    {
        var rng = new SeededRng(DateTime.Now.Millisecond + _tickerIndex);
        for (int i = 0; i < count; i++)
        {
            var eventIndex = rng.NextInt(0, SillyKPIs.TickerEvents.Length);
            var evt = SillyKPIs.TickerEvents[eventIndex];
            var kpi = SillyKPIs.GetKPI(evt.KpiIndex);
            var sign = evt.Delta >= 0 ? "▲" : "▼";
            var color = evt.Delta >= 0 ? "98fb98" : "ff6b6b";

            // Update the KPI value
            if (_sillyKPIs.ContainsKey(evt.KpiIndex))
            {
                _sillyKPIs[evt.KpiIndex] = Math.Clamp(_sillyKPIs[evt.KpiIndex] + evt.Delta, 0, 100);
            }

            var headline = $"{evt.Headline}  [color=#{color}]{sign} {kpi.Name}: {Math.Abs(evt.Delta)} {kpi.Unit}[/color]";
            _tickerQueue.Add(headline);
        }
    }

    private void UpdateTicker()
    {
        if (_tickerLabel is null || _tickerScrollContainer is null || _tickerQueue.Count == 0) return;
        if (_tickerScrolling) return; // Don't interrupt an ongoing scroll

        // Get next headline
        var headline = _tickerQueue[_tickerIndex % _tickerQueue.Count];
        _tickerIndex++;

        // Queue more events if running low
        if (_tickerIndex >= _tickerQueue.Count - 2)
        {
            QueueTickerEvents(3);
        }

        // Start scrolling animation
        ScrollTickerHeadline(headline);
    }

    private void ScrollTickerHeadline(string newText)
    {
        if (_tickerLabel is null || _tickerScrollContainer is null) return;

        _tickerScrolling = true;

        // Set the new text
        _tickerLabel.Text = StripBBCode(newText);

        // Get the text width (need to wait for layout)
        _tickerLabel.ResetSize();

        // Get container and text dimensions
        var containerWidth = _tickerScrollContainer.Size.X;
        var textWidth = _tickerLabel.Size.X;

        // Start position: just off the right edge
        var startX = containerWidth + 20;
        // End position: completely off the left edge
        var endX = -textWidth - 20;

        // Position at start
        _tickerLabel.Position = new Vector2(startX, 2);
        _tickerLabel.Modulate = new Color(0.95f, 0.85f, 0.6f, 1.0f);

        // Calculate scroll duration based on distance (pixels per second)
        var distance = startX - endX;
        var pixelsPerSecond = 100.0f; // Adjust for scroll speed
        var duration = distance / pixelsPerSecond;

        // Kill any existing tween
        _tickerTween?.Kill();
        _tickerTween = _tickerLabel.CreateTween();

        // Scroll from right to left (no ease for constant speed)
        _tickerTween.TweenProperty(_tickerLabel, "position:x", endX, duration);

        // When done, start the next headline
        _tickerTween.TweenCallback(Callable.From(() =>
        {
            _tickerScrolling = false;
            UpdateTicker();
        }));
    }

    private static string StripBBCode(string text)
    {
        // Simple BBCode stripper for Label (not RichTextLabel)
        return System.Text.RegularExpressions.Regex.Replace(text, @"\[/?color[^\]]*\]", "");
    }

    public override void _Process(double delta)
    {
        // Ticker now scrolls continuously - no timer needed

        // Background crisis timer - always running for maximum chaos!
        _crisisTimer += delta;
        if (_crisisTimer >= CrisisCheckInterval)
        {
            _crisisTimer = 0;
            TryTriggerRandomCrisis();
        }
    }

    private void TryTriggerRandomCrisis()
    {
        if (_gameManager is null) return;
        var state = _gameManager.CurrentState;
        if (state is null) return;

        // Don't trigger during Resolution or GameOver
        if (state.Quarter.Phase == GamePhase.Resolution) return;
        if (state.CEO.IsOusted || state.CEO.HasRetired) return;

        // Don't trigger if we already have an active crisis
        if (state.CurrentCrisis is not null) return;

        // Roll the dice
        var rng = new SeededRng(DateTime.Now.Millisecond + state.Quarter.QuarterNumber * 1000);
        var roll = rng.NextInt(1, 101);

        if (roll <= CrisisChance)
        {
            // Trigger a random crisis!
            GD.Print($"[CrisisTimer] CRISIS TRIGGERED! Roll: {roll} <= {CrisisChance}%");
            _gameManager.TriggerRandomCrisis();
        }
    }

    private void BuildHandPanel(VBoxContainer parent)
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderColor = new Color(0.2f, 0.25f, 0.35f);

        _handPanel = new PanelContainer { CustomMinimumSize = new Vector2(0, 190) };
        _handPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _handPanel.ClipContents = true;
        parent.AddChild(_handPanel);

        // Start hidden (will animate in during project phase)
        _handPanel.Modulate = new Color(1, 1, 1, 0);
        _handPanel.CustomMinimumSize = new Vector2(0, 0);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        _handPanel.AddChild(vbox);

        // Header with icon
        var headerHbox = new HBoxContainer();
        headerHbox.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(headerHbox);

        var handIcon = new Label { Text = "📋" };
        handIcon.AddThemeFontSizeOverride("font_size", 14);
        headerHbox.AddChild(handIcon);

        _handInfo = new Label { Text = "YOUR PROJECTS" };
        _handInfo.AddThemeFontSizeOverride("font_size", 12);
        _handInfo.Modulate = new Color(0.7f, 0.7f, 0.75f);
        _handInfo.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerHbox.AddChild(_handInfo);

        var scroll = new ScrollContainer
        {
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        vbox.AddChild(scroll);

        _handContainer = new HBoxContainer();
        _handContainer.AddThemeConstantOverride("separation", 12);
        _handContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(_handContainer);
    }

    private void UpdateDashboard()
    {
        if (_gameManager?.CurrentState is null) return;

        var state = _gameManager.CurrentState;

        // Update header
        _quarterLabel!.Text = state.Quarter.FormattedQuarter;
        _phaseLabel!.Text = GetPhaseDisplayName(state.Quarter.Phase);

        // Update PC with animation if changed
        var currentPC = state.Resources.PoliticalCapital;
        _pcLabel!.Text = currentPC.ToString();

        if (_lastPCValue >= 0 && _lastPCValue != currentPC)
        {
            AnimatePCChange(_lastPCValue, currentPC);
        }
        _lastPCValue = currentPC;

        // Update Evil Score
        var evilScore = state.CEO.EvilScore;
        _evilLabel!.Text = evilScore.ToString();

        // Color code evil score based on severity
        _evilLabel.Modulate = evilScore switch
        {
            >= 20 => new Color(1.0f, 0.2f, 0.2f), // Bright red - dangerous
            >= 10 => new Color(1.0f, 0.5f, 0.3f), // Orange - concerning
            >= 5 => new Color(1.0f, 0.6f, 0.4f),  // Light orange - noticeable
            _ => new Color(0.8f, 0.6f, 0.6f)       // Dim - low evil
        };

        // Update Accumulated Bonus
        var accumulatedBonus = state.CEO.AccumulatedBonus;
        _bonusLabel!.Text = $"${accumulatedBonus}M";

        // Color code based on retirement progress
        _bonusLabel.Modulate = accumulatedBonus >= CEOState.RetirementThreshold
            ? new Color(0.4f, 1.0f, 0.5f) // Bright green - can retire!
            : accumulatedBonus >= CEOState.RetirementThreshold / 2
                ? new Color(0.6f, 0.9f, 0.5f) // Light green - halfway
                : new Color(0.5f, 0.7f, 0.5f); // Dim green - early progress

        // Update Board Favor
        var boardFavor = state.CEO.BoardFavorability;
        _favorLabel!.Text = boardFavor.ToString();

        // Color code based on danger level
        _favorLabel.Modulate = boardFavor switch
        {
            >= 70 => new Color(0.4f, 0.9f, 0.5f),  // Green - excellent standing
            >= 50 => new Color(0.5f, 0.7f, 1.0f),  // Blue - good standing
            >= 40 => new Color(0.9f, 0.8f, 0.4f),  // Yellow - caution
            >= 20 => new Color(1.0f, 0.5f, 0.3f),  // Orange - danger
            _ => new Color(1.0f, 0.3f, 0.3f)       // Red - critical
        };

        // Update Projects Implemented
        var totalProjects = state.CEO.TotalCardsPlayed;
        _projectsLabel!.Text = totalProjects.ToString();

        // Color code based on activity level
        _projectsLabel.Modulate = totalProjects switch
        {
            >= 20 => new Color(0.9f, 0.7f, 1.0f),  // Bright purple - very active
            >= 10 => new Color(0.8f, 0.6f, 0.9f),  // Medium purple - active
            >= 5 => new Color(0.7f, 0.5f, 0.8f),   // Dim purple - some activity
            _ => new Color(0.5f, 0.4f, 0.6f)       // Very dim - inactive
        };

        UpdateMetrics(state);
        UpdateStatus(state);
        UpdateInbox(state);
        UpdateAction(state);
        UpdateHand(state);

        // Show hand panel only during PlayCards phase
        SetHandPanelVisible(state.Quarter.Phase == GamePhase.PlayCards);
    }

    private void UpdateMetrics(QuarterGameState state)
    {
        if (_metricsContainer is null) return;

        // Clear existing meters
        while (_metricsContainer.GetChildCount() > 2)
        {
            var child = _metricsContainer.GetChild(2);
            _metricsContainer.RemoveChild(child);
            child.QueueFree();
        }

        CreateMeterDisplay("Delivery", state.Org.Delivery, new Color(0.3f, 0.7f, 0.9f),
            "Product velocity and execution speed. Low delivery means missed deadlines and angry stakeholders.");
        CreateMeterDisplay("Morale", state.Org.Morale, new Color(0.9f, 0.7f, 0.3f),
            "Employee happiness and motivation. Low morale leads to resignations and quiet quitting.");
        CreateMeterDisplay("Governance", state.Org.Governance, new Color(0.7f, 0.5f, 0.8f),
            "Regulatory compliance and risk management. Low governance invites lawsuits and fines.");
        CreateMeterDisplay("Alignment", state.Org.Alignment, new Color(0.5f, 0.8f, 0.5f),
            "Strategic coherence across teams. Low alignment means everyone's building different products.");
        CreateMeterDisplay("Runway", state.Org.Runway, new Color(0.9f, 0.4f, 0.4f),
            "Financial health and cash reserves. Low runway means you're months away from bankruptcy.");

        // Quarterly Revenue indicator (from Revenue projects)
        var quarterlyRevenue = state.CEO.CurrentQuarterProfit;
        if (quarterlyRevenue != 0 || state.Quarter.Phase == GamePhase.PlayCards)
        {
            _metricsContainer.AddChild(new HSeparator());

            var revenueRow = new HBoxContainer();
            var revenueLabel = new Label
            {
                Text = "💰 Project Revenue",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            revenueLabel.AddThemeFontSizeOverride("font_size", 12);
            revenueLabel.Modulate = new Color(1.0f, 0.85f, 0.2f);
            revenueRow.AddChild(revenueLabel);

            var sign = quarterlyRevenue >= 0 ? "+" : "";
            var revenueValue = new Label { Text = $"{sign}${quarterlyRevenue}M" };
            revenueValue.AddThemeFontSizeOverride("font_size", 12);
            revenueValue.Modulate = quarterlyRevenue >= 0
                ? new Color(0.5f, 0.9f, 0.5f)
                : new Color(0.9f, 0.5f, 0.5f);
            revenueRow.AddChild(revenueValue);

            revenueRow.TooltipText = "Revenue from Revenue projects this quarter.\nOnly Revenue cards affect profit.\nStandard projects affect organizational metrics only.";
            revenueRow.MouseFilter = Control.MouseFilterEnum.Stop;
            _metricsContainer.AddChild(revenueRow);
        }

        // Crisis indicator
        if (state.Crises.ActiveCount > 0)
        {
            var crisisLabel = new Label
            {
                Text = $"Active Crises: {state.Crises.ActiveCount}"
            };
            crisisLabel.AddThemeFontSizeOverride("font_size", 11);
            crisisLabel.Modulate = new Color(0.9f, 0.4f, 0.4f);
            _metricsContainer.AddChild(crisisLabel);
        }
    }

    private void CreateMeterDisplay(string name, int value, Color color, string tooltip = "")
    {
        if (_metricsContainer is null) return;

        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 3);
        if (!string.IsNullOrEmpty(tooltip))
        {
            row.TooltipText = tooltip;
            row.MouseFilter = Control.MouseFilterEnum.Stop;
        }

        var nameRow = new HBoxContainer();
        var nameLabel = new Label
        {
            Text = name,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameRow.AddChild(nameLabel);

        // Add info icon hint for tooltip
        if (!string.IsNullOrEmpty(tooltip))
        {
            var infoHint = new Label { Text = "?" };
            infoHint.AddThemeFontSizeOverride("font_size", 10);
            infoHint.Modulate = new Color(0.4f, 0.4f, 0.5f);
            nameRow.AddChild(infoHint);
        }

        var valueLabel = new Label { Text = value.ToString() };
        valueLabel.AddThemeFontSizeOverride("font_size", 12);
        valueLabel.Modulate = GetValueColor(value);
        nameRow.AddChild(valueLabel);
        row.AddChild(nameRow);

        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 6),
            MaxValue = 100,
            Value = value,
            ShowPercentage = false
        };

        var bgStyle = new StyleBoxFlat { BgColor = color * 0.2f };
        bgStyle.CornerRadiusTopLeft = 2;
        bgStyle.CornerRadiusTopRight = 2;
        bgStyle.CornerRadiusBottomLeft = 2;
        bgStyle.CornerRadiusBottomRight = 2;
        bar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat { BgColor = color };
        fillStyle.CornerRadiusTopLeft = 2;
        fillStyle.CornerRadiusTopRight = 2;
        fillStyle.CornerRadiusBottomLeft = 2;
        fillStyle.CornerRadiusBottomRight = 2;
        bar.AddThemeStyleboxOverride("fill", fillStyle);

        row.AddChild(bar);
        _metricsContainer.AddChild(row);
    }

    private void UpdateStatus(QuarterGameState state)
    {
        if (_statusContainer is null) return;

        // Clear existing status
        while (_statusContainer.GetChildCount() > 2)
        {
            var child = _statusContainer.GetChild(2);
            _statusContainer.RemoveChild(child);
            child.QueueFree();
        }

        AddStatusItem("Quarters Survived", state.CEO.QuartersSurvived.ToString());
        AddStatusItem("Board Favorability", $"{state.CEO.BoardFavorability}%");
        AddStatusItem("Board Pressure", $"Level {state.CEO.BoardPressureLevel}");
        AddStatusItem("Total Profit", $"${state.CEO.TotalProfit}M");

        if (state.CEO.EvilScore > 0)
        {
            AddStatusItem("Corporate Score", state.CEO.EvilScore.ToString(), new Color(0.7f, 0.3f, 0.3f));
        }

        // Current directive
        if (state.CurrentDirective is not null)
        {
            _statusContainer.AddChild(new HSeparator());
            var directiveLabel = new Label { Text = "BOARD DIRECTIVE" };
            directiveLabel.AddThemeFontSizeOverride("font_size", 10);
            directiveLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
            _statusContainer.AddChild(directiveLabel);

            var directiveText = new Label
            {
                Text = state.CurrentDirective.GetDescription(state.CEO.BoardPressureLevel),
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            directiveText.AddThemeFontSizeOverride("font_size", 11);
            directiveText.Modulate = new Color(0.9f, 0.7f, 0.3f);
            _statusContainer.AddChild(directiveText);
        }
    }

    private void AddStatusItem(string label, string value, Color? valueColor = null)
    {
        if (_statusContainer is null) return;

        var row = new HBoxContainer();

        var labelNode = new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        labelNode.AddThemeFontSizeOverride("font_size", 11);
        labelNode.Modulate = new Color(0.6f, 0.6f, 0.65f);
        row.AddChild(labelNode);

        var valueNode = new Label { Text = value };
        valueNode.AddThemeFontSizeOverride("font_size", 12);
        valueNode.Modulate = valueColor ?? new Color(0.85f, 0.85f, 0.9f);
        row.AddChild(valueNode);

        _statusContainer.AddChild(row);
    }

    private void UpdateInbox(QuarterGameState state)
    {
        if (_inboxContainer is null) return;

        // Clear existing threads
        foreach (var child in _inboxContainer.GetChildren())
        {
            child.QueueFree();
        }

        var inbox = state.Inbox;
        // All threads - high priority unread pop to top, then normal ordering
        var threads = inbox.AllThreadsOrdered
            .OrderByDescending(t => t.IsHighPriority && !t.IsFullyRead)  // High priority unread first (response required)
            .ThenByDescending(t => !t.IsFullyRead)  // Then other unread
            .ThenByDescending(t => t.SequenceNumber)  // Then by most recent activity
            .ToList();

        // Clear selected thread if it no longer exists in the inbox (e.g., after game reset)
        if (_selectedThread is not null && !threads.Any(t => t.ThreadId == _selectedThread.ThreadId))
        {
            _selectedThread = null;
            ClearEmailDetail();
        }

        if (threads.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "No messages yet.",
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            emptyLabel.AddThemeFontSizeOverride("font_size", 10);
            emptyLabel.Modulate = new Color(0.4f, 0.4f, 0.45f);
            _inboxContainer.AddChild(emptyLabel);
            return;
        }

        // Show unread count if any
        if (inbox.UnreadThreadCount > 0)
        {
            var unreadLabel = new Label
            {
                Text = $"{inbox.UnreadThreadCount} unread"
            };
            unreadLabel.AddThemeFontSizeOverride("font_size", 9);
            unreadLabel.Modulate = new Color(0.9f, 0.6f, 0.3f);
            _inboxContainer.AddChild(unreadLabel);
        }

        // Auto-select first high-priority unread email (only if nothing selected)
        // Don't steal focus from current email when crisis triggers
        var priorityThread = threads.FirstOrDefault(t => t.IsHighPriority && !t.IsFullyRead);
        if (priorityThread is not null && _selectedThread is null)
        {
            // ShowEmailThread calls UpdateInbox at the end, so return to avoid double-rendering
            ShowEmailThread(priorityThread);
            return;
        }
        else if (_selectedThread is not null && !_isShowingEmailThread)
        {
            // Always refresh selected thread to ensure crisis response panels update correctly
            // This handles cases where CurrentCrisis changes but the thread itself doesn't
            // Only do this if we're not already in ShowEmailThread (to avoid recursion)
            var updatedThread = threads.FirstOrDefault(t => t.ThreadId == _selectedThread.ThreadId);
            if (updatedThread is not null)
            {
                _selectedThread = updatedThread;
                ShowEmailThread(updatedThread);
                return;
            }
        }

        var delay = 0.0f;
        foreach (var thread in threads)
        {
            var threadPanel = CreateEmailThreadPanel(thread);
            _inboxContainer.AddChild(threadPanel);

            // Staggered fade-in animation for email threads
            threadPanel.Modulate = new Color(1, 1, 1, 0);
            var panelCopy = threadPanel;
            var delayTime = delay;
            GetTree().CreateTimer(delayTime).Timeout += () =>
            {
                if (IsInstanceValid(panelCopy))
                {
                    AnimateFadeIn(panelCopy, 0.2f, slideFromRight: true);
                }
            };
            delay += 0.05f; // Stagger each thread by 50ms
        }
    }

    private Control CreateEmailThreadPanel(EmailThread thread)
    {
        var isUnread = !thread.IsFullyRead;
        var isSelected = _selectedThread?.ThreadId == thread.ThreadId;
        var latest = thread.LatestMessage;

        // Dynamic height based on state: Selected=120, Unread=72, Read=48
        float panelHeight = isSelected ? 120f : (isUnread ? 72f : 48f);

        // Use a Button as the container for proper click handling
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(0, panelHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        // Type-based coloring
        var typeColor = thread.ThreadType switch
        {
            EmailThreadType.Crisis => new Color(0.25f, 0.1f, 0.1f),
            EmailThreadType.BoardDirective => new Color(0.2f, 0.15f, 0.08f),
            EmailThreadType.CardResult => new Color(0.08f, 0.12f, 0.18f),
            _ => new Color(0.08f, 0.08f, 0.1f)
        };

        // Background multiplier: Selected=1.5, Unread=1.2, Read=0.6 (darker)
        var bgMultiplier = isSelected ? 1.5f : (isUnread ? 1.2f : 0.6f);
        var bgColor = typeColor * bgMultiplier;

        var accentColor = thread.ThreadType switch
        {
            EmailThreadType.Crisis => new Color(0.9f, 0.3f, 0.3f),
            EmailThreadType.BoardDirective => new Color(0.9f, 0.7f, 0.3f),
            EmailThreadType.CardResult => new Color(0.4f, 0.7f, 0.9f),
            _ => new Color(0.5f, 0.5f, 0.6f)
        };

        // Corner radius: Unread/Selected=6, Read=3 (less prominent)
        int cornerRadius = (isUnread || isSelected) ? 6 : 3;

        // Vertical padding: smaller for read emails
        int verticalPadding = (isUnread || isSelected) ? 10 : 6;

        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 16,
            ContentMarginRight = 14,
            ContentMarginTop = verticalPadding,
            ContentMarginBottom = verticalPadding
        };
        style.CornerRadiusTopLeft = cornerRadius;
        style.CornerRadiusTopRight = cornerRadius;
        style.CornerRadiusBottomLeft = cornerRadius;
        style.CornerRadiusBottomRight = cornerRadius;

        if (isUnread || isSelected)
        {
            style.BorderWidthLeft = isSelected ? 5 : 4;  // Thicker border for selected
            style.BorderColor = isSelected ? new Color(0.5f, 0.7f, 1.0f) : accentColor;
            style.ContentMarginLeft = 12;  // Account for border
        }

        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = bgColor * 1.3f,
            ContentMarginLeft = 16,
            ContentMarginRight = 14,
            ContentMarginTop = verticalPadding,
            ContentMarginBottom = verticalPadding
        };
        hoverStyle.CornerRadiusTopLeft = cornerRadius;
        hoverStyle.CornerRadiusTopRight = cornerRadius;
        hoverStyle.CornerRadiusBottomLeft = cornerRadius;
        hoverStyle.CornerRadiusBottomRight = cornerRadius;
        hoverStyle.BorderWidthLeft = 4;
        hoverStyle.BorderColor = accentColor;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);

        // Apply strong opacity fade for read emails (50%)
        if (!isUnread && !isSelected)
        {
            btn.Modulate = new Color(1f, 1f, 1f, 0.50f);
        }

        // Add pulsing glow animation for high-priority unread threads (crisis/situations)
        if (thread.IsHighPriority && isUnread)
        {
            StartPulsingGlow(btn, accentColor);
        }

        // Content overlay with proper padding offsets
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 16;
        vbox.OffsetRight = -14;
        vbox.OffsetTop = verticalPadding;
        vbox.OffsetBottom = -verticalPadding;
        vbox.AddThemeConstantOverride("separation", 3);
        vbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        btn.AddChild(vbox);

        // Header row: Type badge + message count + unread indicator
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 6);
        headerRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        vbox.AddChild(headerRow);

        // High priority flag (for crisis emails)
        if (thread.IsHighPriority)
        {
            var priorityFlag = new Label { Text = "❗" };
            priorityFlag.AddThemeFontSizeOverride("font_size", 12);
            priorityFlag.Modulate = new Color(1.0f, 0.3f, 0.3f);
            priorityFlag.MouseFilter = Control.MouseFilterEnum.Ignore;
            headerRow.AddChild(priorityFlag);

            // Animate priority flag if unread
            if (!thread.IsFullyRead)
            {
                priorityFlag.Ready += () => AnimateUnreadPulse(priorityFlag);
            }
        }

        // Type badge
        var typeBadge = new Label
        {
            Text = thread.ThreadType switch
            {
                EmailThreadType.Crisis => "URGENT",
                EmailThreadType.BoardDirective => "DIRECTIVE",
                EmailThreadType.CardResult => "PROJECT",
                _ => "MESSAGE"
            }
        };
        typeBadge.AddThemeFontSizeOverride("font_size", 9);
        typeBadge.Modulate = accentColor;
        typeBadge.MouseFilter = Control.MouseFilterEnum.Ignore;
        headerRow.AddChild(typeBadge);

        // "Needs Response" indicator for pending crisis threads
        var needsResponse = thread.ThreadType == EmailThreadType.Crisis &&
            _gameManager?.CurrentState?.CurrentCrisis is not null &&
            thread.OriginatingCardId == _gameManager.CurrentState.CurrentCrisis.EventId;

        if (needsResponse)
        {
            var responseIndicator = new Label
            {
                Text = "⚡ RESPONSE REQUIRED",
                Name = "ResponseRequired"
            };
            responseIndicator.AddThemeFontSizeOverride("font_size", 9);
            responseIndicator.Modulate = new Color(1.0f, 0.8f, 0.2f);  // Yellow/gold for action needed
            responseIndicator.MouseFilter = Control.MouseFilterEnum.Ignore;
            headerRow.AddChild(responseIndicator);

            // Animate the response indicator with a pulsing effect
            responseIndicator.Ready += () => AnimateUnreadPulse(responseIndicator);
        }

        // Message count badge (if more than 1 message)
        if (thread.Messages.Count > 1)
        {
            var countBadge = new Label
            {
                Text = $"({thread.Messages.Count})"
            };
            countBadge.AddThemeFontSizeOverride("font_size", 9);
            countBadge.Modulate = new Color(0.5f, 0.5f, 0.55f);
            countBadge.MouseFilter = Control.MouseFilterEnum.Ignore;
            headerRow.AddChild(countBadge);
        }

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        headerRow.AddChild(spacer);

        // Unread indicator with pulsing animation
        if (isUnread)
        {
            var dot = new Label { Text = "●", Name = "UnreadDot" };
            dot.AddThemeFontSizeOverride("font_size", 10);
            dot.Modulate = accentColor;
            dot.MouseFilter = Control.MouseFilterEnum.Ignore;
            headerRow.AddChild(dot);

            // Set up pulsing animation when the control enters the tree
            dot.Ready += () => AnimateUnreadPulse(dot);
        }

        // Subject line - font size varies by state
        int subjectFontSize = (isUnread || isSelected) ? 13 : 11;
        var subject = new Label
        {
            Text = thread.Subject,
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        subject.AddThemeFontSizeOverride("font_size", subjectFontSize);
        subject.Modulate = isUnread ? new Color(0.95f, 0.95f, 1.0f) : new Color(0.75f, 0.75f, 0.8f);
        vbox.AddChild(subject);

        // From line - only show for unread or selected (hidden for read)
        if ((isUnread || isSelected) && latest is not null)
        {
            var fromLabel = new Label
            {
                Text = latest.FromDisplay,
                ClipText = true,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            fromLabel.AddThemeFontSizeOverride("font_size", 10);
            fromLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
            vbox.AddChild(fromLabel);
        }

        // Preview text - only for selected emails (show first unread message, or latest if all read)
        if (isSelected)
        {
            var previewMsg = thread.Messages.FirstOrDefault(m => !m.IsRead) ?? thread.LatestMessage;
            if (previewMsg is not null)
            {
                // Add a subtle separator
                var separator = new HSeparator();
                separator.Modulate = new Color(1f, 1f, 1f, 0.2f);
                separator.MouseFilter = Control.MouseFilterEnum.Ignore;
                vbox.AddChild(separator);

                // Preview text (first 80 chars)
                var previewText = previewMsg.Body;
                if (previewText.Length > 80)
                    previewText = previewText[..80] + "...";

                var preview = new Label
                {
                    Text = previewText,
                    AutowrapMode = TextServer.AutowrapMode.Word,
                    ClipText = true,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                preview.AddThemeFontSizeOverride("font_size", 10);
                preview.Modulate = new Color(0.6f, 0.6f, 0.65f);
                vbox.AddChild(preview);
            }
        }

        // Click handler
        var threadCopy = thread;
        btn.Pressed += () => ShowEmailThread(threadCopy);

        return btn;
    }

    private void ShowEmailThread(EmailThread thread)
    {
        if (_emailDetailContainer is null) return;
        if (_isShowingEmailThread) return; // Prevent re-entrance

        _isShowingEmailThread = true;
        _selectedThread = thread;

        // Clear existing content
        foreach (var child in _emailDetailContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Type-based accent color
        var accentColor = thread.ThreadType switch
        {
            EmailThreadType.Crisis => new Color(0.9f, 0.4f, 0.4f),
            EmailThreadType.BoardDirective => new Color(0.9f, 0.7f, 0.3f),
            EmailThreadType.CardResult => new Color(0.4f, 0.7f, 0.9f),
            _ => new Color(0.6f, 0.6f, 0.65f)
        };

        // Header panel with subject and type
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.12f),
            ContentMarginLeft = 20,
            ContentMarginRight = 20,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        headerStyle.CornerRadiusTopLeft = 8;
        headerStyle.CornerRadiusTopRight = 8;
        headerStyle.CornerRadiusBottomLeft = 8;
        headerStyle.CornerRadiusBottomRight = 8;
        headerStyle.BorderWidthBottom = 3;
        headerStyle.BorderColor = accentColor;

        var headerPanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        _emailDetailContainer.AddChild(headerPanel);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 8);
        headerPanel.AddChild(headerVbox);

        // Type badge row
        var typeRow = new HBoxContainer();
        typeRow.AddThemeConstantOverride("separation", 8);
        headerVbox.AddChild(typeRow);

        var typeIcon = new Label
        {
            Text = thread.ThreadType switch
            {
                EmailThreadType.Crisis => "⚠",
                EmailThreadType.BoardDirective => "📋",
                EmailThreadType.CardResult => "📊",
                _ => "📧"
            }
        };
        typeIcon.AddThemeFontSizeOverride("font_size", 18);
        typeRow.AddChild(typeIcon);

        var typeLabel = new Label
        {
            Text = thread.ThreadType switch
            {
                EmailThreadType.Crisis => "SITUATION",
                EmailThreadType.BoardDirective => "BOARD DIRECTIVE",
                EmailThreadType.CardResult => "PROJECT UPDATE",
                _ => "MESSAGE"
            }
        };
        typeLabel.AddThemeFontSizeOverride("font_size", 11);
        typeLabel.Modulate = accentColor;
        typeRow.AddChild(typeLabel);

        // Subject
        var subjectLabel = new Label
        {
            Text = thread.Subject,
            AutowrapMode = TextServer.AutowrapMode.Word,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        subjectLabel.AddThemeFontSizeOverride("font_size", 18);
        subjectLabel.Modulate = new Color(0.95f, 0.95f, 1.0f);
        headerVbox.AddChild(subjectLabel);

        // Messages
        foreach (var msg in thread.Messages)
        {
            var msgPanel = CreateEmailMessagePanel(msg);
            _emailDetailContainer.AddChild(msgPanel);
        }

        // For crisis emails, add response buttons directly in the email
        // Show response panel if this is the crisis thread for the current pending crisis
        if (thread.ThreadType == EmailThreadType.Crisis &&
            _gameManager?.CurrentState?.CurrentCrisis is not null &&
            thread.OriginatingCardId == _gameManager.CurrentState.CurrentCrisis.EventId)
        {
            var crisis = _gameManager.CurrentState.CurrentCrisis;
            var state = _gameManager.CurrentState;

            // Create response panel (works for both regular Crisis phase and interrupt crises)
            var responsePanel = CreateCrisisResponsePanel(crisis, state, accentColor);
            _emailDetailContainer.AddChild(responsePanel);
        }

        // Mark thread as read and refresh inbox list
        if (!thread.IsFullyRead)
        {
            _gameManager?.MarkThreadRead(thread.ThreadId);
        }

        // Update inbox to show selection
        UpdateInbox(_gameManager?.CurrentState!);

        _isShowingEmailThread = false; // Allow future calls
    }

    private Control CreateCrisisResponsePanel(EventCard crisis, QuarterGameState state, Color accentColor)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.08f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 16,
            ContentMarginBottom = 16
        };
        style.BorderWidthTop = 2;
        style.BorderColor = accentColor;
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        panel.AddChild(vbox);

        // Header
        var header = new Label
        {
            Text = "RESPONSE REQUIRED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeFontSizeOverride("font_size", 12);
        header.Modulate = accentColor;
        vbox.AddChild(header);

        // Response buttons
        foreach (var choice in crisis.Choices)
        {
            var choicePanel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var choiceStyle = new StyleBoxFlat
            {
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 10,
                ContentMarginBottom = 10
            };
            choiceStyle.CornerRadiusTopLeft = 4;
            choiceStyle.CornerRadiusTopRight = 4;
            choiceStyle.CornerRadiusBottomLeft = 4;
            choiceStyle.CornerRadiusBottomRight = 4;

            // Style based on choice type
            if (choice.HasPCCost)
            {
                choiceStyle.BgColor = new Color(0.1f, 0.15f, 0.2f);
                choiceStyle.BorderColor = new Color(0.3f, 0.5f, 0.7f);
                choiceStyle.BorderWidthLeft = 3;
            }
            else if (choice.IsCorporateChoice)
            {
                choiceStyle.BgColor = new Color(0.2f, 0.1f, 0.15f);
                choiceStyle.BorderColor = new Color(0.7f, 0.3f, 0.5f);
                choiceStyle.BorderWidthLeft = 3;
            }
            else
            {
                choiceStyle.BgColor = new Color(0.12f, 0.12f, 0.14f);
            }
            choicePanel.AddThemeStyleboxOverride("panel", choiceStyle);

            var choiceVbox = new VBoxContainer();
            choiceVbox.AddThemeConstantOverride("separation", 4);
            choicePanel.AddChild(choiceVbox);

            // Choice type indicator
            var typeLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
            typeLabel.AddThemeFontSizeOverride("font_size", 10);

            bool canAfford = true;
            if (choice.HasPCCost)
            {
                canAfford = state.Resources.PoliticalCapital >= choice.PCCost;
                typeLabel.Text = $"[INVEST {choice.PCCost} PC] 70% Success / 10% Failure";
                typeLabel.Modulate = canAfford ? new Color(0.4f, 0.7f, 0.9f) : new Color(0.5f, 0.5f, 0.5f);
                if (!canAfford) typeLabel.Text += " (Can't Afford)";
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
            choiceVbox.AddChild(typeLabel);

            // Main choice button
            var btn = new Button
            {
                Text = choice.Label,
                Disabled = !canAfford
            };

            var choiceId = choice.ChoiceId;
            btn.Pressed += () =>
            {
                // Use appropriate method based on current phase
                if (_gameManager?.CurrentState?.Quarter.Phase == GamePhase.Crisis)
                {
                    _gameManager?.MakeCrisisChoice(choiceId);
                }
                else
                {
                    // Interrupt crisis during PlayCards or BoardDemand - use pending crisis handler
                    _gameManager?.RespondToPendingCrisis(choiceId);
                }
            };
            choiceVbox.AddChild(btn);

            // Expected outcome
            if (choice.HasTieredOutcomes && choice.OutcomeProfile is not null)
            {
                var expectedEffects = choice.OutcomeProfile.Expected;
                if (expectedEffects.Count > 0)
                {
                    var forecastLabel = new Label
                    {
                        Text = $"Expected: {DescribeChoiceEffects(expectedEffects)}",
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    forecastLabel.AddThemeFontSizeOverride("font_size", 10);
                    forecastLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
                    choiceVbox.AddChild(forecastLabel);
                }
            }

            vbox.AddChild(choicePanel);
        }

        return panel;
    }

    private static string DescribeChoiceEffects(IReadOnlyList<IEffect> effects)
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

    private Control CreateEmailMessagePanel(EmailMessage msg)
    {
        var isFromPlayer = msg.IsFromPlayer;

        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat
        {
            BgColor = isFromPlayer
                ? new Color(0.06f, 0.1f, 0.16f)
                : new Color(0.1f, 0.08f, 0.06f),
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        style.BorderWidthLeft = isFromPlayer ? 0 : 4;
        style.BorderWidthRight = isFromPlayer ? 4 : 0;
        style.BorderColor = isFromPlayer
            ? new Color(0.3f, 0.5f, 0.8f)
            : new Color(0.7f, 0.5f, 0.3f);
        style.CornerRadiusTopLeft = 6;
        style.CornerRadiusTopRight = 6;
        style.CornerRadiusBottomLeft = 6;
        style.CornerRadiusBottomRight = 6;
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        // Header row: From + timestamp
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(headerRow);

        var fromLabel = new Label { Text = msg.FromDisplay, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        fromLabel.AddThemeFontSizeOverride("font_size", 12);
        fromLabel.Modulate = isFromPlayer
            ? new Color(0.5f, 0.75f, 1.0f)
            : new Color(1.0f, 0.85f, 0.5f);
        headerRow.AddChild(fromLabel);

        var turnLabel = new Label { Text = QuarterState.FormatQuarter(msg.TurnNumber) };
        turnLabel.AddThemeFontSizeOverride("font_size", 10);
        turnLabel.Modulate = new Color(0.45f, 0.45f, 0.5f);
        headerRow.AddChild(turnLabel);

        // Body - using RichTextLabel for better text handling
        var bodyLabel = new RichTextLabel
        {
            Text = msg.Body,
            BbcodeEnabled = false,
            FitContent = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ScrollActive = false
        };
        bodyLabel.AddThemeFontSizeOverride("normal_font_size", 13);
        bodyLabel.AddThemeColorOverride("default_color", new Color(0.85f, 0.85f, 0.9f));
        vbox.AddChild(bodyLabel);

        return panel;
    }

    private void ClearEmailDetail()
    {
        if (_emailDetailContainer is null) return;

        foreach (var child in _emailDetailContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Show appropriate message based on game phase
        if (_gameManager?.Phase == UIPhase.GameOver)
        {
            ShowGameOverMessage();
        }
        else
        {
            ShowWelcomeMessage();
        }
    }

    private void UpdateAction(QuarterGameState state)
    {
        if (_actionContainer is null || _actionHeader is null ||
            _actionDescription is null || _choicesContainer is null) return;

        // Clear choices
        foreach (var child in _choicesContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Handle game over state - show restart button instead of normal actions
        if (_gameManager?.Phase == UIPhase.GameOver)
        {
            var isRetirement = state.CEO.HasRetired;
            _actionHeader.Text = isRetirement ? "RETIRED" : "OUSTED";
            _actionDescription.Text = isRetirement
                ? "[center][color=#80ff80]You've secured your golden parachute![/color]\n\nView the final score popup or start a new game.[/center]"
                : "[center][color=#ff8080]The board has lost confidence in your leadership.[/color]\n\nView the final score popup or start a new game.[/center]";

            // Show Final Score button to re-open popup
            var showScoreBtn = CreateActionButton("View Final Score", isRetirement ? new Color(0.4f, 1.0f, 0.5f) : Colors.Orange);
            showScoreBtn.Pressed += () =>
            {
                var endScreen = GetNodeOrNull<EndScreen>("/root/Main/EndScreen");
                endScreen?.ShowPopup();
            };
            _choicesContainer.AddChild(showScoreBtn);

            // Restart Game button
            var restartBtn = CreateActionButton("Restart Game", Colors.Cyan);
            restartBtn.Pressed += () => _gameManager?.StartNewGame();
            _choicesContainer.AddChild(restartBtn);

            return; // Don't show normal phase actions
        }

        // Phase-specific content
        switch (state.Quarter.Phase)
        {
            case GamePhase.BoardDemand:
                _actionHeader.Text = "QUARTERLY BRIEFING";
                _actionDescription.Text = state.CurrentDirective is not null
                    ? $"The board has set their directive for {state.Quarter.FormattedQuarter}:\n\n[color=#e6b84d]{state.CurrentDirective.GetDescription(state.CEO.BoardPressureLevel)}[/color]"
                    : "Preparing quarterly briefing...";

                var continueBtn = CreateActionButton("▶ BEGIN PROJECT PHASE", Colors.Cyan);
                continueBtn.Pressed += () => _gameManager?.AdvanceBoardDemand();
                _choicesContainer.AddChild(continueBtn);
                break;

            case GamePhase.Crisis:
                if (state.CurrentCrisis is not null)
                {
                    _actionHeader.Text = $"SITUATION: {state.CurrentCrisis.Title}";
                    _actionDescription.Text = "[color=#ff6666]A situation requires your attention.[/color]\n\n" +
                        "Check the urgent message in your inbox and select a response.";

                    // Check if crisis email exists in inbox (check both IsCrisis flag and OriginatingCardId match)
                    var crisisThread = state.Inbox.Threads.FirstOrDefault(t => t.IsCrisis && t.OriginatingCardId == state.CurrentCrisis.EventId) ??
                                       state.Inbox.Threads.FirstOrDefault(t => t.IsCrisis);
                    if (crisisThread is null)
                    {
                        // Crisis exists but no email - something went wrong, offer skip button
                        var skipLabel = new Label
                        {
                            Text = "Crisis email not found - click below to continue",
                            AutowrapMode = TextServer.AutowrapMode.Word
                        };
                        skipLabel.AddThemeFontSizeOverride("font_size", 10);
                        skipLabel.Modulate = new Color(1.0f, 0.6f, 0.4f);
                        _choicesContainer.AddChild(skipLabel);

                        var skipBtn = CreateActionButton("▶ CONTINUE", new Color(0.9f, 0.6f, 0.3f));
                        skipBtn.Pressed += () => _gameManager?.ForceAdvanceCrisis();
                        _choicesContainer.AddChild(skipBtn);
                    }
                    else
                    {
                        // Prominent ACTION REQUIRED banner
                        var actionBanner = new PanelContainer();
                        var bannerStyle = new StyleBoxFlat
                        {
                            BgColor = new Color(0.4f, 0.15f, 0.1f),
                            ContentMarginLeft = 12,
                            ContentMarginRight = 12,
                            ContentMarginTop = 10,
                            ContentMarginBottom = 10
                        };
                        bannerStyle.BorderWidthTop = 2;
                        bannerStyle.BorderWidthBottom = 2;
                        bannerStyle.BorderWidthLeft = 2;
                        bannerStyle.BorderWidthRight = 2;
                        bannerStyle.BorderColor = new Color(1.0f, 0.4f, 0.3f);
                        bannerStyle.CornerRadiusTopLeft = 6;
                        bannerStyle.CornerRadiusTopRight = 6;
                        bannerStyle.CornerRadiusBottomLeft = 6;
                        bannerStyle.CornerRadiusBottomRight = 6;
                        actionBanner.AddThemeStyleboxOverride("panel", bannerStyle);
                        _choicesContainer.AddChild(actionBanner);

                        var bannerContent = new VBoxContainer();
                        bannerContent.AddThemeConstantOverride("separation", 6);
                        actionBanner.AddChild(bannerContent);

                        var actionLabel = new Label { Text = "ACTION REQUIRED" };
                        actionLabel.AddThemeFontSizeOverride("font_size", 16);
                        actionLabel.Modulate = new Color(1.0f, 0.5f, 0.4f);
                        actionLabel.HorizontalAlignment = HorizontalAlignment.Center;
                        bannerContent.AddChild(actionLabel);

                        var instructionLabel = new Label
                        {
                            Text = "You have an urgent email that requires a response.\nClick the highlighted email in your inbox to read it,\nthen select one of the response options.",
                            AutowrapMode = TextServer.AutowrapMode.Word,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        instructionLabel.AddThemeFontSizeOverride("font_size", 11);
                        instructionLabel.Modulate = new Color(0.9f, 0.85f, 0.8f);
                        bannerContent.AddChild(instructionLabel);

                        // View email button
                        var viewEmailBtn = new Button
                        {
                            Text = "📧 VIEW URGENT EMAIL",
                            CustomMinimumSize = new Vector2(0, 36)
                        };
                        viewEmailBtn.AddThemeFontSizeOverride("font_size", 14);
                        var threadToShow = crisisThread;
                        viewEmailBtn.Pressed += () => ShowEmailThread(threadToShow);
                        bannerContent.AddChild(viewEmailBtn);

                        _choicesContainer.AddChild(new HSeparator());

                        // Less prominent skip option with clearer text
                        var skipHint = new Label
                        {
                            Text = "Can't decide? Let the board choose for you:",
                            AutowrapMode = TextServer.AutowrapMode.Word
                        };
                        skipHint.AddThemeFontSizeOverride("font_size", 9);
                        skipHint.Modulate = new Color(0.5f, 0.5f, 0.55f);
                        _choicesContainer.AddChild(skipHint);

                        var fallbackBtn = new Button
                        {
                            Text = "Let Board Decide (Random)",
                            TooltipText = "The board will choose a response for you.\nNot recommended - you lose control of the outcome.",
                            CustomMinimumSize = new Vector2(0, 28)
                        };
                        fallbackBtn.AddThemeFontSizeOverride("font_size", 10);
                        fallbackBtn.Modulate = new Color(0.6f, 0.6f, 0.6f);
                        fallbackBtn.Pressed += () => _gameManager?.ForceAdvanceCrisis();
                        _choicesContainer.AddChild(fallbackBtn);
                    }
                }
                else
                {
                    _actionHeader.Text = "NO SITUATIONS";
                    _actionDescription.Text = "No situations requiring attention this quarter.";

                    // No crisis - add button to continue
                    var noCrisisBtn = CreateActionButton("▶ CONTINUE TO RESOLUTION", Colors.Cyan);
                    noCrisisBtn.Pressed += () => _gameManager?.ForceAdvanceCrisis();
                    _choicesContainer.AddChild(noCrisisBtn);
                }
                break;

            case GamePhase.PlayCards:
                var cardsPlayed = state.CardsPlayedThisQuarter.Count;
                var remainingPlays = QuarterGameState.MaxCardsPerQuarter - cardsPlayed;
                var nextCost = state.GetNextCardPCCost();
                var nextRisk = state.GetNextCardRiskModifier();
                var canAfford = state.CanAffordNextCard;
                var restraintBonus = ResourceState.CalculateRestraintBonus(cardsPlayed);

                // Show project phase info in action area
                var infoLabel = new Label { Text = "PROJECT EXECUTION" };
                infoLabel.AddThemeFontSizeOverride("font_size", 12);
                infoLabel.Modulate = new Color(0.7f, 0.7f, 0.75f);
                _choicesContainer.AddChild(infoLabel);

                // Cards played this quarter
                var playedLabel = new Label
                {
                    Text = $"Cards played: {cardsPlayed}/{QuarterGameState.MaxCardsPerQuarter}",
                    AutowrapMode = TextServer.AutowrapMode.Word
                };
                playedLabel.AddThemeFontSizeOverride("font_size", 11);
                playedLabel.Modulate = new Color(0.6f, 0.6f, 0.65f);
                _choicesContainer.AddChild(playedLabel);

                if (remainingPlays > 0 && canAfford)
                {
                    // Next card cost/risk info
                    if (nextCost > 0 || nextRisk > 0)
                    {
                        var costLabel = new Label
                        {
                            Text = $"Next card: {nextCost} PC, +{nextRisk}% risk",
                            AutowrapMode = TextServer.AutowrapMode.Word
                        };
                        costLabel.AddThemeFontSizeOverride("font_size", 10);
                        costLabel.Modulate = new Color(0.9f, 0.7f, 0.3f);
                        _choicesContainer.AddChild(costLabel);
                    }

                    var instructionLabel = new Label
                    {
                        Text = "Click a project card to execute it",
                        AutowrapMode = TextServer.AutowrapMode.Word
                    };
                    instructionLabel.AddThemeFontSizeOverride("font_size", 10);
                    instructionLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
                    _choicesContainer.AddChild(instructionLabel);
                }
                else if (!canAfford && remainingPlays > 0)
                {
                    var cantAffordLabel = new Label
                    {
                        Text = $"Not enough PC for next card (need {nextCost})",
                        AutowrapMode = TextServer.AutowrapMode.Word
                    };
                    cantAffordLabel.AddThemeFontSizeOverride("font_size", 10);
                    cantAffordLabel.Modulate = new Color(0.9f, 0.4f, 0.4f);
                    _choicesContainer.AddChild(cantAffordLabel);
                }

                _choicesContainer.AddChild(new HSeparator());

                // Check for pending crises that must be resolved first
                var hasPendingCrisis = _gameManager?.HasPendingCrisis ?? false;

                // End phase button with restraint bonus preview
                var endText = restraintBonus > 0
                    ? $"▶ END PROJECT PHASE (+{restraintBonus} PC)"
                    : "▶ END PROJECT PHASE";

                if (hasPendingCrisis)
                {
                    // Show blocked button and warning
                    var blockedBtn = CreateActionButton("⚠ RESOLVE SITUATION FIRST", new Color(0.5f, 0.5f, 0.5f));
                    blockedBtn.Disabled = true;
                    _choicesContainer.AddChild(blockedBtn);

                    var warningLabel = new Label
                    {
                        Text = "Check your inbox - there's an urgent situation requiring your response!",
                        AutowrapMode = TextServer.AutowrapMode.Word
                    };
                    warningLabel.AddThemeFontSizeOverride("font_size", 10);
                    warningLabel.Modulate = new Color(1.0f, 0.6f, 0.4f);
                    _choicesContainer.AddChild(warningLabel);
                }
                else
                {
                    var endBtn = CreateActionButton(endText, Colors.Cyan);
                    endBtn.Pressed += () => _gameManager?.EndPlayCardsPhase();
                    _choicesContainer.AddChild(endBtn);

                    if (restraintBonus > 0)
                    {
                        var bonusLabel = new Label
                        {
                            Text = "Restraint bonus for playing fewer cards",
                            AutowrapMode = TextServer.AutowrapMode.Word
                        };
                        bonusLabel.AddThemeFontSizeOverride("font_size", 9);
                        bonusLabel.Modulate = new Color(0.4f, 0.8f, 0.4f);
                        _choicesContainer.AddChild(bonusLabel);
                    }
                }

                // PC Spending Section
                _choicesContainer.AddChild(new HSeparator());

                var spendHeader = new Label { Text = "SPEND POLITICAL CAPITAL" };
                spendHeader.AddThemeFontSizeOverride("font_size", 10);
                spendHeader.Modulate = new Color(0.9f, 0.8f, 0.5f);
                _choicesContainer.AddChild(spendHeader);

                // Meter boost buttons (1 PC for +5)
                var boostHint = new Label
                {
                    Text = "Boost Metric (+5 for 1 PC):",
                    AutowrapMode = TextServer.AutowrapMode.Word
                };
                boostHint.AddThemeFontSizeOverride("font_size", 9);
                boostHint.Modulate = new Color(0.6f, 0.6f, 0.65f);
                _choicesContainer.AddChild(boostHint);

                var meterButtonsRow = new HBoxContainer();
                meterButtonsRow.AddThemeConstantOverride("separation", 4);
                _choicesContainer.AddChild(meterButtonsRow);

                // Create small buttons for each meter
                var meters = new[] { Meter.Delivery, Meter.Morale, Meter.Governance, Meter.Alignment, Meter.Runway };
                foreach (var meter in meters)
                {
                    var meterBtn = CreateSmallMeterButton(meter, _gameManager?.CanAffordMeterBoost ?? false);
                    var meterToBoost = meter;
                    meterBtn.Pressed += () => _gameManager?.SpendPCToBoostMeter(meterToBoost);
                    meterButtonsRow.AddChild(meterBtn);
                }

                // Board schmooze button (2 PC for favorability gamble)
                _choicesContainer.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) }); // Spacer

                var schmoozeBtn = CreateActionButton("🍷 SCHMOOZE BOARD (2 PC)", new Color(0.8f, 0.6f, 0.9f));
                schmoozeBtn.Disabled = !(_gameManager?.CanAffordSchmooze ?? false);
                schmoozeBtn.TooltipText = "Wine and dine the board for +1-5% favorability\n(15% chance of embarrassing backfire)";
                schmoozeBtn.Pressed += () => _gameManager?.SpendPCToSchmoozeBoard();
                _choicesContainer.AddChild(schmoozeBtn);

                if (!(_gameManager?.CanAffordSchmooze ?? false))
                {
                    schmoozeBtn.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }

                // Re-org button (discard hand and draw new cards)
                var reorgBtn = CreateActionButton("🔄 RE-ORG HAND (3 PC)", new Color(0.5f, 0.8f, 0.9f));
                reorgBtn.Disabled = !(_gameManager?.CanAffordReorg ?? false);
                reorgBtn.TooltipText = "Discard your current projects and draw 5 new ones\nUseful when your hand doesn't fit the situation";
                reorgBtn.Pressed += () => _gameManager?.SpendPCToReorg();
                _choicesContainer.AddChild(reorgBtn);

                if (!(_gameManager?.CanAffordReorg ?? false))
                {
                    reorgBtn.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }

                // Evil redemption button (always show, disabled when no evil or can't afford)
                var redeemBtn = CreateActionButton("😇 REHABILITATE IMAGE (2 PC)", new Color(0.7f, 0.9f, 0.7f));
                var hasEvil = _gameManager?.HasEvilToRedeem ?? false;
                var canAffordRedeem = _gameManager?.CanAffordEvilRedemption ?? false;
                redeemBtn.Disabled = !hasEvil || !canAffordRedeem;
                redeemBtn.TooltipText = hasEvil
                    ? "Launch PR campaigns and charity galas to improve your reputation\nReduces Evil Score by 1"
                    : "Your reputation is spotless! No evil to redeem.";
                redeemBtn.Pressed += () => _gameManager?.SpendPCToRedeemEvil();
                _choicesContainer.AddChild(redeemBtn);

                if (!hasEvil || !canAffordRedeem)
                {
                    redeemBtn.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }

                break;

            case GamePhase.Resolution:
                if (state.CEO.CanRetire)
                {
                    // Show retirement option
                    _actionHeader.Text = "RETIREMENT AVAILABLE";
                    var scoreBreakdown = ScoreCalculator.GetScoreBreakdown(state.CEO, state.Resources);
                    _actionDescription.Text = $"[center][color=#80ff80]You've accumulated ${state.CEO.AccumulatedBonus}M in board bonuses.[/color]\n" +
                        $"You can retire with dignity... or push your luck.\n\n" +
                        $"[color=#ffff80]Current Score Preview: {scoreBreakdown.FinalScore} points[/color]\n" +
                        $"(Retirement multiplier: x2.0)\n\n" +
                        $"[color=#ff8080]Warning: Each quarter risks ouster (x0.5 score)[/color][/center]";

                    var retireBtn = CreateActionButton("👔 RETIRE NOW - Secure Victory", new Color(0.4f, 1.0f, 0.5f));
                    retireBtn.Pressed += () => _gameManager?.ChooseRetirement();
                    _choicesContainer.AddChild(retireBtn);

                    var continuePlayBtn = CreateActionButton("🎲 CONTINUE PLAYING - Risk It All", Colors.Orange);
                    continuePlayBtn.Pressed += () => _gameManager?.AdvanceResolution();
                    _choicesContainer.AddChild(continuePlayBtn);
                }
                else
                {
                    _actionHeader.Text = "QUARTERLY REVIEW";
                    _actionDescription.Text = $"The board is reviewing your performance this quarter.\n\n" +
                        $"[color=#808080]Accumulated Bonus: ${state.CEO.AccumulatedBonus}M / ${CEOState.RetirementThreshold}M to retire[/color]";

                    var resolveBtn = CreateActionButton("▶ BOARD REVIEW", Colors.Cyan);
                    resolveBtn.Pressed += () => _gameManager?.AdvanceResolution();
                    _choicesContainer.AddChild(resolveBtn);
                }
                break;

            default:
                // Fallback for any unexpected phase - should never happen but prevents getting stuck
                _actionHeader.Text = "UNEXPECTED STATE";
                _actionDescription.Text = $"[color=#ff9966]Game is in unexpected phase: {state.Quarter.Phase}[/color]\n\n" +
                    "Please use the button below to attempt recovery.";

                var recoveryBtn = CreateActionButton("▶ ATTEMPT RECOVERY", new Color(0.9f, 0.6f, 0.3f));
                recoveryBtn.Pressed += () =>
                {
                    // Try to advance to a known good state
                    _gameManager?.ForceAdvanceCrisis();
                };
                _choicesContainer.AddChild(recoveryBtn);
                break;
        }
    }

    private Button CreateActionButton(string text, Color color)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(300, 40)
        };

        var style = new StyleBoxFlat
        {
            BgColor = color * 0.3f,
            BorderColor = color * 0.7f,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        style.BorderWidthTop = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = color * 0.5f,
            BorderColor = color * 0.7f,
            ContentMarginLeft = 15,
            ContentMarginRight = 15,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        hoverStyle.BorderWidthTop = 1;
        hoverStyle.BorderWidthBottom = 1;
        hoverStyle.BorderWidthLeft = 1;
        hoverStyle.BorderWidthRight = 1;
        hoverStyle.CornerRadiusTopLeft = 4;
        hoverStyle.CornerRadiusTopRight = 4;
        hoverStyle.CornerRadiusBottomLeft = 4;
        hoverStyle.CornerRadiusBottomRight = 4;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        return btn;
    }

    private Label CreateForecastLabel(Choice choice, QuarterGameState state)
    {
        var forecast = ChoiceForecast.Create(
            choice.OutcomeProfile!,
            state.Org.Alignment,
            state.CEO.BoardPressureLevel);
        var label = new Label
        {
            Text = $"  Risk: {forecast.RiskLevel} | {forecast.LikelyOutcome}",
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        label.AddThemeFontSizeOverride("font_size", 10);
        label.Modulate = forecast.RiskLevel switch
        {
            RiskLevel.Low => new Color(0.4f, 0.9f, 0.4f),
            RiskLevel.Medium => new Color(0.9f, 0.9f, 0.4f),
            RiskLevel.High => new Color(0.9f, 0.3f, 0.3f),
            _ => Colors.Gray
        };
        return label;
    }

    private Button CreateSmallMeterButton(Meter meter, bool canAfford)
    {
        var abbrev = meter switch
        {
            Meter.Delivery => "DEL",
            Meter.Morale => "MOR",
            Meter.Governance => "GOV",
            Meter.Alignment => "ALN",
            Meter.Runway => "RUN",
            _ => meter.ToString()[..3].ToUpper()
        };

        var color = GetMeterColor(meter);

        var btn = new Button
        {
            Text = abbrev,
            CustomMinimumSize = new Vector2(36, 28),
            Disabled = !canAfford,
            TooltipText = $"Boost {meter} by 5 (costs 1 PC)"
        };
        btn.AddThemeFontSizeOverride("font_size", 9);

        var style = new StyleBoxFlat
        {
            BgColor = color * 0.25f,
            BorderColor = color * 0.6f,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        style.BorderWidthTop = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = color * 0.45f,
            BorderColor = color * 0.8f,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        hoverStyle.BorderWidthTop = 1;
        hoverStyle.BorderWidthBottom = 1;
        hoverStyle.BorderWidthLeft = 1;
        hoverStyle.BorderWidthRight = 1;
        hoverStyle.CornerRadiusTopLeft = 3;
        hoverStyle.CornerRadiusTopRight = 3;
        hoverStyle.CornerRadiusBottomLeft = 3;
        hoverStyle.CornerRadiusBottomRight = 3;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

        if (!canAfford)
        {
            btn.Modulate = new Color(0.5f, 0.5f, 0.5f);
        }

        return btn;
    }

    private void UpdateHand(QuarterGameState state)
    {
        if (_handContainer is null || _handInfo is null) return;

        // Clear existing cards
        foreach (var child in _handContainer.GetChildren())
        {
            child.QueueFree();
        }

        var isPlayPhase = state.Quarter.Phase == GamePhase.PlayCards;
        var cardsPlayed = state.CardsPlayedThisQuarter.Count;
        var canAfford = state.CanAffordNextCard;
        var remaining = QuarterGameState.MaxCardsPerQuarter - cardsPlayed;

        if (isPlayPhase)
        {
            var nextCost = state.GetNextCardPCCost();
            var nextRisk = state.GetNextCardRiskModifier();

            if (remaining > 0 && canAfford)
            {
                var costInfo = nextCost > 0 ? $" | Cost: {nextCost} PC, +{nextRisk}% risk" : "";
                _handInfo.Text = $"YOUR PROJECTS - Click to execute ({remaining} remaining){costInfo}";
            }
            else if (remaining > 0)
            {
                _handInfo.Text = $"YOUR PROJECTS - Need {nextCost} PC to play ({remaining} remaining)";
            }
            else
            {
                _handInfo.Text = $"YOUR PROJECTS - Max cards played this quarter";
            }
        }
        else
        {
            _handInfo.Text = $"YOUR PROJECTS ({state.Hand.Count} cards)";
        }

        var canPlay = isPlayPhase && canAfford && remaining > 0;
        var targetAmount = BoardDirective.ProfitIncrease.GetRequiredAmount(state.CEO.BoardPressureLevel);
        var delivery = state.Org.Delivery;
        foreach (var card in state.Hand.Cards)
        {
            var cardPanel = CreateCardPanel(card, canPlay, state.Org, targetAmount, delivery);
            _handContainer.AddChild(cardPanel);
        }
    }

    private Control CreateCardPanel(PlayableCard card, bool canPlay, OrgState? org = null, int? targetAmount = null, int? delivery = null)
    {
        // Get display color based on card type (Revenue cards get gold, others use meter affinity)
        var cardColor = GetCardDisplayColor(card);

        // Use Button as container for click handling
        var btn = new Button
        {
            CustomMinimumSize = new Vector2(220, 150),
            ClipContents = false
        };

        // Background tinted by card color, darker for corporate cards
        var bgColor = card.IsCorporate
            ? new Color(0.15f, 0.08f, 0.08f)
            : new Color(cardColor.R * 0.15f, cardColor.G * 0.15f, cardColor.B * 0.15f);

        // Border colored by card type
        var borderColor = cardColor;

        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        style.BorderWidthTop = 3;
        style.BorderWidthBottom = 3;
        style.BorderWidthLeft = 3;
        style.BorderWidthRight = 3;
        style.BorderColor = borderColor;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ShadowColor = new Color(0, 0, 0, 0.4f);
        style.ShadowSize = 4;
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = bgColor * 1.4f,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 14,
            ContentMarginBottom = 14
        };
        hoverStyle.BorderWidthTop = 3;
        hoverStyle.BorderWidthBottom = 3;
        hoverStyle.BorderWidthLeft = 3;
        hoverStyle.BorderWidthRight = 3;
        hoverStyle.BorderColor = borderColor * 1.3f;
        hoverStyle.CornerRadiusTopLeft = 8;
        hoverStyle.CornerRadiusTopRight = 8;
        hoverStyle.CornerRadiusBottomLeft = 8;
        hoverStyle.CornerRadiusBottomRight = 8;
        hoverStyle.ShadowColor = new Color(0, 0, 0, 0.5f);
        hoverStyle.ShadowSize = 8;
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", hoverStyle);

        // Content overlay with proper padding offsets
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 16;
        vbox.OffsetRight = -16;
        vbox.OffsetTop = 14;
        vbox.OffsetBottom = -14;
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        btn.AddChild(vbox);

        // Category tag for Revenue cards (prominent gold badge)
        if (card.Category == InertiCorp.Core.Cards.CardCategory.Revenue)
        {
            var categoryTag = new Label
            {
                Text = "💰 REVENUE",
                HorizontalAlignment = HorizontalAlignment.Left,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            categoryTag.AddThemeFontSizeOverride("font_size", 9);
            categoryTag.Modulate = cardColor;
            vbox.AddChild(categoryTag);
        }

        // Title (no clipping, allow wrap if needed)
        var title = new Label
        {
            Text = card.Title,
            AutowrapMode = TextServer.AutowrapMode.Word,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 13);
        title.Modulate = new Color(0.95f, 0.95f, 1.0f);
        vbox.AddChild(title);

        // Risk level badge
        var riskColor = card.RiskLevel switch
        {
            1 => new Color(0.4f, 0.75f, 0.4f),  // Green for SAFE
            3 => new Color(0.85f, 0.4f, 0.4f),  // Red for VOLATILE
            _ => new Color(0.85f, 0.75f, 0.3f)  // Yellow for MODERATE
        };
        var riskLabel = new Label
        {
            Text = $"[{card.RiskLabel}]",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        riskLabel.AddThemeFontSizeOverride("font_size", 9);
        riskLabel.Modulate = riskColor;
        vbox.AddChild(riskLabel);

        // Outcomes: Best / Expected / Worst (with scaling for revenue cards)
        var bestOutcome = new Label
        {
            Text = $"▲ {FormatCompactOutcome(card, card.Outcomes.Good, targetAmount, delivery)}",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        bestOutcome.AddThemeFontSizeOverride("font_size", 9);
        bestOutcome.Modulate = new Color(0.4f, 0.85f, 0.5f);  // Green
        vbox.AddChild(bestOutcome);

        var expectedOutcome = new Label
        {
            Text = $"→ {FormatCompactOutcome(card, card.Outcomes.Expected, targetAmount, delivery)}",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        expectedOutcome.AddThemeFontSizeOverride("font_size", 9);
        expectedOutcome.Modulate = new Color(0.85f, 0.85f, 0.5f);  // Yellow
        vbox.AddChild(expectedOutcome);

        var worstOutcome = new Label
        {
            Text = $"▼ {FormatCompactOutcome(card, card.Outcomes.Bad, targetAmount, delivery)}",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        worstOutcome.AddThemeFontSizeOverride("font_size", 9);
        worstOutcome.Modulate = new Color(0.85f, 0.4f, 0.4f);  // Red
        vbox.AddChild(worstOutcome);

        // Flexible spacer
        var flexSpacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        vbox.AddChild(flexSpacer);

        // Click hint at bottom
        var clickHint = new Label
        {
            Text = "Click for details",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        clickHint.AddThemeFontSizeOverride("font_size", 9);
        clickHint.Modulate = new Color(0.5f, 0.5f, 0.6f);
        vbox.AddChild(clickHint);

        // Hover animation
        btn.MouseEntered += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(btn, "scale", new Vector2(1.05f, 1.05f), 0.1f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
        };
        btn.MouseExited += () =>
        {
            var tween = CreateTween();
            tween.TweenProperty(btn, "scale", Vector2.One, 0.1f)
                .SetEase(Tween.EaseType.Out);
        };

        // Click shows the popup
        var cardForPopup = card;
        var cardCanPlay = canPlay;
        btn.Pressed += () => ShowCardDetail(cardForPopup, cardCanPlay);

        return btn;
    }

    private void PlayCardImmediately(string cardId)
    {
        if (_gameManager is null) return;

        // Play the card immediately (endPhase = false so player can continue)
        _gameManager.PlayCard(cardId, endPhaseAfter: false);

        // Track total cards played for reports
        _totalCardsPlayed++;
    }

    private static string GetPhaseDisplayName(GamePhase phase) => phase switch
    {
        GamePhase.BoardDemand => "BOARD BRIEFING",
        GamePhase.PlayCards => "PROJECT SELECTION",
        GamePhase.Crisis => "SITUATION",
        GamePhase.Resolution => "BOARD REVIEW",
        _ => phase.ToString().ToUpper()
    };

    private static Color GetValueColor(int value)
    {
        if (value >= 60) return new Color(0.4f, 0.9f, 0.4f);
        if (value >= 40) return new Color(0.9f, 0.9f, 0.4f);
        if (value >= 20) return new Color(0.9f, 0.6f, 0.3f);
        return new Color(0.9f, 0.3f, 0.3f);
    }

    /// <summary>
    /// Gets the color for a meter (matches Company Metrics chart colors).
    /// </summary>
    private static Color GetMeterColor(Meter? meter)
    {
        return meter switch
        {
            Meter.Delivery => new Color(0.3f, 0.7f, 0.9f),    // Blue
            Meter.Morale => new Color(0.9f, 0.4f, 0.7f),      // Pink/Magenta (distinct from gold Revenue)
            Meter.Governance => new Color(0.7f, 0.5f, 0.8f),  // Purple
            Meter.Alignment => new Color(0.5f, 0.8f, 0.5f),   // Green
            Meter.Runway => new Color(0.9f, 0.5f, 0.3f),      // Orange (distinct from red)
            _ => new Color(0.5f, 0.5f, 0.6f)                  // Gray (no affinity)
        };
    }

    /// <summary>
    /// Formats a card outcome for display, including both meter effects and profit.
    /// When targetAmount and delivery are provided, revenue profits are scaled.
    /// </summary>
    private static string FormatCardOutcome(PlayableCard card, IReadOnlyList<IEffect> effects, int? targetAmount = null, int? delivery = null)
    {
        var parts = new List<string>();

        foreach (var effect in effects)
        {
            if (effect is MeterEffect me)
            {
                var sign = me.Delta >= 0 ? "+" : "";
                parts.Add($"{me.Meter} {sign}{me.Delta}");
            }
            else if (effect is ProfitEffect pe && card.Category == InertiCorp.Core.Cards.CardCategory.Revenue)
            {
                var profit = pe.Delta;
                if (targetAmount.HasValue && delivery.HasValue)
                {
                    profit = ProfitCalculator.ScaleRevenueProfit(pe.Delta, targetAmount.Value, delivery.Value);
                }
                var sign = profit >= 0 ? "+" : "";
                parts.Add($"${sign}{profit}M");
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "No effect";
    }

    /// <summary>
    /// Formats a compact outcome for small card display (shorter meter names).
    /// When targetAmount and delivery are provided, revenue profits are scaled.
    /// </summary>
    private static string FormatCompactOutcome(PlayableCard card, IReadOnlyList<IEffect> effects, int? targetAmount = null, int? delivery = null)
    {
        var parts = new List<string>();

        foreach (var effect in effects)
        {
            if (effect is MeterEffect me)
            {
                var meterAbbrev = me.Meter switch
                {
                    Meter.Delivery => "Del",
                    Meter.Morale => "Mor",
                    Meter.Governance => "Gov",
                    Meter.Alignment => "Ali",
                    Meter.Runway => "Run",
                    _ => me.Meter.ToString()[..3]
                };
                var sign = me.Delta >= 0 ? "+" : "";
                parts.Add($"{meterAbbrev}{sign}{me.Delta}");
            }
            else if (effect is ProfitEffect pe && card.Category == InertiCorp.Core.Cards.CardCategory.Revenue)
            {
                var profit = pe.Delta;
                if (targetAmount.HasValue && delivery.HasValue)
                {
                    profit = ProfitCalculator.ScaleRevenueProfit(pe.Delta, targetAmount.Value, delivery.Value);
                }
                var sign = profit >= 0 ? "+" : "";
                parts.Add($"${sign}{profit}M");
            }
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "—";
    }

    /// <summary>
    /// Gets the color for a card category.
    /// Revenue cards get a distinct gold color to stand out from meter-based cards.
    /// </summary>
    private static Color GetCategoryColor(InertiCorp.Core.Cards.CardCategory category)
    {
        return category switch
        {
            InertiCorp.Core.Cards.CardCategory.Revenue => new Color(1.0f, 0.85f, 0.2f),  // Gold/Yellow for revenue
            InertiCorp.Core.Cards.CardCategory.Corporate => new Color(0.6f, 0.2f, 0.2f), // Dark red for corporate
            _ => new Color(0.5f, 0.5f, 0.6f)  // Gray for other categories
        };
    }

    /// <summary>
    /// Gets the primary display color for a card.
    /// Revenue cards use category color; others use meter affinity color.
    /// </summary>
    private static Color GetCardDisplayColor(PlayableCard card)
    {
        // Revenue cards always use gold color regardless of meter affinity
        if (card.Category == InertiCorp.Core.Cards.CardCategory.Revenue)
        {
            return GetCategoryColor(InertiCorp.Core.Cards.CardCategory.Revenue);
        }

        // Other cards use their meter affinity color
        return GetMeterColor(card.MeterAffinity);
    }

    /// <summary>
    /// Animates the PC value label when it changes.
    /// </summary>
    private void AnimatePCChange(int oldValue, int newValue)
    {
        if (_pcLabel is null) return;

        // Kill any existing animation
        _pcAnimationTween?.Kill();
        _pcAnimationTween = CreateTween();

        // Choose animation color based on change direction
        var flashColor = newValue > oldValue
            ? new Color(0.4f, 1.0f, 0.4f)  // Green for gain
            : new Color(1.0f, 0.4f, 0.4f); // Red for loss

        var normalColor = new Color(1.0f, 0.9f, 0.4f);

        // Flash and scale animation
        _pcAnimationTween.SetParallel(true);

        // Color flash
        _pcAnimationTween.TweenProperty(_pcLabel, "modulate", flashColor, 0.1f);
        _pcAnimationTween.Chain().TweenProperty(_pcLabel, "modulate", normalColor, 0.3f)
            .SetEase(Tween.EaseType.Out);

        // Scale pulse
        _pcAnimationTween.TweenProperty(_pcLabel, "scale", new Vector2(1.3f, 1.3f), 0.1f)
            .SetEase(Tween.EaseType.Out);
        _pcAnimationTween.Chain().TweenProperty(_pcLabel, "scale", Vector2.One, 0.2f)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// Animates a control fading in with optional slide.
    /// </summary>
    private void AnimateFadeIn(Control control, float duration = 0.3f, bool slideFromRight = false)
    {
        var tween = CreateTween();
        control.Modulate = new Color(1, 1, 1, 0);

        if (slideFromRight)
        {
            var originalPos = control.Position;
            control.Position = new Vector2(originalPos.X + 20, originalPos.Y);
            tween.SetParallel(true);
            tween.TweenProperty(control, "position", originalPos, duration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }

        tween.TweenProperty(control, "modulate:a", 1.0f, duration)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Creates a subtle pulsing glow effect for unread indicators.
    /// Binds tween to the control so it's cleaned up when the control is freed.
    /// </summary>
    private void AnimateUnreadPulse(Control control)
    {
        // IMPORTANT: Use control.CreateTween() so tween dies when control is freed
        // Using CreateTween() (on this) would cause tweens to accumulate forever
        var tween = control.CreateTween();
        tween.SetLoops();

        var baseColor = control.Modulate;
        var brightColor = new Color(
            Mathf.Min(baseColor.R * 1.3f, 1.0f),
            Mathf.Min(baseColor.G * 1.3f, 1.0f),
            Mathf.Min(baseColor.B * 1.3f, 1.0f),
            baseColor.A
        );

        tween.TweenProperty(control, "modulate", brightColor, 0.8f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(control, "modulate", baseColor, 0.8f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    /// <summary>
    /// Creates a more dramatic pulsing glow effect for high-priority crisis emails.
    /// Uses color tinting to make the email visually "flash" for attention.
    /// </summary>
    private void StartPulsingGlow(Control control, Color accentColor)
    {
        // IMPORTANT: Use control.CreateTween() so tween dies when control is freed
        var tween = control.CreateTween();
        tween.SetLoops();

        // Pulse between normal and bright tinted color
        var normalColor = new Color(1f, 1f, 1f, 1f);
        var glowColor = new Color(
            Mathf.Lerp(1f, accentColor.R, 0.4f),
            Mathf.Lerp(1f, accentColor.G, 0.4f),
            Mathf.Lerp(1f, accentColor.B, 0.4f),
            1f
        );

        // Faster, more noticeable pulse for urgency
        tween.TweenProperty(control, "modulate", glowColor, 0.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(control, "modulate", normalColor, 0.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    /// <summary>
    /// Shows or hides the hand panel with slide animation.
    /// </summary>
    private void SetHandPanelVisible(bool visible)
    {
        if (_handPanel is null || _handPanelVisible == visible) return;

        _handPanelVisible = visible;

        // Kill any existing animation
        _handPanelTween?.Kill();
        _handPanelTween = CreateTween();
        _handPanelTween.SetParallel(true);

        if (visible)
        {
            // Slide up and fade in
            _handPanelTween.TweenProperty(_handPanel, "custom_minimum_size:y", 190f, 0.3f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _handPanelTween.TweenProperty(_handPanel, "modulate:a", 1.0f, 0.25f)
                .SetEase(Tween.EaseType.Out);
        }
        else
        {
            // Slide down and fade out
            _handPanelTween.TweenProperty(_handPanel, "custom_minimum_size:y", 0f, 0.25f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            _handPanelTween.TweenProperty(_handPanel, "modulate:a", 0.0f, 0.2f)
                .SetEase(Tween.EaseType.In);
        }
    }
}
