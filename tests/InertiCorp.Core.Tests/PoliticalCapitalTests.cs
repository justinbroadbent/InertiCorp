using InertiCorp.Core;
using InertiCorp.Core.Crisis;
using InertiCorp.Core.Content;

namespace InertiCorp.Core.Tests;

public class ResourceStateTests
{
    [Fact]
    public void Initial_StartsWithSomePC()
    {
        var resources = ResourceState.Initial;

        Assert.True(resources.PoliticalCapital > 0);
    }

    [Fact]
    public void PoliticalCapital_ClampsToMax()
    {
        var resources = new ResourceState(100);

        Assert.Equal(ResourceState.MaxPoliticalCapital, resources.PoliticalCapital);
    }

    [Fact]
    public void PoliticalCapital_ClampsToZero()
    {
        var resources = new ResourceState(-10);

        Assert.Equal(0, resources.PoliticalCapital);
    }

    [Fact]
    public void WithPoliticalCapitalChange_AddsCorrectly()
    {
        var resources = new ResourceState(5);

        var updated = resources.WithPoliticalCapitalChange(3);

        Assert.Equal(8, updated.PoliticalCapital);
    }

    [Fact]
    public void WithPoliticalCapitalChange_SubtractsCorrectly()
    {
        var resources = new ResourceState(5);

        var updated = resources.WithPoliticalCapitalChange(-3);

        Assert.Equal(2, updated.PoliticalCapital);
    }

    [Fact]
    public void CanAfford_TrueWhenSufficient()
    {
        var resources = new ResourceState(5);

        Assert.True(resources.CanAfford(5));
        Assert.True(resources.CanAfford(3));
    }

    [Fact]
    public void CanAfford_FalseWhenInsufficient()
    {
        var resources = new ResourceState(5);

        Assert.False(resources.CanAfford(6));
    }

    [Fact]
    public void WithSpend_DeductsCost()
    {
        var resources = new ResourceState(10);

        var updated = resources.WithSpend(4);

        Assert.Equal(6, updated.PoliticalCapital);
    }

    [Fact]
    public void WithSpend_ThrowsWhenInsufficient()
    {
        var resources = new ResourceState(3);

        Assert.Throws<InvalidOperationException>(() => resources.WithSpend(5));
    }

    [Fact]
    public void WithTurnEndAdjustments_GainsFromHighGovernance()
    {
        var resources = new ResourceState(5);
        var org = new OrgState(50, 50, 60, 50, 50); // Governance >= 60

        var updated = resources.WithTurnEndAdjustments(org);

        Assert.True(updated.PoliticalCapital > 5);
    }

    [Fact]
    public void WithTurnEndAdjustments_GainsFromHighAlignment()
    {
        var resources = new ResourceState(5);
        var org = new OrgState(50, 50, 50, 60, 50); // Alignment >= 60

        var updated = resources.WithTurnEndAdjustments(org);

        Assert.True(updated.PoliticalCapital > 5);
    }

    [Fact]
    public void WithTurnEndAdjustments_LosesFromLowMorale()
    {
        var resources = new ResourceState(5);
        var org = new OrgState(50, 25, 50, 50, 50); // Morale < 30

        var updated = resources.WithTurnEndAdjustments(org);

        Assert.True(updated.PoliticalCapital < 5);
    }

    [Fact]
    public void WithTurnEndAdjustments_DecaysWhenAboveThreshold()
    {
        var resources = new ResourceState(15); // > DecayThreshold
        var org = new OrgState(50, 50, 50, 50, 50); // Neutral

        var updated = resources.WithTurnEndAdjustments(org);

        Assert.True(updated.PoliticalCapital < 15);
    }
}

public class CrisisInstanceTests
{
    [Fact]
    public void IsActive_TrueWhenStatusActive()
    {
        var crisis = CreateTestCrisis(CrisisStatus.Active);

        Assert.True(crisis.IsActive);
    }

    [Fact]
    public void IsActive_FalseWhenMitigated()
    {
        var crisis = CreateTestCrisis(CrisisStatus.Mitigated);

        Assert.False(crisis.IsActive);
    }

    [Fact]
    public void IsOverdue_TrueWhenPastDeadline()
    {
        var crisis = CreateTestCrisis(deadline: 5);

        Assert.True(crisis.IsOverdue(6));
    }

    [Fact]
    public void IsOverdue_FalseBeforeDeadline()
    {
        var crisis = CreateTestCrisis(deadline: 5);

        Assert.False(crisis.IsOverdue(4));
    }

    [Fact]
    public void TurnsRemaining_CalculatesCorrectly()
    {
        var crisis = CreateTestCrisis(deadline: 5);

        Assert.Equal(3, crisis.TurnsRemaining(2));
        Assert.Equal(0, crisis.TurnsRemaining(6));
    }

    [Fact]
    public void WithReducedSeverity_LowersSeverity()
    {
        var crisis = CreateTestCrisis(severity: 4);

        var reduced = crisis.WithReducedSeverity(2);

        Assert.Equal(2, reduced.Severity);
    }

    [Fact]
    public void WithReducedSeverity_ClampsToMinimum()
    {
        var crisis = CreateTestCrisis(severity: 2);

        var reduced = crisis.WithReducedSeverity(5);

        Assert.Equal(1, reduced.Severity);
    }

    [Fact]
    public void WithExtendedDeadline_ExtendsDeadline()
    {
        var crisis = CreateTestCrisis(deadline: 5);

        var extended = crisis.WithExtendedDeadline(2);

        Assert.Equal(7, extended.DeadlineTurn);
    }

    [Fact]
    public void CanFullyMitigateWith_TrueWhenNoMinimum()
    {
        var crisis = CreateTestCrisis(minSpend: null);

        Assert.True(crisis.CanFullyMitigateWith(1));
    }

    [Fact]
    public void CanFullyMitigateWith_RequiresMinimumSpend()
    {
        var crisis = CreateTestCrisis(minSpend: 3);

        Assert.False(crisis.CanFullyMitigateWith(2));
        Assert.True(crisis.CanFullyMitigateWith(3));
        Assert.True(crisis.CanFullyMitigateWith(5));
    }

    private static CrisisInstance CreateTestCrisis(
        CrisisStatus status = CrisisStatus.Active,
        int deadline = 3,
        int severity = 3,
        int? minSpend = null) => new(
            CrisisId: "test_crisis",
            InstanceId: "inst_1",
            Title: "Test Crisis",
            Description: "A test crisis",
            Severity: severity,
            Tags: new[] { "test" },
            CreatedTurn: 1,
            DeadlineTurn: deadline,
            BaseImpact: new Dictionary<Meter, int> { { Meter.Morale, -5 } },
            OngoingImpact: new Dictionary<Meter, int>(),
            Status: status,
            OriginEventId: "event_1",
            MinimumSpendToFullyMitigate: minSpend);
}

public class CrisisStateTests
{
    [Fact]
    public void Empty_HasNoCrises()
    {
        var state = CrisisState.Empty;

        Assert.Equal(0, state.ActiveCount);
    }

    [Fact]
    public void WithCrisisAdded_IncreasesCrisisCount()
    {
        var state = CrisisState.Empty;
        var crisis = CreateTestCrisis();

        var updated = state.WithCrisisAdded(crisis);

        Assert.Equal(1, updated.ActiveCount);
    }

    [Fact]
    public void ActiveCrises_OnlyReturnsActive()
    {
        var active = CreateTestCrisis("c1", CrisisStatus.Active);
        var mitigated = CreateTestCrisis("c2", CrisisStatus.Mitigated);
        var state = CrisisState.Empty
            .WithCrisisAdded(active)
            .WithCrisisAdded(mitigated);

        Assert.Single(state.ActiveCrises);
        Assert.Equal("c1", state.ActiveCrises[0].InstanceId);
    }

    [Fact]
    public void ProcessDeadlines_ExpiresOverdueCrises()
    {
        var overdue = CreateTestCrisis("c1", deadline: 2);
        var onTime = CreateTestCrisis("c2", deadline: 5);
        var state = CrisisState.Empty
            .WithCrisisAdded(overdue)
            .WithCrisisAdded(onTime);

        var (newState, expired) = state.ProcessDeadlines(3);

        Assert.Single(expired);
        Assert.Equal("c1", expired[0].InstanceId);
        Assert.Equal(CrisisStatus.Expired, newState.GetCrisis("c1")!.Status);
        Assert.Equal(CrisisStatus.Active, newState.GetCrisis("c2")!.Status);
    }

    [Fact]
    public void GetTotalOngoingImpact_SumsAllActiveCrises()
    {
        var c1 = new CrisisInstance(
            "crisis_1", "c1", "Crisis 1", "Desc", 3,
            Array.Empty<string>(), 1, 5,
            new Dictionary<Meter, int>(),
            new Dictionary<Meter, int> { { Meter.Delivery, -1 } },
            CrisisStatus.Active, "e1");

        var c2 = new CrisisInstance(
            "crisis_2", "c2", "Crisis 2", "Desc", 3,
            Array.Empty<string>(), 1, 5,
            new Dictionary<Meter, int>(),
            new Dictionary<Meter, int> { { Meter.Delivery, -2 }, { Meter.Morale, -1 } },
            CrisisStatus.Active, "e2");

        var state = CrisisState.Empty
            .WithCrisisAdded(c1)
            .WithCrisisAdded(c2);

        var impact = state.GetTotalOngoingImpact();

        Assert.Equal(-3, impact[Meter.Delivery]);
        Assert.Equal(-1, impact[Meter.Morale]);
    }

    private static CrisisInstance CreateTestCrisis(
        string instanceId = "c1",
        CrisisStatus status = CrisisStatus.Active,
        int deadline = 5) => new(
            CrisisId: "test_crisis",
            InstanceId: instanceId,
            Title: "Test Crisis",
            Description: "A test crisis",
            Severity: 3,
            Tags: new[] { "test" },
            CreatedTurn: 1,
            DeadlineTurn: deadline,
            BaseImpact: new Dictionary<Meter, int> { { Meter.Morale, -5 } },
            OngoingImpact: new Dictionary<Meter, int>(),
            Status: status,
            OriginEventId: "event_1");
}

public class StaffQualityWeightsTests
{
    [Fact]
    public void BudgetWeights_HighIneptChance()
    {
        var weights = StaffQualityWeights.Budget;

        Assert.True(weights.IneptWeight > weights.GoodWeight);
    }

    [Fact]
    public void PremiumWeights_HighGoodChance()
    {
        var weights = StaffQualityWeights.Premium;

        Assert.True(weights.GoodWeight > weights.IneptWeight);
    }

    [Fact]
    public void DetermineQuality_Deterministic()
    {
        var weights = StaffQualityWeights.Standard;

        var result1 = weights.DetermineQuality(42);
        var result2 = weights.DetermineQuality(42);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void DetermineQuality_LowRollIsInept()
    {
        var weights = new StaffQualityWeights(50, 30, 20);

        var result = weights.DetermineQuality(25); // 25 < 50

        Assert.Equal(StaffQuality.Inept, result);
    }

    [Fact]
    public void DetermineQuality_HighRollIsGood()
    {
        var weights = new StaffQualityWeights(20, 30, 50);

        var result = weights.DetermineQuality(75); // 75 >= 20+30=50

        Assert.Equal(StaffQuality.Good, result);
    }
}

public class CrisisResolverTests
{
    [Fact]
    public void Resolve_DeterminesDeterministicOutcome()
    {
        var crisis = CreateTestCrisis();
        var response = CreateTestResponse();
        var resources = new ResourceState(10);
        var org = OrgState.Default;
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        var result1 = CrisisResolver.Resolve(crisis, response, resources, org, rng1);
        var result2 = CrisisResolver.Resolve(crisis, response, resources, org, rng2);

        Assert.Equal(result1.Outcome, result2.Outcome);
        Assert.Equal(result1.AssignedStaff, result2.AssignedStaff);
        Assert.Equal(result1.RollValue, result2.RollValue);
    }

    [Fact]
    public void Resolve_AppliesMitigationBonus()
    {
        var crisis = CreateTestCrisis(severity: 2);
        var highBonus = CreateTestResponse(mitigationBonus: 5);
        var lowBonus = CreateTestResponse(mitigationBonus: -2);
        var resources = new ResourceState(10);
        var org = OrgState.Default;

        // With same RNG seed, higher bonus should have better or equal outcome
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        var highResult = CrisisResolver.Resolve(crisis, highBonus, resources, org, rng1);
        var lowResult = CrisisResolver.Resolve(crisis, lowBonus, resources, org, rng2);

        Assert.True(highResult.ModifiedRoll >= lowResult.ModifiedRoll);
    }

    [Fact]
    public void Resolve_CheapResponseCannotFullyMitigateWhenMinSpendRequired()
    {
        var crisis = CreateTestCrisis(minSpend: 5);
        var cheapResponse = CreateTestResponse(cost: 1, mitigationBonus: 10);
        var resources = new ResourceState(10);
        var org = OrgState.Default;

        // Even with high roll, should downgrade to Mixed
        var rng = new SeededRng(999); // Try different seeds

        var result = CrisisResolver.Resolve(crisis, cheapResponse, resources, org, rng);

        // If it would have been Good, it should be downgraded
        if (result.RollValue + cheapResponse.MitigationBonus >= CrisisResolver.SuccessThreshold)
        {
            Assert.NotEqual(OutcomeTier.Good, result.Outcome);
        }
    }

    [Fact]
    public void Resolve_IneptStaffOnFailSpawnsEffect()
    {
        var crisis = CreateTestCrisis();
        var response = CreateTestResponse();
        response = response with { StaffQuality = new StaffQualityWeights(100, 0, 0) }; // Always inept
        var resources = new ResourceState(10);
        var org = OrgState.Default;

        // Use a seed that produces a fail outcome
        var rng = new SeededRng(1);

        var result = CrisisResolver.Resolve(crisis, response, resources, org, rng);

        // If outcome is not Good and staff is Inept, should spawn inept PM effect
        if (result.Outcome != OutcomeTier.Good && result.AssignedStaff == StaffQuality.Inept)
        {
            Assert.Contains("inept_project_manager", result.SpawnedEffects);
        }
    }

    [Fact]
    public void CalculateSuccessChance_HigherBonusIncreasesChance()
    {
        var crisis = CreateTestCrisis(severity: 3);
        var org = OrgState.Default;

        var lowBonus = CreateTestResponse(mitigationBonus: 0);
        var highBonus = CreateTestResponse(mitigationBonus: 3);

        var lowChance = CrisisResolver.CalculateSuccessChance(crisis, lowBonus, org);
        var highChance = CrisisResolver.CalculateSuccessChance(crisis, highBonus, org);

        Assert.True(highChance > lowChance);
    }

    private static CrisisInstance CreateTestCrisis(
        int severity = 3,
        int? minSpend = null) => new(
            CrisisId: "test_crisis",
            InstanceId: "inst_1",
            Title: "Test Crisis",
            Description: "A test crisis",
            Severity: severity,
            Tags: new[] { "test" },
            CreatedTurn: 1,
            DeadlineTurn: 5,
            BaseImpact: new Dictionary<Meter, int> { { Meter.Morale, -5 } },
            OngoingImpact: new Dictionary<Meter, int>(),
            Status: CrisisStatus.Active,
            OriginEventId: "event_1",
            MinimumSpendToFullyMitigate: minSpend);

    private static CrisisResponse CreateTestResponse(
        int cost = 2,
        int mitigationBonus = 1) => new(
            ResponseId: "test_response",
            Title: "Test Response",
            Description: "A test response",
            CostPC: cost,
            MitigationBonus: mitigationBonus,
            StaffQuality: StaffQualityWeights.Standard,
            Outcomes: new ResponseOutcomes(
                Success: new ResponseOutcome(CrisisOperation.Mitigate),
                Mixed: new ResponseOutcome(CrisisOperation.ReduceSeverity, SeverityReduction: 1),
                Fail: new ResponseOutcome(CrisisOperation.Escalate)));
}

public class CrisisContentTests
{
    [Fact]
    public void AllCrises_HasContent()
    {
        Assert.True(CrisisContent.AllCrises.Count > 0);
    }

    [Fact]
    public void AllResponses_HasContent()
    {
        Assert.True(CrisisContent.AllResponses.Count > 0);
    }

    [Fact]
    public void GetCrisis_FindsByID()
    {
        var crisis = CrisisContent.GetCrisis("email_thread_meltdown");

        Assert.NotNull(crisis);
        Assert.Equal("Email Thread Meltdown", crisis.Title);
    }

    [Fact]
    public void GetResponse_FindsByID()
    {
        var response = CrisisContent.GetResponse("tiger_team");

        Assert.NotNull(response);
        Assert.Equal("Spin Up a Tiger Team", response.Title);
    }

    [Fact]
    public void GetAffordableResponses_FiltersCorrectly()
    {
        var affordable = CrisisContent.GetAffordableResponses(2);

        Assert.All(affordable, r => Assert.True(r.CostPC <= 2));
    }

    [Fact]
    public void AllResponses_HaveValidCosts()
    {
        foreach (var response in CrisisContent.AllResponses)
        {
            Assert.True(response.CostPC >= 1, $"Response {response.ResponseId} has invalid cost");
            Assert.True(response.CostPC <= ResourceState.MaxPoliticalCapital,
                $"Response {response.ResponseId} cost exceeds max PC");
        }
    }

    [Fact]
    public void AllCrises_HaveValidSeverity()
    {
        foreach (var crisis in CrisisContent.AllCrises)
        {
            Assert.True(crisis.Severity >= 1 && crisis.Severity <= 5,
                $"Crisis {crisis.CrisisId} has invalid severity");
        }
    }
}

public class QuarterGameStateResourceTests
{
    [Fact]
    public void NewGame_HasInitialResources()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);

        Assert.NotNull(state.Resources);
        Assert.Equal(ResourceState.Initial.PoliticalCapital, state.Resources.PoliticalCapital);
    }

    [Fact]
    public void NewGame_HasEmptyCrises()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);

        Assert.NotNull(state.Crises);
        Assert.Equal(0, state.Crises.ActiveCount);
    }

    [Fact]
    public void WithPoliticalCapitalChange_UpdatesResources()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);

        var updated = state.WithPoliticalCapitalChange(5);

        Assert.Equal(state.Resources.PoliticalCapital + 5, updated.Resources.PoliticalCapital);
    }

    [Fact]
    public void WithCrises_UpdatesCrises()
    {
        var deckSet = CreateTestDeckSet();
        var state = QuarterGameState.NewGame(42, deckSet);
        var crisis = new CrisisInstance(
            "test", "inst_1", "Test", "Desc", 3,
            Array.Empty<string>(), 1, 3,
            new Dictionary<Meter, int>(), new Dictionary<Meter, int>(),
            CrisisStatus.Active, "e1");

        var updated = state.WithCrises(state.Crises.WithCrisisAdded(crisis));

        Assert.Equal(1, updated.Crises.ActiveCount);
    }

    private static DeckSet CreateTestDeckSet() =>
        new(
            new EventDeck(CrisisEvents.All),
            new EventDeck(BoardEvents.All),
            new EventDeck(ProjectEvents.All));
}
