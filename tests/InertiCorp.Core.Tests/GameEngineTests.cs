namespace InertiCorp.Core.Tests;

public class GameEngineTests
{
    [Fact]
    public void AdvanceTurn_IncrementsTurnNumber()
    {
        var state = GameState.NewGame(seed: 123);
        var input = TurnInput.Empty;
        var rng = new SeededRng(123);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(2, newState.Turn.TurnNumber);
    }

    [Fact]
    public void AdvanceTurn_12Times_ReachesEndOfQuarter()
    {
        var state = GameState.NewGame(seed: 456);
        var input = TurnInput.Empty;
        var rng = new SeededRng(456);

        for (int i = 0; i < 12; i++)
        {
            (state, _) = GameEngine.AdvanceTurn(state, input, rng);
        }

        Assert.Equal(13, state.Turn.TurnNumber); // Started at 1, advanced 12 times
        Assert.True(state.Turn.TurnNumber > GameConstants.TurnsPerQuarter);
    }

    [Fact]
    public void AdvanceTurn_ReturnsTurnLog_WithCorrectTurnNumber()
    {
        var state = GameState.NewGame(seed: 789);
        var input = TurnInput.Empty;
        var rng = new SeededRng(789);

        var (_, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(1, log.TurnNumber); // Log reflects the turn that was just played
    }

    [Fact]
    public void AdvanceTurn_IsDeterministic_SameInputsSameOutput()
    {
        var state1 = GameState.NewGame(seed: 999);
        var state2 = GameState.NewGame(seed: 999);
        var input = TurnInput.Empty;
        var rng1 = new SeededRng(999);
        var rng2 = new SeededRng(999);

        var (newState1, log1) = GameEngine.AdvanceTurn(state1, input, rng1);
        var (newState2, log2) = GameEngine.AdvanceTurn(state2, input, rng2);

        Assert.Equal(newState1.Turn.TurnNumber, newState2.Turn.TurnNumber);
        Assert.Equal(newState1.Org.Delivery, newState2.Org.Delivery);
        Assert.Equal(newState1.Org.Morale, newState2.Org.Morale);
        Assert.Equal(log1.TurnNumber, log2.TurnNumber);
        Assert.Equal(log1.Entries.Count, log2.Entries.Count);
    }

    [Fact]
    public void AdvanceTurn_LogContainsEntries()
    {
        var state = GameState.NewGame(seed: 111);
        var input = TurnInput.Empty;
        var rng = new SeededRng(111);

        var (_, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.NotNull(log.Entries);
        // At minimum, should have an info entry about turn advance
        Assert.Contains(log.Entries, e => e.Category == LogCategory.Info);
    }

    [Fact]
    public void AdvanceTurn_DoesNotMutateOriginalState()
    {
        var original = GameState.NewGame(seed: 222);
        var input = TurnInput.Empty;
        var rng = new SeededRng(222);

        var (newState, _) = GameEngine.AdvanceTurn(original, input, rng);

        Assert.Equal(1, original.Turn.TurnNumber); // Original unchanged
        Assert.Equal(2, newState.Turn.TurnNumber); // New state advanced
    }

    [Fact]
    public void AdvanceTurn_RunwayAt0_SetsIsLostTrue()
    {
        var state = GameState.NewGame(seed: 333)
            .WithOrg(new OrgState(Delivery: 50, Morale: 50, Governance: 50, Alignment: 50, Runway: 0));
        var input = TurnInput.Empty;
        var rng = new SeededRng(333);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.True(newState.IsLost);
    }

    [Fact]
    public void AdvanceTurn_MoraleAt0_SetsIsLostTrue()
    {
        var state = GameState.NewGame(seed: 444)
            .WithOrg(new OrgState(Delivery: 50, Morale: 0, Governance: 50, Alignment: 50, Runway: 50));
        var input = TurnInput.Empty;
        var rng = new SeededRng(444);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.True(newState.IsLost);
    }

    [Fact]
    public void AdvanceTurn_MetersClamped_LowMeterTriggersLoss()
    {
        // Start with Morale at 5, simulate an effect that would bring it to -5 (clamped to 0)
        var state = GameState.NewGame(seed: 555)
            .WithOrg(new OrgState(Delivery: 50, Morale: 5, Governance: 50, Alignment: 50, Runway: 50));

        // Apply a change that would reduce Morale below 0
        var orgWithChange = state.Org.WithMeterChange(Meter.Morale, -10);
        state = state.WithOrg(orgWithChange);

        var input = TurnInput.Empty;
        var rng = new SeededRng(555);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(0, newState.Org.Morale); // Clamped to 0
        Assert.True(newState.IsLost);
    }

    [Fact]
    public void AdvanceTurn_OnLostGame_ThrowsInvalidOperationException()
    {
        var state = GameState.NewGame(seed: 666)
            .WithOrg(new OrgState(Delivery: 50, Morale: 0, Governance: 50, Alignment: 50, Runway: 50));
        var input = TurnInput.Empty;
        var rng = new SeededRng(666);

        // First advance triggers loss
        var (lostState, _) = GameEngine.AdvanceTurn(state, input, rng);
        Assert.True(lostState.IsLost);

        // Second advance should throw
        Assert.Throws<InvalidOperationException>(() =>
            GameEngine.AdvanceTurn(lostState, input, rng));
    }

    [Fact]
    public void AdvanceTurn_HealthyMeters_DoesNotSetIsLost()
    {
        var state = GameState.NewGame(seed: 777); // All meters at 50
        var input = TurnInput.Empty;
        var rng = new SeededRng(777);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.False(newState.IsLost);
    }

    [Fact]
    public void AdvanceTurn_LossBeforeTurn12_EndsImmediately()
    {
        var state = GameState.NewGame(seed: 888)
            .WithOrg(new OrgState(Delivery: 50, Morale: 0, Governance: 50, Alignment: 50, Runway: 50));
        var input = TurnInput.Empty;
        var rng = new SeededRng(888);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.True(newState.IsLost);
        Assert.Equal(2, newState.Turn.TurnNumber); // Only advanced one turn before loss
    }
}

public class TurnLogTests
{
    [Fact]
    public void LogEntry_WithMeterChange_HasMeterAndDelta()
    {
        var entry = LogEntry.MeterChange(Meter.Morale, -10, "Morale decreased");

        Assert.Equal(LogCategory.MeterChange, entry.Category);
        Assert.Equal(Meter.Morale, entry.Meter);
        Assert.Equal(-10, entry.Delta);
        Assert.Equal("Morale decreased", entry.Message);
    }

    [Fact]
    public void LogEntry_Info_HasNoMeterOrDelta()
    {
        var entry = LogEntry.Info("Turn started");

        Assert.Equal(LogCategory.Info, entry.Category);
        Assert.Null(entry.Meter);
        Assert.Null(entry.Delta);
    }
}

public class TurnInputTests
{
    [Fact]
    public void Empty_IsValidPlaceholder()
    {
        var input = TurnInput.Empty;

        Assert.NotNull(input);
    }
}
