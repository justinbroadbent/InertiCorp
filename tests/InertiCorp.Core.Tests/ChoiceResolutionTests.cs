namespace InertiCorp.Core.Tests;

public class ChoiceResolutionTests
{
    private static EventDeck CreateTestDeck()
    {
        var cards = new List<EventCard>
        {
            new EventCard(
                "EVT_TEST_1",
                "Test Event 1",
                "A test event",
                new List<Choice>
                {
                    new("CHC_BOOST", "Boost morale", new[] { new MeterEffect(Meter.Morale, 10) }),
                    new("CHC_DRAIN", "Drain morale", new[] { new MeterEffect(Meter.Morale, -10) })
                }
            ),
            new EventCard(
                "EVT_TEST_2",
                "Test Event 2",
                "Another test event",
                new List<Choice>
                {
                    new("CHC_DELIVER", "Focus on delivery", new[] { new MeterEffect(Meter.Delivery, 15) }),
                    new("CHC_GOVERN", "Focus on governance", new[] { new MeterEffect(Meter.Governance, 15) })
                }
            )
        };
        return new EventDeck(cards);
    }

    [Fact]
    public void TurnInput_ContainsChosenChoiceId()
    {
        var input = new TurnInput("CHC_BOOST");

        Assert.Equal("CHC_BOOST", input.ChosenChoiceId);
    }

    [Fact]
    public void AdvanceTurn_ChoiceA_AppliesEffectsA()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 123, deck);
        var rng = new SeededRng(123);

        // Peek to see what card we'll get
        var expectedCard = deck.Peek();
        var choiceA = expectedCard.Choices[0];
        var input = new TurnInput(choiceA.ChoiceId);

        var (newState, log) = GameEngine.AdvanceTurn(state, input, rng);

        // Effects should have been applied
        Assert.NotEqual(state.Org, newState.Org);
    }

    [Fact]
    public void AdvanceTurn_ChoiceB_AppliesEffectsB()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 456, deck);
        var rng = new SeededRng(456);

        var expectedCard = deck.Peek();
        var choiceB = expectedCard.Choices[1];
        var input = new TurnInput(choiceB.ChoiceId);

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        // Different choice = different effects
        Assert.NotEqual(state.Org, newState.Org);
    }

    [Fact]
    public void AdvanceTurn_InvalidChoiceId_ThrowsArgumentException()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 789, deck);
        var rng = new SeededRng(789);
        var input = new TurnInput("CHC_INVALID");

        var ex = Assert.Throws<ArgumentException>(() =>
            GameEngine.AdvanceTurn(state, input, rng));

        Assert.Contains("CHC_INVALID", ex.Message);
    }

    [Fact]
    public void AdvanceTurn_TurnLog_IncludesEventId()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 111, deck);
        var rng = new SeededRng(111);

        var expectedCard = deck.Peek();
        var input = new TurnInput(expectedCard.Choices[0].ChoiceId);

        var (_, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(expectedCard.EventId, log.DrawnEventId);
    }

    [Fact]
    public void AdvanceTurn_TurnLog_IncludesChoiceId()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 222, deck);
        var rng = new SeededRng(222);

        var expectedCard = deck.Peek();
        var choiceId = expectedCard.Choices[0].ChoiceId;
        var input = new TurnInput(choiceId);

        var (_, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(choiceId, log.ChosenChoiceId);
    }

    [Fact]
    public void AdvanceTurn_TurnLog_IncludesMeterDeltas()
    {
        var deck = CreateTestDeck();
        var state = GameState.NewGame(seed: 333, deck);
        var rng = new SeededRng(333);

        var expectedCard = deck.Peek();
        var input = new TurnInput(expectedCard.Choices[0].ChoiceId);

        var (_, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Contains(log.Entries, e => e.Category == LogCategory.MeterChange);
    }

    [Fact]
    public void AdvanceTurn_AppliesAllEffectsInChoice()
    {
        // Create a choice with multiple effects
        var multiEffectCard = new EventCard(
            "EVT_MULTI",
            "Multi Effect",
            "Multiple effects",
            new List<Choice>
            {
                new("CHC_MULTI", "Do both", new IEffect[]
                {
                    new MeterEffect(Meter.Morale, 5),
                    new MeterEffect(Meter.Delivery, -5)
                }),
                new("CHC_NONE", "Do nothing", Array.Empty<IEffect>())
            }
        );

        var deck = new EventDeck(new[] { multiEffectCard });
        var state = GameState.NewGame(seed: 444, deck);
        var rng = new SeededRng(444);
        var input = new TurnInput("CHC_MULTI");

        var (newState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(65, newState.Org.Morale);  // 60 + 5
        Assert.Equal(55, newState.Org.Delivery); // 60 - 5
    }

    [Fact]
    public void AdvanceTurn_DrawsFromDeck()
    {
        var deck = CreateTestDeck();
        var initialDrawCount = deck.DrawPileCount;
        var state = GameState.NewGame(seed: 555, deck);
        var rng = new SeededRng(555);

        var card = deck.Peek();
        var input = new TurnInput(card.Choices[0].ChoiceId);

        GameEngine.AdvanceTurn(state, input, rng);

        // Deck should have one less card in draw pile
        Assert.Equal(initialDrawCount - 1, state.Deck!.DrawPileCount);
    }

    [Fact]
    public void AdvanceTurn_IsDeterministic()
    {
        var deck1 = CreateTestDeck();
        var deck2 = CreateTestDeck();
        var state1 = GameState.NewGame(seed: 666, deck1);
        var state2 = GameState.NewGame(seed: 666, deck2);
        var rng1 = new SeededRng(666);
        var rng2 = new SeededRng(666);

        var card1 = deck1.Peek();
        var card2 = deck2.Peek();
        var input1 = new TurnInput(card1.Choices[0].ChoiceId);
        var input2 = new TurnInput(card2.Choices[0].ChoiceId);

        var (newState1, log1) = GameEngine.AdvanceTurn(state1, input1, rng1);
        var (newState2, log2) = GameEngine.AdvanceTurn(state2, input2, rng2);

        Assert.Equal(newState1.Org.Morale, newState2.Org.Morale);
        Assert.Equal(log1.DrawnEventId, log2.DrawnEventId);
    }
}
