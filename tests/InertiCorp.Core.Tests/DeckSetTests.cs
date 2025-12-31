namespace InertiCorp.Core.Tests;

using InertiCorp.Core.Content;

public class DeckSetTests
{
    [Fact]
    public void DeckSet_HoldsThreeDecks()
    {
        var crisisCards = new[] { CrisisEvents.All[0] };
        var boardCards = new[] { BoardEvents.All[0] };
        var projectCards = new[] { ProjectEvents.All[0] };

        var deckSet = new DeckSet(
            new EventDeck(crisisCards),
            new EventDeck(boardCards),
            new EventDeck(projectCards)
        );

        Assert.NotNull(deckSet.CrisisDeck);
        Assert.NotNull(deckSet.BoardDeck);
        Assert.NotNull(deckSet.ProjectDeck);
    }

    [Fact]
    public void GetCrisisDeck_ReturnsCrisisDeck()
    {
        var crisisCards = new[] { CrisisEvents.All[0] };
        var boardCards = new[] { BoardEvents.All[0] };
        var projectCards = new[] { ProjectEvents.All[0] };

        var deckSet = new DeckSet(
            new EventDeck(crisisCards),
            new EventDeck(boardCards),
            new EventDeck(projectCards)
        );

        var deck = deckSet.GetCrisisDeck();

        Assert.Same(deckSet.CrisisDeck, deck);
    }

    [Fact]
    public void DrawCrisis_IsDeterministic()
    {
        var deckSet1 = CreateTestDeckSet();
        var deckSet2 = CreateTestDeckSet();
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        var (_, card1) = deckSet1.DrawCrisis(rng1);
        var (_, card2) = deckSet2.DrawCrisis(rng2);

        Assert.Equal(card1.EventId, card2.EventId);
    }

    [Fact]
    public void DrawCrisis_ReturnsUpdatedDeckSet()
    {
        var deckSet = CreateTestDeckSet();
        var rng = new SeededRng(42);
        var originalCrisisCount = deckSet.CrisisDeck.DrawPileCount;

        var (newDeckSet, card) = deckSet.DrawCrisis(rng);

        // Original unchanged
        Assert.Equal(originalCrisisCount, deckSet.CrisisDeck.DrawPileCount);
        // New is different
        Assert.NotSame(deckSet, newDeckSet);
    }

    private static DeckSet CreateTestDeckSet()
    {
        return new DeckSet(
            new EventDeck(CrisisEvents.All),
            new EventDeck(BoardEvents.All),
            new EventDeck(ProjectEvents.All)
        );
    }
}
