namespace InertiCorp.Core.Crisis;

/// <summary>
/// Immutable collection of active and resolved crises.
/// </summary>
public sealed record CrisisState(IReadOnlyList<CrisisInstance> Crises)
{

    /// <summary>
    /// Empty crisis state with no active crises.
    /// </summary>
    public static CrisisState Empty => new(Array.Empty<CrisisInstance>());

    /// <summary>
    /// All currently active crises.
    /// </summary>
    public IReadOnlyList<CrisisInstance> ActiveCrises =>
        Crises.Where(c => c.IsActive).ToList();

    /// <summary>
    /// Number of active crises.
    /// </summary>
    public int ActiveCount => Crises.Count(c => c.IsActive);

    /// <summary>
    /// Gets a crisis by instance ID.
    /// </summary>
    public CrisisInstance? GetCrisis(string instanceId) =>
        Crises.FirstOrDefault(c => c.InstanceId == instanceId);

    /// <summary>
    /// Returns a new state with a crisis added.
    /// </summary>
    public CrisisState WithCrisisAdded(CrisisInstance crisis) =>
        new(Crises.Append(crisis).ToList());

    /// <summary>
    /// Returns a new state with a crisis updated.
    /// </summary>
    public CrisisState WithCrisisUpdated(CrisisInstance updated) =>
        new(Crises.Select(c => c.InstanceId == updated.InstanceId ? updated : c).ToList());

    /// <summary>
    /// Returns a new state with a crisis removed (for cleanup).
    /// </summary>
    public CrisisState WithCrisisRemoved(string instanceId) =>
        new(Crises.Where(c => c.InstanceId != instanceId).ToList());

    /// <summary>
    /// Generates a unique instance ID for a new crisis.
    /// </summary>
    public string GenerateInstanceId(int seed, int turn) =>
        $"crisis_{turn}_{Math.Abs(HashCode.Combine(seed, turn, Crises.Count))}";

    /// <summary>
    /// Creates a crisis from a definition and adds it to the state.
    /// </summary>
    public (CrisisState NewState, CrisisInstance Created) CreateCrisis(
        CrisisDefinition definition,
        int currentTurn,
        string originEventId,
        int seed)
    {
        var instanceId = GenerateInstanceId(seed, currentTurn);
        var instance = definition.CreateInstance(instanceId, currentTurn, originEventId);
        return (WithCrisisAdded(instance), instance);
    }

    /// <summary>
    /// Processes deadline expirations for all active crises.
    /// Returns updated state and list of expired crises with their impacts.
    /// </summary>
    public (CrisisState NewState, IReadOnlyList<CrisisInstance> Expired) ProcessDeadlines(int currentTurn)
    {
        var expired = new List<CrisisInstance>();
        var updated = new List<CrisisInstance>();

        foreach (var crisis in Crises)
        {
            if (crisis.IsActive && crisis.IsOverdue(currentTurn))
            {
                var expiredCrisis = crisis.WithExpired();
                updated.Add(expiredCrisis);
                expired.Add(expiredCrisis);
            }
            else
            {
                updated.Add(crisis);
            }
        }

        return (new CrisisState(updated), expired);
    }

    /// <summary>
    /// Calculates total ongoing impact from all active crises.
    /// </summary>
    public IReadOnlyDictionary<Meter, int> GetTotalOngoingImpact()
    {
        var totals = new Dictionary<Meter, int>();

        foreach (var crisis in ActiveCrises)
        {
            foreach (var (meter, delta) in crisis.OngoingImpact)
            {
                totals.TryGetValue(meter, out var current);
                totals[meter] = current + delta;
            }
        }

        return totals;
    }
}
