namespace InertiCorp.Core.Tests;

using InertiCorp.Core.Content;

public class PlaceholderContentTests
{
    [Fact]
    public void PlaceholderEvents_Contains_MeetingOverload()
    {
        var events = PlaceholderEvents.All;

        var meetingOverload = events.FirstOrDefault(e => e.EventId == "EVT_PLACEHOLDER_001_MEETING_OVERLOAD");

        Assert.NotNull(meetingOverload);
        Assert.Equal("Meeting Overload", meetingOverload.Title);
        Assert.InRange(meetingOverload.Choices.Count, 2, 3);
    }

    [Fact]
    public void PlaceholderEvents_Contains_DeadlinePressure()
    {
        var events = PlaceholderEvents.All;

        var deadlinePressure = events.FirstOrDefault(e => e.EventId == "EVT_PLACEHOLDER_002_DEADLINE_PRESSURE");

        Assert.NotNull(deadlinePressure);
        Assert.Equal("Deadline Pressure", deadlinePressure.Title);
        Assert.InRange(deadlinePressure.Choices.Count, 2, 3);
    }

    [Fact]
    public void MeetingOverload_ChoicesAffect_MoraleAndAlignment()
    {
        var events = PlaceholderEvents.All;
        var meetingOverload = events.First(e => e.EventId == "EVT_PLACEHOLDER_001_MEETING_OVERLOAD");

        // At least one choice should affect Morale
        var affectsMorale = meetingOverload.Choices.Any(c =>
            c.Effects.OfType<MeterEffect>().Any(e => e.Meter == Meter.Morale));

        // At least one choice should affect Alignment
        var affectsAlignment = meetingOverload.Choices.Any(c =>
            c.Effects.OfType<MeterEffect>().Any(e => e.Meter == Meter.Alignment));

        Assert.True(affectsMorale, "Meeting Overload should have choices affecting Morale");
        Assert.True(affectsAlignment, "Meeting Overload should have choices affecting Alignment");
    }

    [Fact]
    public void DeadlinePressure_ChoicesAffect_DeliveryAndRunway()
    {
        var events = PlaceholderEvents.All;
        var deadlinePressure = events.First(e => e.EventId == "EVT_PLACEHOLDER_002_DEADLINE_PRESSURE");

        // At least one choice should affect Delivery
        var affectsDelivery = deadlinePressure.Choices.Any(c =>
            c.Effects.OfType<MeterEffect>().Any(e => e.Meter == Meter.Delivery));

        // At least one choice should affect Runway
        var affectsRunway = deadlinePressure.Choices.Any(c =>
            c.Effects.OfType<MeterEffect>().Any(e => e.Meter == Meter.Runway));

        Assert.True(affectsDelivery, "Deadline Pressure should have choices affecting Delivery");
        Assert.True(affectsRunway, "Deadline Pressure should have choices affecting Runway");
    }

    [Fact]
    public void PlaceholderObjectives_Contains_KeepMorale()
    {
        var objectives = PlaceholderObjectives.All;

        var keepMorale = objectives.FirstOrDefault(o => o.ObjectiveId == "OBJ_PLACEHOLDER_001_KEEP_MORALE");

        Assert.NotNull(keepMorale);
        Assert.Contains("Morale", keepMorale.Description);
    }

    [Fact]
    public void PlaceholderObjectives_Contains_HitDelivery()
    {
        var objectives = PlaceholderObjectives.All;

        var hitDelivery = objectives.FirstOrDefault(o => o.ObjectiveId == "OBJ_PLACEHOLDER_002_HIT_DELIVERY");

        Assert.NotNull(hitDelivery);
        Assert.Contains("Delivery", hitDelivery.Description);
    }

    [Fact]
    public void KeepMorale_Evaluates_MoraleGreaterOrEqual40()
    {
        var objectives = PlaceholderObjectives.All;
        var keepMorale = objectives.First(o => o.ObjectiveId == "OBJ_PLACEHOLDER_001_KEEP_MORALE");

        var passingState = new OrgState(50, 40, 50, 50, 50);
        var failingState = new OrgState(50, 39, 50, 50, 50);

        Assert.True(keepMorale.IsMet(passingState));
        Assert.False(keepMorale.IsMet(failingState));
    }

    [Fact]
    public void HitDelivery_Evaluates_DeliveryGreaterOrEqual60()
    {
        var objectives = PlaceholderObjectives.All;
        var hitDelivery = objectives.First(o => o.ObjectiveId == "OBJ_PLACEHOLDER_002_HIT_DELIVERY");

        var passingState = new OrgState(60, 50, 50, 50, 50);
        var failingState = new OrgState(59, 50, 50, 50, 50);

        Assert.True(hitDelivery.IsMet(passingState));
        Assert.False(hitDelivery.IsMet(failingState));
    }

    [Fact]
    public void CanRunGameLoop_WithPlaceholderContent()
    {
        // Create deck from placeholder events
        var deck = new EventDeck(PlaceholderEvents.All);
        var state = GameState.NewGame(seed: 42, deck);
        var rng = new SeededRng(42);

        // Run a turn - peek at the card and make a valid choice
        var card = deck.Peek();
        var input = new TurnInput(card.Choices[0].ChoiceId);

        var (newState, log) = GameEngine.AdvanceTurn(state, input, rng);

        Assert.Equal(2, newState.Turn.TurnNumber);
        Assert.NotNull(log.DrawnEventId);
        Assert.NotNull(log.ChosenChoiceId);
    }

    [Fact]
    public void PlaceholderEvents_AllHaveUniqueIds()
    {
        var events = PlaceholderEvents.All;
        var ids = events.Select(e => e.EventId).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void PlaceholderObjectives_AllHaveUniqueIds()
    {
        var objectives = PlaceholderObjectives.All;
        var ids = objectives.Select(o => o.ObjectiveId).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
