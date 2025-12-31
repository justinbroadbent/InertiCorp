namespace InertiCorp.Core.Tests;

public class QuarterStateTests
{
    [Fact]
    public void GamePhase_HasFourPhases()
    {
        var phases = Enum.GetValues<GamePhase>();
        Assert.Equal(4, phases.Length);
        Assert.Contains(GamePhase.BoardDemand, phases);
        Assert.Contains(GamePhase.PlayCards, phases);
        Assert.Contains(GamePhase.Crisis, phases);
        Assert.Contains(GamePhase.Resolution, phases);
    }

    [Fact]
    public void Initial_StartsAtQuarter1_BoardDemandPhase()
    {
        var state = QuarterState.Initial;

        Assert.Equal(1, state.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, state.Phase);
    }

    [Fact]
    public void NextPhase_FromBoardDemand_GoesToPlayCards()
    {
        var state = QuarterState.Initial;

        var next = state.NextPhase();

        Assert.Equal(1, next.QuarterNumber);
        Assert.Equal(GamePhase.PlayCards, next.Phase);
    }

    [Fact]
    public void NextPhase_FromPlayCards_GoesToCrisis()
    {
        var state = new QuarterState(1, GamePhase.PlayCards);

        var next = state.NextPhase();

        Assert.Equal(1, next.QuarterNumber);
        Assert.Equal(GamePhase.Crisis, next.Phase);
    }

    [Fact]
    public void NextPhase_FromCrisis_GoesToResolution()
    {
        var state = new QuarterState(1, GamePhase.Crisis);

        var next = state.NextPhase();

        Assert.Equal(1, next.QuarterNumber);
        Assert.Equal(GamePhase.Resolution, next.Phase);
    }

    [Fact]
    public void NextPhase_FromResolution_GoesToNextQuarterBoardDemand()
    {
        var state = new QuarterState(1, GamePhase.Resolution);

        var next = state.NextPhase();

        Assert.Equal(2, next.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, next.Phase);
    }

    [Fact]
    public void NextPhase_MultipleQuarters_IncrementsCorrectly()
    {
        var state = QuarterState.Initial;

        // Complete quarter 1: BoardDemand -> PlayCards -> Crisis -> Resolution -> Q2 BoardDemand
        state = state.NextPhase(); // PlayCards
        state = state.NextPhase(); // Crisis
        state = state.NextPhase(); // Resolution
        state = state.NextPhase(); // Q2 BoardDemand

        Assert.Equal(2, state.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, state.Phase);

        // Complete quarter 2
        state = state.NextPhase().NextPhase().NextPhase().NextPhase();

        Assert.Equal(3, state.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, state.Phase);
    }

    [Fact]
    public void IsResolutionPhase_TrueOnlyInResolution()
    {
        Assert.False(new QuarterState(1, GamePhase.BoardDemand).IsResolutionPhase);
        Assert.False(new QuarterState(1, GamePhase.PlayCards).IsResolutionPhase);
        Assert.False(new QuarterState(1, GamePhase.Crisis).IsResolutionPhase);
        Assert.True(new QuarterState(1, GamePhase.Resolution).IsResolutionPhase);
    }

    [Fact]
    public void IsPlayerChoicePhase_TrueForCrisisAndPlayCards()
    {
        Assert.False(new QuarterState(1, GamePhase.BoardDemand).IsPlayerChoicePhase);
        Assert.True(new QuarterState(1, GamePhase.PlayCards).IsPlayerChoicePhase);
        Assert.True(new QuarterState(1, GamePhase.Crisis).IsPlayerChoicePhase);
        Assert.False(new QuarterState(1, GamePhase.Resolution).IsPlayerChoicePhase);
    }

    [Fact]
    public void IsCrisisPhase_TrueOnlyInCrisis()
    {
        Assert.False(new QuarterState(1, GamePhase.BoardDemand).IsCrisisPhase);
        Assert.False(new QuarterState(1, GamePhase.PlayCards).IsCrisisPhase);
        Assert.True(new QuarterState(1, GamePhase.Crisis).IsCrisisPhase);
        Assert.False(new QuarterState(1, GamePhase.Resolution).IsCrisisPhase);
    }

    [Fact]
    public void IsPlayCardsPhase_TrueOnlyInPlayCards()
    {
        Assert.False(new QuarterState(1, GamePhase.BoardDemand).IsPlayCardsPhase);
        Assert.True(new QuarterState(1, GamePhase.PlayCards).IsPlayCardsPhase);
        Assert.False(new QuarterState(1, GamePhase.Crisis).IsPlayCardsPhase);
        Assert.False(new QuarterState(1, GamePhase.Resolution).IsPlayCardsPhase);
    }

    [Fact]
    public void QuarterState_IsImmutable()
    {
        var state = QuarterState.Initial;
        var next = state.NextPhase();

        Assert.Equal(1, state.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, state.Phase);
        Assert.NotSame(state, next);
    }
}
