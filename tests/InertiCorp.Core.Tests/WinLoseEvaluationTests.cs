namespace InertiCorp.Core.Tests;

public class WinLoseEvaluationTests
{
    private static Objective CreateObjective(string id, Meter meter, int threshold)
    {
        return new Objective(
            id,
            $"Test {id}",
            $"Keep {meter} >= {threshold}",
            new MeterThresholdCondition(meter, threshold)
        );
    }

    private static EventDeck CreateSimpleDeck()
    {
        return new EventDeck(new[]
        {
            new EventCard("EVT_NOOP", "No-op Event", "Does nothing",
                new List<Choice>
                {
                    new("CHC_NOOP", "Do nothing", Array.Empty<IEffect>()),
                    new("CHC_NOOP2", "Also nothing", Array.Empty<IEffect>())
                })
        });
    }

    private static GameState AdvanceToTurn12(GameState state, IRng rng)
    {
        var current = state;
        while (current.Turn.TurnNumber < 12 && !current.IsLost)
        {
            // Use fixed choice ID since we know the deck
            var input = new TurnInput("CHC_NOOP");
            (current, _) = GameEngine.AdvanceTurn(current, input, rng);
        }
        return current;
    }

    #region ObjectiveResult Tests

    [Fact]
    public void ObjectiveResult_ContainsObjectiveAndStatus()
    {
        var objective = CreateObjective("OBJ_1", Meter.Morale, 50);
        var result = new ObjectiveResult(objective, IsMet: true);

        Assert.Same(objective, result.Objective);
        Assert.True(result.IsMet);
    }

    #endregion

    #region Win Condition Tests

    [Fact]
    public void AfterTurn12_TwoOfThreeObjectivesMet_IsWonTrue()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),    // Will be met (default 50)
            CreateObjective("OBJ_2", Meter.Delivery, 40),  // Will be met (default 50)
            CreateObjective("OBJ_3", Meter.Governance, 90) // Won't be met
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 123, deck: deck, objectivePool: pool);
        var rng = new SeededRng(123);

        // Advance to turn 12
        state = AdvanceToTurn12(state, rng);

        // Complete turn 12
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.True(finalState.IsWon);
        Assert.False(finalState.IsLost);
    }

    [Fact]
    public void AfterTurn12_OneOfThreeObjectivesMet_IsWonFalse()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),    // Will be met (default 50)
            CreateObjective("OBJ_2", Meter.Delivery, 90),  // Won't be met
            CreateObjective("OBJ_3", Meter.Governance, 90) // Won't be met
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 456, deck: deck, objectivePool: pool);
        var rng = new SeededRng(456);

        state = AdvanceToTurn12(state, rng);
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.False(finalState.IsWon);
    }

    [Fact]
    public void AfterTurn12_ThreeOfThreeObjectivesMet_IsWonTrue()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),
            CreateObjective("OBJ_2", Meter.Delivery, 40),
            CreateObjective("OBJ_3", Meter.Governance, 40)
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 789, deck: deck, objectivePool: pool);
        var rng = new SeededRng(789);

        state = AdvanceToTurn12(state, rng);
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.True(finalState.IsWon);
    }

    #endregion

    #region Loss Override Tests

    [Fact]
    public void ThreeObjectivesMet_ButMoraleZero_IsLostTrue_IsWonFalse()
    {
        // Create a deck that drains morale to zero
        var drainMoraleDeck = new EventDeck(new[]
        {
            new EventCard("EVT_DRAIN", "Drain Morale", "Drains morale",
                new List<Choice>
                {
                    new("CHC_DRAIN", "Drain", new IEffect[] { new MeterEffect(Meter.Morale, -10) }),
                    new("CHC_SKIP", "Skip", Array.Empty<IEffect>())
                })
        });

        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Delivery, 40),
            CreateObjective("OBJ_2", Meter.Governance, 40),
            CreateObjective("OBJ_3", Meter.Alignment, 40)
        };
        var pool = new ObjectivePool(objectives);
        var state = GameState.NewGame(seed: 111, deck: drainMoraleDeck, objectivePool: pool);
        var rng = new SeededRng(111);

        // Drain morale to zero by choosing drain repeatedly
        var current = state;
        while (!current.IsLost && current.Turn.TurnNumber <= 12)
        {
            var input = new TurnInput("CHC_DRAIN");
            (current, _) = GameEngine.AdvanceTurn(current, input, rng);
        }

        Assert.True(current.IsLost);
        Assert.False(current.IsWon);
    }

    [Fact]
    public void LossCondition_OverridesWin()
    {
        // Even if objectives would be met, loss takes precedence
        var drainRunwayDeck = new EventDeck(new[]
        {
            new EventCard("EVT_DRAIN", "Drain Runway", "Drains runway",
                new List<Choice>
                {
                    new("CHC_DRAIN", "Drain", new IEffect[] { new MeterEffect(Meter.Runway, -10) }),
                    new("CHC_SKIP", "Skip", Array.Empty<IEffect>())
                })
        });

        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),
            CreateObjective("OBJ_2", Meter.Delivery, 40),
            CreateObjective("OBJ_3", Meter.Governance, 40)
        };
        var pool = new ObjectivePool(objectives);
        var state = GameState.NewGame(seed: 222, deck: drainRunwayDeck, objectivePool: pool);
        var rng = new SeededRng(222);

        var current = state;
        while (!current.IsLost && current.Turn.TurnNumber <= 12)
        {
            var input = new TurnInput("CHC_DRAIN");
            (current, _) = GameEngine.AdvanceTurn(current, input, rng);
        }

        Assert.True(current.IsLost);
        Assert.False(current.IsWon);
    }

    #endregion

    #region ObjectiveResults Tests

    [Fact]
    public void AfterTurn12_ObjectiveResults_ContainsAllObjectives()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),
            CreateObjective("OBJ_2", Meter.Delivery, 40),
            CreateObjective("OBJ_3", Meter.Governance, 90)
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 333, deck: deck, objectivePool: pool);
        var rng = new SeededRng(333);

        state = AdvanceToTurn12(state, rng);
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(3, finalState.ObjectiveResults.Count);
    }

    [Fact]
    public void AfterTurn12_ObjectiveResults_ShowsCorrectMetStatus()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),    // Met
            CreateObjective("OBJ_2", Meter.Delivery, 40),  // Met
            CreateObjective("OBJ_3", Meter.Governance, 90) // Not met
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 444, deck: deck, objectivePool: pool);
        var rng = new SeededRng(444);

        state = AdvanceToTurn12(state, rng);
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        var metCount = finalState.ObjectiveResults.Count(r => r.IsMet);
        var failedCount = finalState.ObjectiveResults.Count(r => !r.IsMet);

        Assert.Equal(2, metCount);
        Assert.Equal(1, failedCount);
    }

    [Fact]
    public void BeforeTurn12_ObjectiveResults_IsEmpty()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),
            CreateObjective("OBJ_2", Meter.Delivery, 40),
            CreateObjective("OBJ_3", Meter.Governance, 40)
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 555, deck: deck, objectivePool: pool);
        var rng = new SeededRng(555);

        // Advance only one turn
        var input = new TurnInput("CHC_NOOP");
        var (afterTurn1, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Empty(afterTurn1.ObjectiveResults);
        Assert.False(afterTurn1.IsWon);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void NoObjectives_AfterTurn12_IsWonFalse()
    {
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 666, deck: deck);
        var rng = new SeededRng(666);

        state = AdvanceToTurn12(state, rng);
        var input = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.False(finalState.IsWon);
        Assert.Empty(finalState.ObjectiveResults);
    }

    [Fact]
    public void EvaluationOnlyHappensOnce_AtTurn12()
    {
        var objectives = new List<Objective>
        {
            CreateObjective("OBJ_1", Meter.Morale, 40),
            CreateObjective("OBJ_2", Meter.Delivery, 40),
            CreateObjective("OBJ_3", Meter.Governance, 40)
        };
        var pool = new ObjectivePool(objectives);
        var deck = CreateSimpleDeck();
        var state = GameState.NewGame(seed: 777, deck: deck, objectivePool: pool);
        var rng = new SeededRng(777);

        // Advance to turn 11
        while (state.Turn.TurnNumber < 11)
        {
            var input = new TurnInput("CHC_NOOP");
            (state, _) = GameEngine.AdvanceTurn(state, input, rng);
        }

        Assert.False(state.IsWon);
        Assert.Empty(state.ObjectiveResults);

        // Complete turn 11 -> turn 12
        var input11 = new TurnInput("CHC_NOOP");
        var (stateTurn12, _) = GameEngine.AdvanceTurn(state, input11, rng);

        Assert.False(stateTurn12.IsWon);
        Assert.Empty(stateTurn12.ObjectiveResults);

        // Complete turn 12 -> evaluation happens
        var input12 = new TurnInput("CHC_NOOP");
        var (finalState, _) = GameEngine.AdvanceTurn(stateTurn12, input12, rng);

        Assert.True(finalState.IsWon);
        Assert.Equal(3, finalState.ObjectiveResults.Count);
    }

    #endregion
}
