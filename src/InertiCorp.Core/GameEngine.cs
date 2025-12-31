namespace InertiCorp.Core;

/// <summary>
/// Core game engine handling turn advancement and game logic.
/// </summary>
public static class GameEngine
{
    /// <summary>
    /// Advances the game by one turn.
    /// </summary>
    /// <param name="state">Current game state.</param>
    /// <param name="input">Player input for this turn.</param>
    /// <param name="rng">Random number generator (must be used exclusively).</param>
    /// <returns>New game state and log of what happened.</returns>
    /// <exception cref="InvalidOperationException">Thrown if game is already lost or won.</exception>
    /// <exception cref="ArgumentException">Thrown if choice ID is invalid.</exception>
    public static (GameState NewState, TurnLog Log) AdvanceTurn(
        GameState state,
        TurnInput input,
        IRng rng)
    {
        // Cannot advance a finished game
        if (state.IsLost)
        {
            throw new InvalidOperationException("Cannot advance turn: game is already lost.");
        }
        if (state.IsWon)
        {
            throw new InvalidOperationException("Cannot advance turn: game is already won.");
        }

        var entries = new List<LogEntry>();
        var currentTurn = state.Turn.TurnNumber;
        string? drawnEventId = null;
        string? chosenChoiceId = null;

        var newState = state;

        // Handle event deck if present
        if (state.Deck is not null)
        {
            // Draw event
            var eventCard = state.Deck.DrawWithReshuffle(rng);
            drawnEventId = eventCard.EventId;
            chosenChoiceId = input.ChosenChoiceId;

            entries.Add(LogEntry.Event($"Event: {eventCard.Title}"));

            // Validate and get the chosen choice
            var choice = eventCard.GetChoice(input.ChosenChoiceId);

            // Apply all effects from the choice
            foreach (var effect in choice.Effects)
            {
                var (updatedState, effectEntries) = effect.Apply(newState, rng);
                newState = updatedState;
                entries.AddRange(effectEntries);
            }
        }

        // Log turn completion
        entries.Add(LogEntry.Info($"Turn {currentTurn} completed"));

        // Advance turn number
        var newTurn = state.Turn.NextTurn();
        newState = newState.WithTurn(newTurn);

        // Check lose conditions first (loss overrides win)
        if (CheckLoseCondition(newState.Org))
        {
            newState = newState.WithLoss();
            entries.Add(LogEntry.Info("Game Over: Critical meter reached zero."));
        }
        // If not lost and turn 12 just completed, evaluate objectives
        else if (currentTurn == GameConstants.TurnsPerQuarter)
        {
            newState = EvaluateObjectives(newState, entries);
        }

        var log = new TurnLog(currentTurn, entries, drawnEventId, chosenChoiceId);

        return (newState, log);
    }

    /// <summary>
    /// Evaluates objectives at end of quarter and determines win/lose.
    /// </summary>
    private static GameState EvaluateObjectives(GameState state, List<LogEntry> entries)
    {
        if (state.ActiveObjectives.Count == 0)
        {
            return state;
        }

        var results = new List<ObjectiveResult>();
        foreach (var objective in state.ActiveObjectives)
        {
            var isMet = objective.IsMet(state.Org);
            results.Add(new ObjectiveResult(objective, isMet));
            entries.Add(LogEntry.Info($"Objective '{objective.Title}': {(isMet ? "Met" : "Failed")}"));
        }

        var metCount = results.Count(r => r.IsMet);
        var isWon = metCount >= 2;

        if (isWon)
        {
            entries.Add(LogEntry.Info($"Victory! {metCount} of {results.Count} objectives met."));
        }
        else
        {
            entries.Add(LogEntry.Info($"Defeat. Only {metCount} of {results.Count} objectives met."));
        }

        return state.WithWinEvaluation(isWon, results);
    }

    /// <summary>
    /// Checks if any lose condition is met.
    /// </summary>
    private static bool CheckLoseCondition(OrgState org)
    {
        return org.Runway == 0 || org.Morale == 0;
    }
}
