namespace InertiCorp.Core.Tests;

public class ObjectiveDrawingTests
{
    private static List<Objective> CreateTestObjectives(int count)
    {
        var objectives = new List<Objective>();
        for (int i = 1; i <= count; i++)
        {
            objectives.Add(new Objective(
                $"OBJ_TEST_{i}",
                $"Test Objective {i}",
                $"Description for objective {i}",
                new MeterThresholdCondition(Meter.Morale, 50)
            ));
        }
        return objectives;
    }

    #region ObjectivePool Tests

    [Fact]
    public void ObjectivePool_HoldsAvailableObjectives()
    {
        var objectives = CreateTestObjectives(5);
        var pool = new ObjectivePool(objectives);

        Assert.Equal(5, pool.Count);
    }

    [Fact]
    public void ObjectivePool_Draw_ReturnsRequestedCount()
    {
        var objectives = CreateTestObjectives(5);
        var pool = new ObjectivePool(objectives);
        var rng = new SeededRng(123);

        var drawn = pool.Draw(3, rng);

        Assert.Equal(3, drawn.Count);
    }

    [Fact]
    public void ObjectivePool_Draw_ThrowsIfPoolTooSmall()
    {
        var objectives = CreateTestObjectives(2);
        var pool = new ObjectivePool(objectives);
        var rng = new SeededRng(123);

        Assert.Throws<InvalidOperationException>(() => pool.Draw(3, rng));
    }

    [Fact]
    public void ObjectivePool_Draw_NoDuplicates()
    {
        var objectives = CreateTestObjectives(10);
        var pool = new ObjectivePool(objectives);
        var rng = new SeededRng(456);

        var drawn = pool.Draw(3, rng);
        var ids = drawn.Select(o => o.ObjectiveId).ToList();

        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public void ObjectivePool_Draw_IsDeterministic()
    {
        var objectives1 = CreateTestObjectives(10);
        var objectives2 = CreateTestObjectives(10);
        var pool1 = new ObjectivePool(objectives1);
        var pool2 = new ObjectivePool(objectives2);
        var rng1 = new SeededRng(789);
        var rng2 = new SeededRng(789);

        var drawn1 = pool1.Draw(3, rng1);
        var drawn2 = pool2.Draw(3, rng2);

        Assert.Equal(
            drawn1.Select(o => o.ObjectiveId),
            drawn2.Select(o => o.ObjectiveId)
        );
    }

    [Fact]
    public void ObjectivePool_Draw_DifferentSeeds_DifferentResults()
    {
        var objectives1 = CreateTestObjectives(10);
        var objectives2 = CreateTestObjectives(10);
        var pool1 = new ObjectivePool(objectives1);
        var pool2 = new ObjectivePool(objectives2);
        var rng1 = new SeededRng(111);
        var rng2 = new SeededRng(222);

        var drawn1 = pool1.Draw(3, rng1);
        var drawn2 = pool2.Draw(3, rng2);

        // With 10 objectives and 3 draws, very unlikely to get same result
        Assert.NotEqual(
            drawn1.Select(o => o.ObjectiveId).ToList(),
            drawn2.Select(o => o.ObjectiveId).ToList()
        );
    }

    #endregion

    #region GameState Objective Drawing Tests

    [Fact]
    public void GameState_NewGame_WithObjectivePool_DrawsThreeObjectives()
    {
        var objectives = CreateTestObjectives(5);
        var pool = new ObjectivePool(objectives);

        var state = GameState.NewGame(seed: 123, objectivePool: pool);

        Assert.Equal(3, state.ActiveObjectives.Count);
    }

    [Fact]
    public void GameState_NewGame_WithObjectivePool_NoDuplicates()
    {
        var objectives = CreateTestObjectives(10);
        var pool = new ObjectivePool(objectives);

        var state = GameState.NewGame(seed: 456, objectivePool: pool);
        var ids = state.ActiveObjectives.Select(o => o.ObjectiveId).ToList();

        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public void GameState_NewGame_WithObjectivePool_IsDeterministic()
    {
        var objectives1 = CreateTestObjectives(10);
        var objectives2 = CreateTestObjectives(10);
        var pool1 = new ObjectivePool(objectives1);
        var pool2 = new ObjectivePool(objectives2);

        var state1 = GameState.NewGame(seed: 789, objectivePool: pool1);
        var state2 = GameState.NewGame(seed: 789, objectivePool: pool2);

        Assert.Equal(
            state1.ActiveObjectives.Select(o => o.ObjectiveId),
            state2.ActiveObjectives.Select(o => o.ObjectiveId)
        );
    }

    [Fact]
    public void GameState_NewGame_WithPoolTooSmall_ThrowsInvalidOperationException()
    {
        var objectives = CreateTestObjectives(2);
        var pool = new ObjectivePool(objectives);

        Assert.Throws<InvalidOperationException>(() =>
            GameState.NewGame(seed: 123, objectivePool: pool));
    }

    [Fact]
    public void GameState_NewGame_WithDeckAndObjectivePool()
    {
        var objectives = CreateTestObjectives(5);
        var pool = new ObjectivePool(objectives);
        var deck = new EventDeck(new[]
        {
            new EventCard("EVT_1", "Event 1", "Desc",
                new List<Choice>
                {
                    new("CHC_1", "Choice 1", Array.Empty<IEffect>()),
                    new("CHC_2", "Choice 2", Array.Empty<IEffect>())
                })
        });

        var state = GameState.NewGame(seed: 123, deck: deck, objectivePool: pool);

        Assert.NotNull(state.Deck);
        Assert.Equal(3, state.ActiveObjectives.Count);
    }

    [Fact]
    public void GameState_NewGame_WithoutObjectivePool_HasEmptyActiveObjectives()
    {
        var state = GameState.NewGame(seed: 123);

        Assert.Empty(state.ActiveObjectives);
    }

    #endregion
}
