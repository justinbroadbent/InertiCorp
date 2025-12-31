using InertiCorp.Core.Cards;

namespace InertiCorp.Core.Tests;

using InertiCorp.Core.Content;

public class QuarterEngineTests
{
    private static DeckSet CreateTestDeckSet()
    {
        return new DeckSet(
            new EventDeck(CrisisEvents.All),
            new EventDeck(BoardEvents.All),
            new EventDeck(ProjectEvents.All)
        );
    }

    private static IReadOnlyList<PlayableCard> CreateTestPlayableCards()
    {
        var outcomes = new OutcomeProfile(
            Good: new IEffect[] { new MeterEffect(Meter.Delivery, 10) },
            Expected: new IEffect[] { new MeterEffect(Meter.Delivery, 5) },
            Bad: new IEffect[] { new MeterEffect(Meter.Delivery, -5) }
        );

        return new[]
        {
            new PlayableCard("TEST_CARD_1", "Test Card 1", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_2", "Test Card 2", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_3", "Test Card 3", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_4", "Test Card 4", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_5", "Test Card 5", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_6", "Test Card 6", "Test description", "Flavor", outcomes),
            new PlayableCard("TEST_CARD_7", "Test Card 7", "Test description", "Flavor", outcomes),
        };
    }

    [Fact]
    public void NewGame_StartsInBoardDemandPhase()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);

        Assert.Equal(GamePhase.BoardDemand, state.Quarter.Phase);
    }

    [Fact]
    public void Advance_BoardDemand_MovesToPlayCards()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);
        var rng = new SeededRng(42);

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);

        Assert.Equal(GamePhase.PlayCards, newState.Quarter.Phase);
    }

    [Fact]
    public void Advance_Crisis_RequiresChoiceId()
    {
        var state = AdvanceToPhase(GamePhase.Crisis);
        var choiceId = state.CurrentCrisis!.Choices[0].ChoiceId;

        var (newState, log) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), new SeededRng(42));

        Assert.NotNull(newState);
        Assert.NotNull(log);
    }

    [Fact]
    public void Advance_Crisis_InvalidChoiceId_Throws()
    {
        var state = AdvanceToPhase(GamePhase.Crisis);

        Assert.Throws<ArgumentException>(() =>
            QuarterEngine.Advance(state, QuarterInput.ForChoice("INVALID_CHOICE"), new SeededRng(42)));
    }

    [Fact]
    public void Advance_FromCrisis_MovesToResolution()
    {
        var state = AdvanceToPhase(GamePhase.Crisis);
        var choiceId = state.CurrentCrisis!.Choices[0].ChoiceId;

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), new SeededRng(42));

        Assert.Equal(GamePhase.Resolution, newState.Quarter.Phase);
    }

    [Fact]
    public void Advance_PlayCards_WithNoCard_MovesToCrisis()
    {
        var state = AdvanceToPhase(GamePhase.PlayCards);

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, new SeededRng(42));

        Assert.Equal(GamePhase.Crisis, newState.Quarter.Phase);
        Assert.NotNull(newState.CurrentCrisis);
    }

    [Fact]
    public void Advance_InResolution_MovesToNextQuarterBoardDemand()
    {
        var state = AdvanceToPhase(GamePhase.Resolution);

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, new SeededRng(100));

        Assert.Equal(2, newState.Quarter.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, newState.Quarter.Phase);
    }

    [Fact]
    public void Advance_Resolution_IncrementsBoardPressure_Every2Quarters()
    {
        var state = AdvanceToPhase(GamePhase.Resolution);

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.Empty, new SeededRng(100));

        // After Q1: pressure = 1/2 = 0 (increases every 2 quarters)
        Assert.Equal(0, newState.CEO.BoardPressureLevel);
        Assert.Equal(1, newState.CEO.QuartersSurvived);
    }

    [Fact]
    public void Advance_IsDeterministic()
    {
        var deckSet1 = CreateTestDeckSet();
        var deckSet2 = CreateTestDeckSet();
        var state1 = QuarterGameState.NewGame(42, deckSet1);
        var state2 = QuarterGameState.NewGame(42, deckSet2);

        // Advance both through BoardDemand
        var (newState1, _) = QuarterEngine.Advance(state1, QuarterInput.Empty, new SeededRng(42));
        var (newState2, _) = QuarterEngine.Advance(state2, QuarterInput.Empty, new SeededRng(42));

        Assert.Equal(newState1.Quarter.Phase, newState2.Quarter.Phase);
        Assert.Equal(newState1.CurrentCrisis?.EventId, newState2.CurrentCrisis?.EventId);
    }

    [Fact]
    public void FullQuarter_CanBeSimulated()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);
        var rng = new SeededRng(42);

        // BoardDemand -> PlayCards
        (state, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
        Assert.Equal(GamePhase.PlayCards, state.Quarter.Phase);

        // PlayCards -> Crisis (skip playing cards)
        (state, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, rng);
        Assert.Equal(GamePhase.Crisis, state.Quarter.Phase);

        // Crisis -> Resolution
        var choiceId = state.CurrentCrisis!.Choices[0].ChoiceId;
        (state, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), rng);
        Assert.Equal(GamePhase.Resolution, state.Quarter.Phase);

        // Resolution -> Q2 BoardDemand
        (state, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);

        Assert.Equal(2, state.Quarter.QuarterNumber);
        Assert.Equal(GamePhase.BoardDemand, state.Quarter.Phase);
        Assert.Equal(1, state.CEO.QuartersSurvived);
    }

    [Fact]
    public void PlayCards_GeneratesEmailImmediately()
    {
        var state = AdvanceToPhaseWithCards(GamePhase.PlayCards);
        var cardId = state.Hand.Cards[0].CardId;
        var initialThreadCount = state.Inbox.ThreadCount;

        var (newState, _) = QuarterEngine.Advance(state, QuarterInput.ForPlayCard(cardId, true), new SeededRng(42));

        // Email thread should be created immediately when card is played
        Assert.True(newState.Inbox.ThreadCount > initialThreadCount);
    }

    private QuarterGameState AdvanceToPhase(GamePhase targetPhase)
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);
        var rng = new SeededRng(42);

        while (state.Quarter.Phase != targetPhase)
        {
            if (state.Quarter.IsCrisisPhase && state.CurrentCrisis != null)
            {
                var choiceId = state.CurrentCrisis.Choices[0].ChoiceId;
                (state, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), rng);
            }
            else if (state.Quarter.IsPlayCardsPhase)
            {
                (state, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, rng);
            }
            else
            {
                (state, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
            }
        }

        return state;
    }

    private QuarterGameState AdvanceToPhaseWithCards(GamePhase targetPhase)
    {
        var deckSet = CreateTestDeckSet();
        var playableCards = CreateTestPlayableCards();
        var state = QuarterGameState.NewGame(42, deckSet, playableCards);
        var rng = new SeededRng(42);

        while (state.Quarter.Phase != targetPhase)
        {
            if (state.Quarter.IsCrisisPhase && state.CurrentCrisis != null)
            {
                var choiceId = state.CurrentCrisis.Choices[0].ChoiceId;
                (state, _) = QuarterEngine.Advance(state, QuarterInput.ForChoice(choiceId), rng);
            }
            else if (state.Quarter.IsPlayCardsPhase)
            {
                (state, _) = QuarterEngine.Advance(state, QuarterInput.EndCardPlay, rng);
            }
            else
            {
                (state, _) = QuarterEngine.Advance(state, QuarterInput.Empty, rng);
            }
        }

        return state;
    }
}
