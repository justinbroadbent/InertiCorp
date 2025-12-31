namespace InertiCorp.Core;

/// <summary>
/// Immutable record tracking resources beyond the core meters.
/// Political Capital is earned through governance/alignment and spent on crisis responses.
/// </summary>
public sealed record ResourceState
{
    /// <summary>
    /// Maximum Political Capital that can be accumulated.
    /// </summary>
    public const int MaxPoliticalCapital = 20;

    /// <summary>
    /// Threshold above which PC decays by 1 per turn.
    /// </summary>
    public const int DecayThreshold = 10;

    /// <summary>
    /// Political Capital - currency for purchasing crisis responses.
    /// </summary>
    public int PoliticalCapital { get; }

    public ResourceState(int politicalCapital)
    {
        PoliticalCapital = Math.Clamp(politicalCapital, 0, MaxPoliticalCapital);
    }

    /// <summary>
    /// Starting resource state with initial Political Capital.
    /// </summary>
    public static ResourceState Initial => new(politicalCapital: 10);

    /// <summary>
    /// Returns a new ResourceState with PC changed by delta.
    /// </summary>
    public ResourceState WithPoliticalCapitalChange(int delta) =>
        new(PoliticalCapital + delta);

    /// <summary>
    /// Applies turn-end earning/decay rules based on org meters.
    /// </summary>
    public ResourceState WithTurnEndAdjustments(OrgState org)
    {
        int delta = 0;

        // Earn PC from high governance/alignment
        if (org.Governance >= 60) delta += 1;
        if (org.Alignment >= 60) delta += 1;

        // Lose PC from low morale (people stop backing you)
        if (org.Morale < 30) delta -= 1;

        // Decay if hoarding too much (political capital spoils)
        if (PoliticalCapital > DecayThreshold) delta -= 1;

        return WithPoliticalCapitalChange(delta);
    }

    /// <summary>
    /// Whether there's enough PC to afford a cost.
    /// </summary>
    public bool CanAfford(int cost) => PoliticalCapital >= cost;

    /// <summary>
    /// Returns a new ResourceState after spending PC. Throws if insufficient.
    /// </summary>
    public ResourceState WithSpend(int cost)
    {
        if (!CanAfford(cost))
            throw new InvalidOperationException($"Insufficient Political Capital: have {PoliticalCapital}, need {cost}");

        return new(PoliticalCapital - cost);
    }

    /// <summary>
    /// Calculates PC earned from restraint (fewer projects = more PC).
    /// 0 cards: +3 PC, 1 card: +2 PC, 2 cards: +1 PC, 3 cards: +0 PC
    /// </summary>
    public static int CalculateRestraintBonus(int cardsPlayed) => cardsPlayed switch
    {
        0 => 3,
        1 => 2,
        2 => 1,
        _ => 0  // 3 or more cards = no bonus
    };

    /// <summary>
    /// Gets the meter exchange rate for trading organizational health for PC.
    /// Returns (meterCost, pcGain) - how much meter to lose for how much PC gained.
    /// </summary>
    public static (int MeterCost, int PCGain) GetMeterExchangeRate(Meter meter) => meter switch
    {
        Meter.Morale => (10, 1),     // -10 Morale = +1 PC
        Meter.Alignment => (10, 1),  // -10 Alignment = +1 PC
        Meter.Delivery => (10, 1),   // -10 Delivery = +1 PC
        Meter.Governance => (15, 1), // -15 Governance = +1 PC (harder to extract PC from)
        Meter.Runway => (20, 1),     // -20 Runway = +1 PC (most expensive)
        _ => throw new ArgumentOutOfRangeException(nameof(meter))
    };

    /// <summary>
    /// Checks whether a meter has enough value to exchange for PC.
    /// </summary>
    public static bool CanExchangeMeterForPC(OrgState org, Meter meter)
    {
        var (meterCost, _) = GetMeterExchangeRate(meter);
        return org.GetMeter(meter) >= meterCost;
    }
}
