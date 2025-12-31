namespace InertiCorp.Core.Tests;

public class ChoiceTests
{
    [Fact]
    public void Choice_HasRequiredProperties()
    {
        var choice = new Choice(
            ChoiceId: "CHC_APPROVE",
            Label: "Approve the request",
            Effects: Array.Empty<IEffect>()
        );

        Assert.Equal("CHC_APPROVE", choice.ChoiceId);
        Assert.Equal("Approve the request", choice.Label);
        Assert.Empty(choice.Effects);
    }

    [Fact]
    public void Choice_IsImmutable()
    {
        var choice1 = new Choice("CHC_A", "Choice A", Array.Empty<IEffect>());
        var choice2 = new Choice("CHC_A", "Choice A", Array.Empty<IEffect>());

        // Records with same values are equal
        Assert.Equal(choice1.ChoiceId, choice2.ChoiceId);
    }
}

public class EventCardTests
{
    [Fact]
    public void EventCard_HasRequiredProperties()
    {
        var choices = new List<Choice>
        {
            new("CHC_YES", "Yes", Array.Empty<IEffect>()),
            new("CHC_NO", "No", Array.Empty<IEffect>())
        };

        var card = new EventCard(
            EventId: "EVT_SECURITY_AUDIT",
            Title: "Security Audit",
            Description: "The security team wants to run an audit.",
            Choices: choices
        );

        Assert.Equal("EVT_SECURITY_AUDIT", card.EventId);
        Assert.Equal("Security Audit", card.Title);
        Assert.Equal("The security team wants to run an audit.", card.Description);
        Assert.Equal(2, card.Choices.Count);
    }

    [Fact]
    public void EventCard_With2Choices_IsValid()
    {
        var choices = new List<Choice>
        {
            new("CHC_A", "Option A", Array.Empty<IEffect>()),
            new("CHC_B", "Option B", Array.Empty<IEffect>())
        };

        var card = new EventCard("EVT_TEST", "Test", "Description", choices);

        Assert.Equal(2, card.Choices.Count);
    }

    [Fact]
    public void EventCard_With4Choices_IsValid()
    {
        var choices = new List<Choice>
        {
            new("CHC_A", "Option A", Array.Empty<IEffect>()),
            new("CHC_B", "Option B", Array.Empty<IEffect>()),
            new("CHC_C", "Option C", Array.Empty<IEffect>()),
            new("CHC_D", "Option D", Array.Empty<IEffect>())
        };

        var card = new EventCard("EVT_TEST", "Test", "Description", choices);

        Assert.Equal(4, card.Choices.Count);
    }

    [Fact]
    public void EventCard_WithLessThan2Choices_Throws()
    {
        var choices = new List<Choice>
        {
            new("CHC_ONLY", "Only option", Array.Empty<IEffect>())
        };

        Assert.Throws<ArgumentException>(() =>
            new EventCard("EVT_TEST", "Test", "Description", choices));
    }

    [Fact]
    public void EventCard_WithMoreThan4Choices_Throws()
    {
        var choices = new List<Choice>
        {
            new("CHC_A", "A", Array.Empty<IEffect>()),
            new("CHC_B", "B", Array.Empty<IEffect>()),
            new("CHC_C", "C", Array.Empty<IEffect>()),
            new("CHC_D", "D", Array.Empty<IEffect>()),
            new("CHC_E", "E", Array.Empty<IEffect>())
        };

        Assert.Throws<ArgumentException>(() =>
            new EventCard("EVT_TEST", "Test", "Description", choices));
    }

    [Fact]
    public void EventCard_WithDuplicateChoiceIds_Throws()
    {
        var choices = new List<Choice>
        {
            new("CHC_SAME", "Option A", Array.Empty<IEffect>()),
            new("CHC_SAME", "Option B", Array.Empty<IEffect>())
        };

        Assert.Throws<ArgumentException>(() =>
            new EventCard("EVT_TEST", "Test", "Description", choices));
    }

    [Fact]
    public void EventCard_GetChoiceById_ReturnsCorrectChoice()
    {
        var choices = new List<Choice>
        {
            new("CHC_YES", "Yes", Array.Empty<IEffect>()),
            new("CHC_NO", "No", Array.Empty<IEffect>())
        };

        var card = new EventCard("EVT_TEST", "Test", "Description", choices);

        var choice = card.GetChoice("CHC_NO");

        Assert.Equal("CHC_NO", choice.ChoiceId);
        Assert.Equal("No", choice.Label);
    }

    [Fact]
    public void EventCard_GetChoiceById_InvalidId_Throws()
    {
        var choices = new List<Choice>
        {
            new("CHC_YES", "Yes", Array.Empty<IEffect>()),
            new("CHC_NO", "No", Array.Empty<IEffect>())
        };

        var card = new EventCard("EVT_TEST", "Test", "Description", choices);

        Assert.Throws<ArgumentException>(() => card.GetChoice("CHC_INVALID"));
    }
}
