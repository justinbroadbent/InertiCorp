namespace InertiCorp.Core.Tests;

public class ObjectiveModelTests
{
    #region MeterThresholdCondition Tests

    [Fact]
    public void MeterThresholdCondition_IsMet_WhenMeterEqualsThreshold()
    {
        var condition = new MeterThresholdCondition(Meter.Morale, 60);
        var state = new OrgState(50, 60, 50, 50, 50);

        Assert.True(condition.IsMet(state));
    }

    [Fact]
    public void MeterThresholdCondition_IsMet_WhenMeterExceedsThreshold()
    {
        var condition = new MeterThresholdCondition(Meter.Morale, 60);
        var state = new OrgState(50, 75, 50, 50, 50);

        Assert.True(condition.IsMet(state));
    }

    [Fact]
    public void MeterThresholdCondition_IsNotMet_WhenMeterBelowThreshold()
    {
        var condition = new MeterThresholdCondition(Meter.Morale, 60);
        var state = new OrgState(50, 59, 50, 50, 50);

        Assert.False(condition.IsMet(state));
    }

    [Fact]
    public void MeterThresholdCondition_WorksWithAllMeters()
    {
        var state = new OrgState(Delivery: 80, Morale: 70, Governance: 60, Alignment: 50, Runway: 40);

        Assert.True(new MeterThresholdCondition(Meter.Delivery, 80).IsMet(state));
        Assert.True(new MeterThresholdCondition(Meter.Morale, 70).IsMet(state));
        Assert.True(new MeterThresholdCondition(Meter.Governance, 60).IsMet(state));
        Assert.True(new MeterThresholdCondition(Meter.Alignment, 50).IsMet(state));
        Assert.True(new MeterThresholdCondition(Meter.Runway, 40).IsMet(state));
    }

    [Fact]
    public void MeterThresholdCondition_IsDeterministic()
    {
        var condition = new MeterThresholdCondition(Meter.Delivery, 50);
        var state = new OrgState(50, 50, 50, 50, 50);

        var result1 = condition.IsMet(state);
        var result2 = condition.IsMet(state);

        Assert.Equal(result1, result2);
    }

    #endregion

    #region CompositeCondition Tests

    [Fact]
    public void CompositeCondition_And_AllTrue_ReturnsTrue()
    {
        var condition = CompositeCondition.And(
            new MeterThresholdCondition(Meter.Morale, 50),
            new MeterThresholdCondition(Meter.Delivery, 50)
        );
        var state = new OrgState(60, 60, 50, 50, 50);

        Assert.True(condition.IsMet(state));
    }

    [Fact]
    public void CompositeCondition_And_OneFalse_ReturnsFalse()
    {
        var condition = CompositeCondition.And(
            new MeterThresholdCondition(Meter.Morale, 50),
            new MeterThresholdCondition(Meter.Delivery, 70)
        );
        var state = new OrgState(60, 60, 50, 50, 50); // Delivery is 60, below 70

        Assert.False(condition.IsMet(state));
    }

    [Fact]
    public void CompositeCondition_Or_AllTrue_ReturnsTrue()
    {
        var condition = CompositeCondition.Or(
            new MeterThresholdCondition(Meter.Morale, 50),
            new MeterThresholdCondition(Meter.Delivery, 50)
        );
        var state = new OrgState(60, 60, 50, 50, 50);

        Assert.True(condition.IsMet(state));
    }

    [Fact]
    public void CompositeCondition_Or_OneTrue_ReturnsTrue()
    {
        var condition = CompositeCondition.Or(
            new MeterThresholdCondition(Meter.Morale, 50),
            new MeterThresholdCondition(Meter.Delivery, 70)
        );
        var state = new OrgState(60, 60, 50, 50, 50); // Morale is 60 >= 50

        Assert.True(condition.IsMet(state));
    }

    [Fact]
    public void CompositeCondition_Or_AllFalse_ReturnsFalse()
    {
        var condition = CompositeCondition.Or(
            new MeterThresholdCondition(Meter.Morale, 70),
            new MeterThresholdCondition(Meter.Delivery, 70)
        );
        var state = new OrgState(60, 60, 50, 50, 50);

        Assert.False(condition.IsMet(state));
    }

    [Fact]
    public void CompositeCondition_CanBeNested()
    {
        // (Morale >= 50 AND Delivery >= 50) OR Runway >= 80
        var condition = CompositeCondition.Or(
            CompositeCondition.And(
                new MeterThresholdCondition(Meter.Morale, 50),
                new MeterThresholdCondition(Meter.Delivery, 50)
            ),
            new MeterThresholdCondition(Meter.Runway, 80)
        );

        var stateWithBothMeters = new OrgState(60, 60, 50, 50, 50);
        var stateWithHighRunway = new OrgState(40, 40, 50, 50, 80);
        var stateWithNeither = new OrgState(40, 40, 50, 50, 50);

        Assert.True(condition.IsMet(stateWithBothMeters));
        Assert.True(condition.IsMet(stateWithHighRunway));
        Assert.False(condition.IsMet(stateWithNeither));
    }

    #endregion

    #region Objective Tests

    [Fact]
    public void Objective_HasRequiredProperties()
    {
        var condition = new MeterThresholdCondition(Meter.Morale, 60);
        var objective = new Objective(
            "OBJ_TEST",
            "Test Title",
            "Test Description",
            condition
        );

        Assert.Equal("OBJ_TEST", objective.ObjectiveId);
        Assert.Equal("Test Title", objective.Title);
        Assert.Equal("Test Description", objective.Description);
        Assert.Same(condition, objective.Condition);
    }

    [Fact]
    public void Objective_IsMet_DelegatesToCondition()
    {
        var condition = new MeterThresholdCondition(Meter.Morale, 60);
        var objective = new Objective(
            "OBJ_MORALE_CHECK",
            "Morale Check",
            "Keep morale at 60 or above",
            condition
        );

        var passingState = new OrgState(50, 60, 50, 50, 50);
        var failingState = new OrgState(50, 59, 50, 50, 50);

        Assert.True(objective.IsMet(passingState));
        Assert.False(objective.IsMet(failingState));
    }

    [Fact]
    public void Objective_Evaluation_IsDeterministic()
    {
        var objective = new Objective(
            "OBJ_TEST",
            "Test",
            "Test objective",
            new MeterThresholdCondition(Meter.Delivery, 50)
        );
        var state = new OrgState(50, 50, 50, 50, 50);

        var result1 = objective.IsMet(state);
        var result2 = objective.IsMet(state);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Objective_Evaluation_IsSideEffectFree()
    {
        var objective = new Objective(
            "OBJ_TEST",
            "Test",
            "Test objective",
            new MeterThresholdCondition(Meter.Delivery, 50)
        );
        var state = new OrgState(60, 50, 50, 50, 50);

        objective.IsMet(state);

        // State should be unchanged
        Assert.Equal(60, state.Delivery);
        Assert.Equal(50, state.Morale);
    }

    #endregion
}
