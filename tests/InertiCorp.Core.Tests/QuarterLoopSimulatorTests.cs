using Xunit;
using Xunit.Abstractions;

namespace InertiCorp.Core.Tests;

public class QuarterLoopSimulatorTests
{
    private readonly ITestOutputHelper _output;

    public QuarterLoopSimulatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SingleGame_CompletesWithoutError()
    {
        var simulator = new QuarterLoopSimulator(QuarterLoopSimulator.Strategy.Balanced);
        var result = simulator.PlayGame(seed: 12345);

        Assert.True(result.QuartersSurvived >= 0);
        Assert.NotEmpty(result.DeathReason);
    }

    [Fact]
    public void SingleGame_TracksSituationMetrics()
    {
        var simulator = new QuarterLoopSimulator(QuarterLoopSimulator.Strategy.Balanced);
        var result = simulator.PlayGame(seed: 12345);

        // Situations should be tracked
        Assert.NotNull(result.Situations);
        Assert.NotNull(result.Situations.ResponseTypeUsed);
        Assert.NotNull(result.Situations.SituationsBySeverity);
    }

    [Theory]
    [InlineData(QuarterLoopSimulator.Strategy.Random)]
    [InlineData(QuarterLoopSimulator.Strategy.Conservative)]
    [InlineData(QuarterLoopSimulator.Strategy.Aggressive)]
    [InlineData(QuarterLoopSimulator.Strategy.Balanced)]
    [InlineData(QuarterLoopSimulator.Strategy.DeferHeavy)]
    [InlineData(QuarterLoopSimulator.Strategy.PCHoarder)]
    public void AllStrategies_CompleteGames(QuarterLoopSimulator.Strategy strategy)
    {
        var simulator = new QuarterLoopSimulator(strategy);
        var result = simulator.PlayGame(seed: 42);

        Assert.True(result.QuartersSurvived >= 0);
    }

    [Fact]
    public void RunSimulation_GeneratesValidStats()
    {
        var stats = QuarterLoopSimulator.RunSimulation(
            gameCount: 10,
            strategy: QuarterLoopSimulator.Strategy.Balanced,
            baseSeed: 42);

        Assert.Equal(10, stats.GamesPlayed);
        Assert.True(stats.AvgQuartersSurvived >= 0);
        Assert.True(stats.AvgSituationsPerGame >= 0);
        Assert.NotEmpty(stats.DeathReasons);
    }

    [Fact]
    public void FormatReport_ProducesReadableOutput()
    {
        var stats = QuarterLoopSimulator.RunSimulation(
            gameCount: 10,
            strategy: QuarterLoopSimulator.Strategy.Balanced,
            baseSeed: 42);

        var report = QuarterLoopSimulator.FormatReport(stats);

        Assert.NotEmpty(report);
        Assert.Contains("SURVIVAL METRICS", report);
        Assert.Contains("SITUATION METRICS", report);
    }

    [Fact]
    public void SimulateAllStrategies()
    {
        // Run 1000 games per strategy for statistically meaningful data
        var report = QuarterLoopSimulator.RunAllStrategiesComparison(
            gamesPerStrategy: 1000,
            baseSeed: 42);

        _output.WriteLine(report);
        _output.WriteLine("");

        // Also run detailed reports for each strategy
        foreach (var strategy in Enum.GetValues<QuarterLoopSimulator.Strategy>())
        {
            var stats = QuarterLoopSimulator.RunSimulation(1000, strategy, 42);
            _output.WriteLine(QuarterLoopSimulator.FormatReport(stats));
            _output.WriteLine("");
        }

        Assert.NotEmpty(report);
    }
}
