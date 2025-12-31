using Godot;
using InertiCorp.Core;

namespace InertiCorp.Game;

/// <summary>
/// UI component displaying the current quarter and phase.
/// </summary>
public partial class TurnCounter : Label
{
    private GameManager? _gameManager;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/Main/GameManager");
        _gameManager.StateChanged += OnStateChanged;
        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        if (_gameManager is not null)
        {
            _gameManager.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_gameManager?.CurrentState is null)
        {
            Text = "Y-Q- | --";
            return;
        }

        var quarter = _gameManager.CurrentState.Quarter;
        Text = $"{quarter.FormattedQuarter} | {quarter.Phase}";
    }
}
