using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI panel displaying outcome and log entries after a choice.
/// </summary>
public partial class HeadlinesPanel : PanelContainer
{
    private GameManager? _gameManager;

    private Label? _titleLabel;
    private Label? _outcomeLabel;
    private VBoxContainer? _entriesContainer;
    private Button? _continueButton;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");

        _titleLabel = GetNode<Label>("VBox/Title");
        _outcomeLabel = GetNodeOrNull<Label>("VBox/Outcome");
        _entriesContainer = GetNode<VBoxContainer>("VBox/Entries");
        _continueButton = GetNode<Button>("VBox/ContinueButton");

        _gameManager.PhaseChanged += OnPhaseChanged;
        _continueButton.Pressed += OnContinuePressed;

        UpdateVisibility();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.PhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPhaseChanged()
    {
        UpdateVisibility();
        if (_gameManager?.Phase == UIPhase.ShowingResolution)
        {
            UpdateHeadlines();
        }
    }

    private void UpdateVisibility()
    {
        // Show during Resolution phase
        Visible = _gameManager?.Phase == UIPhase.ShowingResolution;
    }

    private void UpdateHeadlines()
    {
        ClearEntries();

        if (_gameManager?.LastLog is null || _entriesContainer is null) return;

        var log = _gameManager.LastLog;

        // Update title with phase info
        if (_titleLabel is not null)
        {
            _titleLabel.Text = $"{QuarterState.FormatQuarter(log.QuarterNumber)} - {log.Phase} Results";
        }

        // Check for outcome tier in the log entries
        if (_outcomeLabel is not null)
        {
            var outcomeLine = log.Entries.FirstOrDefault(e => e.Message.StartsWith("Outcome:"));
            if (outcomeLine is not null)
            {
                _outcomeLabel.Text = outcomeLine.Message;
                _outcomeLabel.Visible = true;

                // Color based on outcome
                if (outcomeLine.Message.Contains("Good"))
                    _outcomeLabel.Modulate = new Color(0.3f, 0.8f, 0.3f);
                else if (outcomeLine.Message.Contains("Bad"))
                    _outcomeLabel.Modulate = new Color(0.9f, 0.3f, 0.3f);
                else
                    _outcomeLabel.Modulate = new Color(0.9f, 0.8f, 0.2f);
            }
            else
            {
                _outcomeLabel.Visible = false;
            }
        }

        // Add entries (skip the outcome line if we already showed it)
        foreach (var entry in log.Entries)
        {
            if (entry.Message.StartsWith("Outcome:")) continue;

            var label = new Label
            {
                Text = FormatEntry(entry),
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.Word
            };

            // Color based on category
            if (entry.Category == LogCategory.MeterChange)
            {
                if (entry.Delta.HasValue)
                {
                    label.Modulate = entry.Delta.Value >= 0
                        ? new Color(0.5f, 1f, 0.5f)
                        : new Color(1f, 0.5f, 0.5f);
                }
            }

            _entriesContainer.AddChild(label);
        }
    }

    private static string FormatEntry(LogEntry entry)
    {
        if (entry.Category == LogCategory.MeterChange && entry.Meter.HasValue && entry.Delta.HasValue)
        {
            var sign = entry.Delta.Value >= 0 ? "+" : "";
            return $"{entry.Meter.Value} {sign}{entry.Delta.Value}";
        }

        return entry.Message;
    }

    private void ClearEntries()
    {
        if (_entriesContainer is null) return;

        foreach (var child in _entriesContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnContinuePressed()
    {
        _gameManager?.ContinueToNextTurn();
    }
}
