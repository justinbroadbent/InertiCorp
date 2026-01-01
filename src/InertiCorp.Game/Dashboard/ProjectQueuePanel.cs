using Godot;

namespace InertiCorp.Game.Dashboard;

/// <summary>
/// UI panel showing "active projects" being processed in the background.
/// This is a thin UI layer that displays data from BackgroundEmailProcessor.
/// </summary>
public partial class ProjectQueuePanel : PanelContainer
{
    private VBoxContainer? _projectsContainer;
    private Label? _headerLabel;
    private readonly Dictionary<string, ProjectUIEntry> _uiEntries = new();

    public override void _Ready()
    {
        // Style the panel
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 0.95f),
            BorderColor = new Color(0.2f, 0.25f, 0.3f),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        style.BorderWidthTop = 1;
        style.BorderWidthBottom = 1;
        style.BorderWidthLeft = 1;
        style.BorderWidthRight = 1;
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        AddThemeStyleboxOverride("panel", style);

        // Main container
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        AddChild(vbox);

        // Header
        _headerLabel = new Label
        {
            Text = "ACTIVE PROJECTS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _headerLabel.AddThemeFontSizeOverride("font_size", 11);
        _headerLabel.Modulate = new Color(0.6f, 0.65f, 0.7f);
        vbox.AddChild(_headerLabel);

        vbox.AddChild(new HSeparator());

        // Projects container
        _projectsContainer = new VBoxContainer();
        _projectsContainer.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(_projectsContainer);

        // Initially hidden when empty
        UpdateVisibility();
    }

    public override void _Process(double delta)
    {
        var processor = BackgroundEmailProcessor.Instance;
        if (processor == null)
        {
            UpdateVisibility();
            return;
        }

        var activeProjects = processor.ActiveProjects;

        // Remove UI entries for completed projects
        var toRemove = _uiEntries.Keys.Except(activeProjects.Keys).ToList();
        foreach (var cardId in toRemove)
        {
            if (_uiEntries.TryGetValue(cardId, out var entry))
            {
                entry.Container?.QueueFree();
                _uiEntries.Remove(cardId);
            }
        }

        // Add UI entries for new projects
        foreach (var (cardId, project) in activeProjects)
        {
            if (!_uiEntries.ContainsKey(cardId))
            {
                var uiEntry = CreateProjectUI(project);
                _uiEntries[cardId] = uiEntry;
                _projectsContainer?.AddChild(uiEntry.Container);
            }
        }

        // Update existing entries
        foreach (var (cardId, project) in activeProjects)
        {
            if (_uiEntries.TryGetValue(cardId, out var uiEntry))
            {
                UpdateProjectUI(uiEntry, project);
            }
        }

        UpdateHeader(activeProjects.Count);
        UpdateVisibility();
    }

    private ProjectUIEntry CreateProjectUI(ActiveProject project)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 2);

        // Title row
        var titleRow = new HBoxContainer();
        container.AddChild(titleRow);

        var titleLabel = new Label
        {
            Text = TruncateText(project.Title, 22),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 10);
        titleRow.AddChild(titleLabel);

        var percentLabel = new Label
        {
            Text = "0%"
        };
        percentLabel.AddThemeFontSizeOverride("font_size", 9);
        percentLabel.Modulate = new Color(0.5f, 0.7f, 0.5f);
        titleRow.AddChild(percentLabel);

        // Progress bar
        var progressBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(0, 8),
            Value = 0,
            MaxValue = 100,
            ShowPercentage = false
        };
        var bgStyle = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.12f) };
        bgStyle.CornerRadiusTopLeft = 2;
        bgStyle.CornerRadiusTopRight = 2;
        bgStyle.CornerRadiusBottomLeft = 2;
        bgStyle.CornerRadiusBottomRight = 2;
        progressBar.AddThemeStyleboxOverride("background", bgStyle);

        var fillStyle = new StyleBoxFlat { BgColor = new Color(0.3f, 0.5f, 0.4f) };
        fillStyle.CornerRadiusTopLeft = 2;
        fillStyle.CornerRadiusTopRight = 2;
        fillStyle.CornerRadiusBottomLeft = 2;
        fillStyle.CornerRadiusBottomRight = 2;
        progressBar.AddThemeStyleboxOverride("fill", fillStyle);
        container.AddChild(progressBar);

        // Status message
        var statusLabel = new Label
        {
            Text = project.StatusMessage
        };
        statusLabel.AddThemeFontSizeOverride("font_size", 8);
        statusLabel.Modulate = new Color(0.5f, 0.5f, 0.55f);
        container.AddChild(statusLabel);

        // Set tooltip with full title and description for hover
        container.TooltipText = $"{project.Title}\n\n{project.Description}";

        return new ProjectUIEntry(container, progressBar, percentLabel, statusLabel);
    }

    private static void UpdateProjectUI(ProjectUIEntry ui, ActiveProject project)
    {
        ui.ProgressBar?.SetValue(project.ProgressPercent);
        ui.PercentLabel?.SetText($"{project.ProgressPercent}%");
        ui.StatusLabel?.SetText(project.StatusMessage);
    }

    private void UpdateHeader(int count)
    {
        if (_headerLabel == null) return;
        _headerLabel.Text = count > 0
            ? $"ACTIVE PROJECTS ({count})"
            : "ACTIVE PROJECTS";
    }

    private void UpdateVisibility()
    {
        var hasProjects = BackgroundEmailProcessor.Instance?.HasActiveProjects ?? false;
        Visible = hasProjects;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// UI elements for a single project entry.
    /// </summary>
    private sealed record ProjectUIEntry(
        Control Container,
        ProgressBar? ProgressBar,
        Label? PercentLabel,
        Label? StatusLabel);
}
