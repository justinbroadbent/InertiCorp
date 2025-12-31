namespace InertiCorp.Core.Tests;

public class OrgStateTests
{
    [Fact]
    public void Constructor_DefaultValues_AllMetersAt60()
    {
        var state = OrgState.Default;

        Assert.Equal(60, state.Delivery);
        Assert.Equal(60, state.Morale);
        Assert.Equal(60, state.Governance);
        Assert.Equal(60, state.Alignment);
        Assert.Equal(60, state.Runway);
    }

    [Fact]
    public void Constructor_ClampsMetersToMin0()
    {
        var state = new OrgState(
            Delivery: -10,
            Morale: -5,
            Governance: 0,
            Alignment: 50,
            Runway: 100
        );

        Assert.Equal(0, state.Delivery);
        Assert.Equal(0, state.Morale);
        Assert.Equal(0, state.Governance);
    }

    [Fact]
    public void Constructor_CapsMetersAt100()
    {
        // Meters are capped at 100
        var state = new OrgState(
            Delivery: 150,
            Morale: 100,
            Governance: 101,
            Alignment: 50,
            Runway: 200
        );

        Assert.Equal(100, state.Delivery);  // Capped from 150
        Assert.Equal(100, state.Morale);
        Assert.Equal(100, state.Governance);  // Capped from 101
        Assert.Equal(100, state.Runway);  // Capped from 200
    }

    [Fact]
    public void WithMeterChange_AppliesDeltaAndClamps()
    {
        var state = OrgState.Default; // All at 60

        var updated = state.WithMeterChange(Meter.Morale, -70);

        Assert.Equal(0, updated.Morale); // Clamped to 0
        Assert.Equal(60, updated.Delivery); // Unchanged
    }

    [Fact]
    public void WithMeterChange_PositiveDelta()
    {
        var state = OrgState.Default; // All at 60

        var updated = state.WithMeterChange(Meter.Delivery, 30);

        Assert.Equal(90, updated.Delivery);
    }

    [Fact]
    public void OrgState_IsImmutable()
    {
        var original = OrgState.Default; // All at 60
        var modified = original.WithMeterChange(Meter.Morale, -10);

        Assert.Equal(60, original.Morale); // Original unchanged
        Assert.Equal(50, modified.Morale); // New instance has change
    }
}

public class TurnStateTests
{
    [Fact]
    public void Constructor_SetsTurnNumber()
    {
        var state = new TurnState(TurnNumber: 5);

        Assert.Equal(5, state.TurnNumber);
    }

    [Fact]
    public void TurnsPerQuarter_Is12()
    {
        Assert.Equal(12, GameConstants.TurnsPerQuarter);
    }

    [Fact]
    public void Initial_StartsAtTurn1()
    {
        var state = TurnState.Initial;

        Assert.Equal(1, state.TurnNumber);
    }

    [Fact]
    public void NextTurn_IncrementsTurnNumber()
    {
        var state = new TurnState(TurnNumber: 3);
        var next = state.NextTurn();

        Assert.Equal(4, next.TurnNumber);
    }

    [Fact]
    public void IsLastTurn_TrueOnTurn12()
    {
        var state = new TurnState(TurnNumber: 12);

        Assert.True(state.IsLastTurn);
    }

    [Fact]
    public void IsLastTurn_FalseBeforeTurn12()
    {
        var state = new TurnState(TurnNumber: 11);

        Assert.False(state.IsLastTurn);
    }
}

public class GameStateTests
{
    [Fact]
    public void NewGame_CreatesDefaultState()
    {
        var state = GameState.NewGame(seed: 12345);

        Assert.Equal(12345, state.Seed);
        Assert.Equal(1, state.Turn.TurnNumber);
        Assert.Equal(60, state.Org.Delivery);
        Assert.Equal(60, state.Org.Morale);
        Assert.Equal(60, state.Org.Governance);
        Assert.Equal(60, state.Org.Alignment);
        Assert.Equal(60, state.Org.Runway);
        Assert.False(state.IsLost);
    }

    [Fact]
    public void GameState_IsImmutable()
    {
        var original = GameState.NewGame(seed: 100);
        var modified = original.WithTurn(new TurnState(5));

        Assert.Equal(1, original.Turn.TurnNumber);
        Assert.Equal(5, modified.Turn.TurnNumber);
    }

    [Fact]
    public void NewGame_DifferentSeeds_StoreDifferentSeeds()
    {
        var state1 = GameState.NewGame(seed: 111);
        var state2 = GameState.NewGame(seed: 222);

        Assert.Equal(111, state1.Seed);
        Assert.Equal(222, state2.Seed);
    }
}
