namespace InertiCorp.Core.Tests;

public class EventDeckTests
{
    private static List<EventCard> CreateTestCards(int count)
    {
        var cards = new List<EventCard>();
        for (int i = 1; i <= count; i++)
        {
            var choices = new List<Choice>
            {
                new($"CHC_{i}_A", "Option A", Array.Empty<IEffect>()),
                new($"CHC_{i}_B", "Option B", Array.Empty<IEffect>())
            };
            cards.Add(new EventCard($"EVT_{i}", $"Event {i}", $"Description {i}", choices));
        }
        return cards;
    }

    [Fact]
    public void Constructor_InitializesWithCards()
    {
        var cards = CreateTestCards(5);
        var deck = new EventDeck(cards);

        Assert.Equal(5, deck.DrawPileCount);
        Assert.Equal(0, deck.DiscardPileCount);
    }

    [Fact]
    public void Shuffle_RandomizesDrawPile()
    {
        var cards = CreateTestCards(10);
        var deck = new EventDeck(cards);
        var rng = new SeededRng(12345);

        deck.Shuffle(rng);

        // Draw all cards and check they're not in original order
        var drawnIds = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            drawnIds.Add(deck.Draw().EventId);
        }

        var originalIds = cards.Select(c => c.EventId).ToList();
        Assert.NotEqual(originalIds, drawnIds); // Should be shuffled
        Assert.Equal(originalIds.OrderBy(x => x), drawnIds.OrderBy(x => x)); // Same cards
    }

    [Fact]
    public void Shuffle_SameSeed_SameOrder()
    {
        var cards = CreateTestCards(10);
        var deck1 = new EventDeck(cards);
        var deck2 = new EventDeck(cards);
        var rng1 = new SeededRng(99999);
        var rng2 = new SeededRng(99999);

        deck1.Shuffle(rng1);
        deck2.Shuffle(rng2);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(deck1.Draw().EventId, deck2.Draw().EventId);
        }
    }

    [Fact]
    public void Draw_ReturnsNextCard()
    {
        var cards = CreateTestCards(3);
        var deck = new EventDeck(cards);

        var card = deck.Draw();

        Assert.NotNull(card);
        Assert.Equal(2, deck.DrawPileCount);
        Assert.Equal(1, deck.DiscardPileCount);
    }

    [Fact]
    public void Draw_MovesCardToDiscard()
    {
        var cards = CreateTestCards(3);
        var deck = new EventDeck(cards);

        deck.Draw();
        deck.Draw();

        Assert.Equal(1, deck.DrawPileCount);
        Assert.Equal(2, deck.DiscardPileCount);
    }

    [Fact]
    public void Draw_WhenEmpty_ReshufflesDiscard()
    {
        var cards = CreateTestCards(3);
        var deck = new EventDeck(cards);
        var rng = new SeededRng(123);

        // Draw all 3 cards
        deck.Draw();
        deck.Draw();
        deck.Draw();

        Assert.Equal(0, deck.DrawPileCount);
        Assert.Equal(3, deck.DiscardPileCount);

        // Next draw should reshuffle
        var card = deck.DrawWithReshuffle(rng);

        Assert.NotNull(card);
        Assert.Equal(2, deck.DrawPileCount); // 3 reshuffled, 1 drawn
        Assert.Equal(1, deck.DiscardPileCount); // The newly drawn card
    }

    [Fact]
    public void Draw_CyclesCorrectly()
    {
        var cards = CreateTestCards(3);
        var deck = new EventDeck(cards);
        var rng = new SeededRng(456);

        // Draw 6 cards (2 full cycles)
        var drawnIds = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            var card = deck.DrawWithReshuffle(rng);
            drawnIds.Add(card.EventId);
        }

        // Should have all 3 cards twice
        Assert.Equal(6, drawnIds.Count);
        var uniqueIds = drawnIds.Distinct().ToList();
        Assert.Equal(3, uniqueIds.Count);
    }

    [Fact]
    public void DrawWithReshuffle_SameSeed_SameSequence()
    {
        var cards = CreateTestCards(5);
        var deck1 = new EventDeck(cards);
        var deck2 = new EventDeck(cards);
        var rng1 = new SeededRng(777);
        var rng2 = new SeededRng(777);

        // Initial shuffle
        deck1.Shuffle(rng1);
        deck2.Shuffle(rng2);

        // Draw 10 cards (triggers reshuffle)
        for (int i = 0; i < 10; i++)
        {
            var card1 = deck1.DrawWithReshuffle(rng1);
            var card2 = deck2.DrawWithReshuffle(rng2);
            Assert.Equal(card1.EventId, card2.EventId);
        }
    }

    [Fact]
    public void Peek_ReturnsTopCardWithoutRemoving()
    {
        var cards = CreateTestCards(3);
        var deck = new EventDeck(cards);

        var peeked = deck.Peek();
        var drawn = deck.Draw();

        Assert.Equal(peeked.EventId, drawn.EventId);
    }

    [Fact]
    public void IsEmpty_TrueWhenNoCardsInDrawPile()
    {
        var cards = CreateTestCards(2);
        var deck = new EventDeck(cards);

        Assert.False(deck.IsDrawPileEmpty);

        deck.Draw();
        Assert.False(deck.IsDrawPileEmpty);

        deck.Draw();
        Assert.True(deck.IsDrawPileEmpty);
    }
}
