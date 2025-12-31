using InertiCorp.Core.Quarter;
using InertiCorp.Core.Content;

namespace InertiCorp.Core.Tests;

public class QuarterLoopStateTests
{
    [Fact]
    public void Initial_StartsAtQuarter1_ProjectsPhase()
    {
        var state = QuarterLoopState.Initial;

        Assert.Equal(1, state.QuarterNumber);
        Assert.Equal(QuarterPhase.Projects, state.Phase);
        Assert.Empty(state.SelectedProjectIds);
        Assert.False(state.ReorgUsedThisQuarter);
        Assert.Equal(0, state.QuarterProfit);
        Assert.Null(state.ActiveCrisis);
        Assert.Null(state.BoardReview);
        Assert.Equal(0, state.ConsecutivePoorQuarters);
    }

    [Fact]
    public void WithProjectSelected_AddsToSelection()
    {
        var state = QuarterLoopState.Initial;

        var updated = state.WithProjectSelected("project_1");

        Assert.Single(updated.SelectedProjectIds);
        Assert.Contains("project_1", updated.SelectedProjectIds);
    }

    [Fact]
    public void WithProjectSelected_AccumulatesMultipleSelections()
    {
        var state = QuarterLoopState.Initial
            .WithProjectSelected("project_1")
            .WithProjectSelected("project_2")
            .WithProjectSelected("project_3");

        Assert.Equal(3, state.SelectedProjectIds.Count);
        Assert.Contains("project_1", state.SelectedProjectIds);
        Assert.Contains("project_2", state.SelectedProjectIds);
        Assert.Contains("project_3", state.SelectedProjectIds);
    }

    [Fact]
    public void ProjectsComplete_FalseWhenLessThanThreeSelected()
    {
        var state = QuarterLoopState.Initial
            .WithProjectSelected("project_1")
            .WithProjectSelected("project_2");

        Assert.False(state.ProjectsComplete);
    }

    [Fact]
    public void ProjectsComplete_TrueWhenThreeSelected()
    {
        var state = QuarterLoopState.Initial
            .WithProjectSelected("project_1")
            .WithProjectSelected("project_2")
            .WithProjectSelected("project_3");

        Assert.True(state.ProjectsComplete);
    }

    [Fact]
    public void ProjectsComplete_TrueWithOneWhenReorgUsed()
    {
        var state = QuarterLoopState.Initial
            .WithReorgUsed()
            .WithProjectSelected("project_1");

        Assert.True(state.ProjectsComplete);
    }

    [Fact]
    public void ProjectsComplete_FalseWithZeroWhenReorgUsed()
    {
        var state = QuarterLoopState.Initial
            .WithReorgUsed();

        Assert.False(state.ProjectsComplete);
    }

    [Fact]
    public void WithReorgUsed_SetsFlag()
    {
        var state = QuarterLoopState.Initial;

        var updated = state.WithReorgUsed();

        Assert.True(updated.ReorgUsedThisQuarter);
    }

    [Fact]
    public void WithProfitAdded_AccumulatesProfit()
    {
        var state = QuarterLoopState.Initial
            .WithProfitAdded(5)
            .WithProfitAdded(3);

        Assert.Equal(8, state.QuarterProfit);
    }

    [Fact]
    public void NextPhase_FromProjects_GoesToSituation()
    {
        var state = QuarterLoopState.Initial;

        var next = state.NextPhase();

        Assert.Equal(QuarterPhase.Situation, next.Phase);
        Assert.Equal(1, next.QuarterNumber);
    }

    [Fact]
    public void NextPhase_FromSituation_GoesToBoardMeeting()
    {
        var state = QuarterLoopState.Initial
            .NextPhase(); // Situation

        var next = state.NextPhase();

        Assert.Equal(QuarterPhase.BoardMeeting, next.Phase);
        Assert.Equal(1, next.QuarterNumber);
    }

    [Fact]
    public void NextPhase_FromBoardMeeting_StartsNextQuarter()
    {
        var state = QuarterLoopState.Initial
            .NextPhase() // Situation
            .NextPhase(); // BoardMeeting

        var next = state.NextPhase();

        Assert.Equal(QuarterPhase.Projects, next.Phase);
        Assert.Equal(2, next.QuarterNumber);
        Assert.Empty(next.SelectedProjectIds);
        Assert.False(next.ReorgUsedThisQuarter);
        Assert.Equal(0, next.QuarterProfit);
    }

    [Fact]
    public void IsProjectsPhase_TrueOnlyInProjects()
    {
        Assert.True(QuarterLoopState.Initial.IsProjectsPhase);
        Assert.False(QuarterLoopState.Initial.NextPhase().IsProjectsPhase);
    }

    [Fact]
    public void IsBoardMeetingPhase_TrueOnlyInBoardMeeting()
    {
        var state = QuarterLoopState.Initial
            .NextPhase() // Situation
            .NextPhase(); // BoardMeeting

        Assert.True(state.IsBoardMeetingPhase);
    }

    [Fact]
    public void IsSituationPhase_TrueOnlyInSituation()
    {
        var state = QuarterLoopState.Initial
            .NextPhase(); // Situation

        Assert.False(QuarterLoopState.Initial.IsSituationPhase);
        Assert.True(state.IsSituationPhase);
        Assert.False(state.NextPhase().IsSituationPhase); // BoardMeeting
    }
}

public class BoardReviewCalculatorTests
{
    private static OrgState MakeOrg(
        int delivery = 50, int morale = 50,
        int governance = 50, int alignment = 50, int runway = 50) =>
        new(delivery, morale, governance, alignment, runway);

    private static CEOState MakeCEO(int favorability = 50, int pressure = 1) =>
        CEOState.Initial with { BoardFavorability = favorability, BoardPressureLevel = pressure };

    [Fact]
    public void ScoreToRating_HighScore_ReturnsA()
    {
        Assert.Equal(BoardRating.A, BoardReviewCalculator.ScoreToRating(80));
        Assert.Equal(BoardRating.A, BoardReviewCalculator.ScoreToRating(100));
    }

    [Fact]
    public void ScoreToRating_MidHighScore_ReturnsB()
    {
        Assert.Equal(BoardRating.B, BoardReviewCalculator.ScoreToRating(60));
        Assert.Equal(BoardRating.B, BoardReviewCalculator.ScoreToRating(79));
    }

    [Fact]
    public void ScoreToRating_MidScore_ReturnsC()
    {
        Assert.Equal(BoardRating.C, BoardReviewCalculator.ScoreToRating(40));
        Assert.Equal(BoardRating.C, BoardReviewCalculator.ScoreToRating(59));
    }

    [Fact]
    public void ScoreToRating_MidLowScore_ReturnsD()
    {
        Assert.Equal(BoardRating.D, BoardReviewCalculator.ScoreToRating(20));
        Assert.Equal(BoardRating.D, BoardReviewCalculator.ScoreToRating(39));
    }

    [Fact]
    public void ScoreToRating_LowScore_ReturnsF()
    {
        Assert.Equal(BoardRating.F, BoardReviewCalculator.ScoreToRating(0));
        Assert.Equal(BoardRating.F, BoardReviewCalculator.ScoreToRating(19));
    }

    [Fact]
    public void Calculate_NeutralState_ScoresInBRange()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Base 50 + meter contributions (alignment 50 gives +5, runway 50 gives +4) = ~59 -> B rating
        Assert.Equal(BoardRating.B, result.Rating);
        Assert.Equal(EmploymentDecision.Retain, result.Decision);
    }

    [Fact]
    public void Calculate_HighProfit_IncreasesScore()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 20,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Base 50 + 30 (high profit) = 80 -> A rating
        Assert.Equal(BoardRating.A, result.Rating);
    }

    [Fact]
    public void Calculate_NegativeProfit_DecreasesScore()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: -15,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Base 50 - 30 (negative profit) = 20 -> D rating
        Assert.Equal(BoardRating.D, result.Rating);
    }

    [Fact]
    public void Calculate_HighAlignment_IncreasesScore()
    {
        var org = MakeOrg(alignment: 80);
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Alignment >=70 gives +10 (weighted more than other meters)
        Assert.True(result.RawScore > 50);
    }

    [Fact]
    public void Calculate_HighFavorability_GivesBonus()
    {
        var org = MakeOrg();
        var ceo = MakeCEO(favorability: 75);
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // High favorability adds +5
        Assert.True(result.RawScore >= 55);
    }

    [Fact]
    public void Calculate_LowFavorability_GivesPenalty()
    {
        var org = MakeOrg();
        var ceo = MakeCEO(favorability: 25);
        var rng = new SeededRng(123);

        var withLowFav = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        var withNormalFav = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: MakeCEO(favorability: 50),
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Low favorability should give lower score than normal
        Assert.True(withLowFav.RawScore < withNormalFav.RawScore);
    }

    [Fact]
    public void Calculate_WithInfluence_AppliesScoreBonus()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);
        var influence = new BoardInfluenceResult(
            PackageId: "test",
            PCSpent: 2,
            ScoreBonusApplied: 15,
            BacklashTriggered: false,
            ScheduledConsequenceId: null);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 1,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: influence,
            consecutivePoorQuarters: 0,
            rng: rng);

        // Modified score should be higher than raw
        Assert.True(result.ModifiedScore > result.RawScore);
        Assert.Equal(15, result.ModifiedScore - result.RawScore);
    }

    [Fact]
    public void Calculate_TooManyPoorQuarters_ForcesTermination()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 4,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 3, // Max is 3
            rng: rng);

        Assert.Equal(EmploymentDecision.Terminate, result.Decision);
    }

    [Fact]
    public void IsPoorRating_TrueForDAndF()
    {
        Assert.False(BoardReviewCalculator.IsPoorRating(BoardRating.A));
        Assert.False(BoardReviewCalculator.IsPoorRating(BoardRating.B));
        Assert.False(BoardReviewCalculator.IsPoorRating(BoardRating.C));
        Assert.True(BoardReviewCalculator.IsPoorRating(BoardRating.D));
        Assert.True(BoardReviewCalculator.IsPoorRating(BoardRating.F));
    }

    [Fact]
    public void Calculate_Termination_IncludesGoldenParachute()
    {
        var org = MakeOrg();
        var ceo = MakeCEO();
        var rng = new SeededRng(123);

        var result = BoardReviewCalculator.Calculate(
            quarterNumber: 4,
            quarterProfit: 0,
            org: org,
            ceo: ceo,
            resources: ResourceState.Initial,
            influence: null,
            consecutivePoorQuarters: 3,
            rng: rng);

        Assert.Equal(EmploymentDecision.Terminate, result.Decision);
        Assert.NotNull(result.Parachute);
    }
}

public class BoardInfluenceContentTests
{
    [Fact]
    public void AllPackages_HasFivePackages()
    {
        Assert.Equal(5, BoardInfluenceContent.AllPackages.Count);
    }

    [Fact]
    public void AllPackages_HaveUniqueIds()
    {
        var ids = BoardInfluenceContent.AllPackages.Select(p => p.PackageId).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count);
    }

    [Fact]
    public void GetPackage_ReturnsCorrectPackage()
    {
        var package = BoardInfluenceContent.GetPackage("spin_metrics");

        Assert.NotNull(package);
        Assert.Equal("Spin the Metrics", package.Title);
        Assert.Equal(2, package.CostPC);
    }

    [Fact]
    public void GetPackage_InvalidId_ReturnsNull()
    {
        var package = BoardInfluenceContent.GetPackage("nonexistent");

        Assert.Null(package);
    }

    [Fact]
    public void GetAffordablePackages_FiltersCorrectly()
    {
        var affordable = BoardInfluenceContent.GetAffordablePackages(3);

        // spin_metrics (2 PC) and strategic_leak (3 PC) should be affordable
        Assert.Contains(affordable, p => p.PackageId == "spin_metrics");
        Assert.Contains(affordable, p => p.PackageId == "strategic_leak");
        Assert.DoesNotContain(affordable, p => p.PackageId == "executive_offsite"); // 4 PC
    }

    [Fact]
    public void GetAffordablePackages_ZeroPC_ReturnsEmpty()
    {
        var affordable = BoardInfluenceContent.GetAffordablePackages(0);

        Assert.Empty(affordable);
    }

    [Fact]
    public void AllPackages_HavePositiveCost()
    {
        foreach (var package in BoardInfluenceContent.AllPackages)
        {
            Assert.True(package.CostPC > 0, $"Package {package.PackageId} should have positive cost");
        }
    }

    [Fact]
    public void AllPackages_HavePositiveScoreBonus()
    {
        foreach (var package in BoardInfluenceContent.AllPackages)
        {
            Assert.True(package.ScoreBonus > 0, $"Package {package.PackageId} should have positive bonus");
        }
    }

    [Fact]
    public void ApplyInfluence_ReturnsResult()
    {
        var package = BoardInfluenceContent.GetPackage("spin_metrics")!;

        var result = BoardInfluenceContent.ApplyInfluence(package, seed: 123, quarterNumber: 1);

        Assert.Equal("spin_metrics", result.PackageId);
        Assert.Equal(2, result.PCSpent);
        Assert.Equal(5, result.ScoreBonusApplied);
    }

    [Fact]
    public void ApplyInfluence_DeterministicBacklash()
    {
        var package = BoardInfluenceContent.GetPackage("strategic_leak")!; // 50% backlash risk

        // Same seed should always give same result
        var result1 = BoardInfluenceContent.ApplyInfluence(package, seed: 42, quarterNumber: 1);
        var result2 = BoardInfluenceContent.ApplyInfluence(package, seed: 42, quarterNumber: 1);

        Assert.Equal(result1.BacklashTriggered, result2.BacklashTriggered);
    }

    [Fact]
    public void ApplyInfluence_NoBacklashConsequence_ForConsultantEndorsement()
    {
        var package = BoardInfluenceContent.GetPackage("consultant_endorsement")!;

        var result = BoardInfluenceContent.ApplyInfluence(package, seed: 123, quarterNumber: 1);

        // Consultant endorsement has no backlash consequence (null)
        // Even if backlash "triggers", there's no consequence scheduled
        if (result.BacklashTriggered)
        {
            Assert.Null(result.ScheduledConsequenceId);
        }
    }
}

public class QuarterPhaseTests
{
    [Fact]
    public void QuarterPhase_HasThreePhases()
    {
        var phases = Enum.GetValues<QuarterPhase>();
        Assert.Equal(3, phases.Length);
        Assert.Contains(QuarterPhase.Projects, phases);
        Assert.Contains(QuarterPhase.Situation, phases);
        Assert.Contains(QuarterPhase.BoardMeeting, phases);
    }
}

public class GoldenParachuteTests
{
    [Fact]
    public void Calculate_ActiveCEO_BaseComponents()
    {
        var parachute = GoldenParachute.Calculate(
            quartersSurvived: 4,
            politicalCapital: 5,
            evilScore: 0,
            totalCardsPlayed: 10);

        // Base payout: $10M
        // Tenure bonus: 4 quarters * $3M = $12M
        // PC conversion: 5 PC * $5M = $25M
        // Evil penalty: 0
        // Total: 10 + 12 + 25 = $47M
        Assert.Equal(10, parachute.BasePayout);
        Assert.Equal(12, parachute.TenureBonus);
        Assert.Equal(25, parachute.PCConversion);
        Assert.Equal(0, parachute.EthicsPenalty);
        Assert.Equal(47, parachute.TotalPayout);
    }

    [Fact]
    public void Calculate_EvilReducesParachute()
    {
        var parachute = GoldenParachute.Calculate(
            quartersSurvived: 4,
            politicalCapital: 5,
            evilScore: 10,
            totalCardsPlayed: 10);

        // Base: $10M, Tenure: $12M, PC: $25M, Evil: -$20M
        // Total: 10 + 12 + 25 - 20 = $27M
        Assert.Equal(20, parachute.EthicsPenalty);
        Assert.Equal(27, parachute.TotalPayout);
    }

    [Fact]
    public void Calculate_InactiveCEO_MinimalParachute()
    {
        var parachute = GoldenParachute.Calculate(
            quartersSurvived: 8,
            politicalCapital: 10,
            evilScore: 0,
            totalCardsPlayed: 0); // Never played any cards

        // Inactive CEO gets only base payout regardless of tenure/PC
        Assert.Equal(10, parachute.BasePayout);
        Assert.Equal(0, parachute.TenureBonus);
        Assert.Equal(0, parachute.PCConversion);
        Assert.Equal(10, parachute.TotalPayout);
    }

    [Fact]
    public void Calculate_NeverBelowBase()
    {
        var parachute = GoldenParachute.Calculate(
            quartersSurvived: 1,
            politicalCapital: 0,
            evilScore: 50,
            totalCardsPlayed: 5);

        // Even with high evil and low tenure, minimum is base payout
        Assert.True(parachute.TotalPayout >= 10);
    }
}
