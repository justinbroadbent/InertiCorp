namespace InertiCorp.Core.Tests;

public class CEOStateTests
{
    [Fact]
    public void Initial_BoardPressureLevel_IsOne()
    {
        var state = CEOState.Initial;
        Assert.Equal(1, state.BoardPressureLevel);
    }

    [Fact]
    public void Initial_QuartersSurvived_IsZero()
    {
        var state = CEOState.Initial;
        Assert.Equal(0, state.QuartersSurvived);
    }

    [Fact]
    public void Initial_BoardFavorability_Is75()
    {
        var state = CEOState.Initial;
        Assert.Equal(75, state.BoardFavorability);
    }

    [Fact]
    public void Initial_IsOusted_IsFalse()
    {
        var state = CEOState.Initial;
        Assert.False(state.IsOusted);
    }

    [Fact]
    public void WithFavorabilityChange_IncreasesWithinBounds()
    {
        var state = CEOState.Initial; // 75

        var next = state.WithFavorabilityChange(20);

        Assert.Equal(95, next.BoardFavorability);
    }

    [Fact]
    public void WithFavorabilityChange_DecreasesWithinBounds()
    {
        var state = CEOState.Initial; // 75

        var next = state.WithFavorabilityChange(-30);

        Assert.Equal(45, next.BoardFavorability);
    }

    [Fact]
    public void WithFavorabilityChange_ClampsToMax100()
    {
        var state = CEOState.Initial; // 75

        var next = state.WithFavorabilityChange(100);

        Assert.Equal(100, next.BoardFavorability);
    }

    [Fact]
    public void WithFavorabilityChange_ClampsToMin0()
    {
        var state = CEOState.Initial; // 50

        var next = state.WithFavorabilityChange(-100);

        Assert.Equal(0, next.BoardFavorability);
    }

    [Fact]
    public void Favorability0_DoesNotTriggerInstantOuster()
    {
        var state = CEOState.Initial.WithFavorabilityChange(-75); // 75 - 75 = 0

        Assert.Equal(0, state.BoardFavorability);
        Assert.False(state.IsOusted);
    }

    [Fact]
    public void WithOusted_SetsIsOustedTrue()
    {
        var state = CEOState.Initial;

        var next = state.WithOusted();

        Assert.True(next.IsOusted);
    }

    // Story 6.4: Golden Parachute

    [Fact]
    public void Initial_TotalProfit_IsZero()
    {
        var state = CEOState.Initial;
        Assert.Equal(0, state.TotalProfit);
    }

    [Fact]
    public void Initial_EvilScore_IsZero()
    {
        var state = CEOState.Initial;
        Assert.Equal(0, state.EvilScore);
    }

    [Fact]
    public void WithProfitAdded_AccumulatesTotal()
    {
        var state = CEOState.Initial;

        state = state.WithProfitAdded(1000);
        state = state.WithProfitAdded(500);

        Assert.Equal(1500, state.TotalProfit);
    }

    [Fact]
    public void ParachutePayout_InactiveCEO_ReturnsMinimal()
    {
        // Inactive CEO (no cards played) gets only base payout
        var state = CEOState.Initial
            .WithProfitAdded(1000) // Even with big profit
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete(); // 4 quarters but no cards

        // TotalCardsPlayed = 0 -> minimal parachute
        Assert.Equal(10, state.ParachutePayout);
        Assert.Contains("no strategic initiatives", state.ParachuteDescription);
    }

    [Fact]
    public void ParachutePayout_ActiveCEO_AccumulatesWithTenure()
    {
        // Active CEO (played cards) gets full calculation
        var state = CEOState.Initial
            .WithCardsPlayedRecorded(5) // Played some cards
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete(); // 4 quarters

        // Base: $10M
        // Tenure: 4 * $3M = $12M
        // Evil: 0
        // Total = 10 + 12 = $22M
        Assert.Equal(22, state.ParachutePayout);
        Assert.Contains("$22M", state.ParachuteDescription);
    }

    [Fact]
    public void ParachutePayout_WithEvilScore_ReducesPayout()
    {
        var state = CEOState.Initial
            .WithCardsPlayedRecorded(10) // Active CEO
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete()
            .WithQuarterComplete() // 5 quarters
            .WithEvilScoreChange(5);

        // Base: $10M
        // Tenure: 5 * $3M = $15M
        // Evil penalty: 5 * $2M = -$10M
        // Total = 10 + 15 - 10 = $15M
        Assert.Equal(15, state.ParachutePayout);
    }

    [Fact]
    public void WithEvilScoreChange_Accumulates()
    {
        var state = CEOState.Initial;

        state = state.WithEvilScoreChange(1);
        state = state.WithEvilScoreChange(2);

        Assert.Equal(3, state.EvilScore);
    }

    [Fact]
    public void WithQuarterComplete_IncrementsQuartersSurvived()
    {
        var state = CEOState.Initial;

        var next = state.WithQuarterComplete();

        Assert.Equal(1, next.QuartersSurvived);
    }

    [Fact]
    public void WithQuarterComplete_IncrementsBoardPressure_Every2Quarters()
    {
        var state = CEOState.Initial;

        // After Q1: pressure = 1/2 = 0
        var next = state.WithQuarterComplete();
        Assert.Equal(0, next.BoardPressureLevel);

        // After Q2: pressure = 2/2 = 1
        next = next.WithQuarterComplete();
        Assert.Equal(1, next.BoardPressureLevel);
    }

    [Fact]
    public void WithQuarterComplete_MultipleQuarters_TracksCorrectly()
    {
        var state = CEOState.Initial;

        state = state.WithQuarterComplete(); // Q1 done, pressure = 0
        state = state.WithQuarterComplete(); // Q2 done, pressure = 1
        state = state.WithQuarterComplete(); // Q3 done, pressure = 1
        state = state.WithQuarterComplete(); // Q4 done, pressure = 2

        Assert.Equal(4, state.QuartersSurvived);
        Assert.Equal(2, state.BoardPressureLevel);
    }

    [Fact]
    public void CEOState_IsImmutable()
    {
        var state = CEOState.Initial;
        var next = state.WithQuarterComplete();

        Assert.Equal(0, state.QuartersSurvived);
        Assert.Equal(1, state.BoardPressureLevel);
        Assert.NotSame(state, next);
    }
}
