namespace InertiCorp.Core;

/// <summary>
/// Difficulty levels with CEO-themed names.
/// Each represents a different board/investor temperament.
/// </summary>
public enum Difficulty
{
    /// <summary>
    /// Easy mode - "The Welch"
    /// Named after Jack Welch, who despite being demanding,
    /// led GE through 20 years of growth with strong shareholder returns.
    /// The board is patient and rewards are generous.
    /// </summary>
    TheWelch = 0,

    /// <summary>
    /// Regular mode - "The Nadella"
    /// Named after Satya Nadella, known for his balanced,
    /// transformational leadership at Microsoft.
    /// The board has reasonable expectations.
    /// </summary>
    TheNadella = 1,

    /// <summary>
    /// Hard mode - "The Icahn"
    /// Named after Carl Icahn, the legendary activist investor
    /// known for aggressively pressuring CEOs for results.
    /// The board is impatient and unforgiving.
    /// </summary>
    TheIcahn = 2
}

/// <summary>
/// Configuration values for each difficulty level.
/// Affects board favorability, retirement requirements, and pressure scaling.
/// </summary>
public sealed class DifficultySettings
{
    /// <summary>
    /// Current active difficulty. Defaults to Regular (TheNadella).
    /// </summary>
    public static Difficulty Current { get; set; } = Difficulty.TheNadella;

    /// <summary>
    /// Gets the settings for the current difficulty.
    /// </summary>
    public static DifficultySettings CurrentSettings => ForDifficulty(Current);

    /// <summary>
    /// Retirement threshold in millions. Must accumulate this much bonus to retire.
    /// </summary>
    public int RetirementThreshold { get; init; }

    /// <summary>
    /// Quarter when tenure decay begins. Earlier = harder.
    /// </summary>
    public int TenureDecayStartQuarter { get; init; }

    /// <summary>
    /// Whether tenure decay is enabled at all.
    /// </summary>
    public bool TenureDecayEnabled { get; init; }

    /// <summary>
    /// Success reward modifier. Applied to base success reward.
    /// </summary>
    public int SuccessRewardBonus { get; init; }

    /// <summary>
    /// Starting board favorability (0-100).
    /// </summary>
    public int StartingFavorability { get; init; }

    /// <summary>
    /// Display name for the difficulty.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Short description of what this difficulty means.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Longer flavor text about the CEO this is named after.
    /// </summary>
    public string FlavorText { get; init; } = "";

    /// <summary>
    /// Gets settings for a specific difficulty level.
    /// </summary>
    public static DifficultySettings ForDifficulty(Difficulty difficulty) => difficulty switch
    {
        Difficulty.TheWelch => new DifficultySettings
        {
            RetirementThreshold = 120,
            TenureDecayStartQuarter = 99, // Effectively disabled
            TenureDecayEnabled = false,
            SuccessRewardBonus = 1,       // +1 to success rewards
            StartingFavorability = 80,
            Name = "The Welch",
            Description = "Easy - A patient board with generous rewards",
            FlavorText = "Jack Welch led GE for 20 years, growing it into the world's most valuable company. Your board remembers the good old days of steady growth and patient capital."
        },
        Difficulty.TheNadella => new DifficultySettings
        {
            RetirementThreshold = 140,
            TenureDecayStartQuarter = 16, // Year 4
            TenureDecayEnabled = true,
            SuccessRewardBonus = 0,
            StartingFavorability = 75,
            Name = "The Nadella",
            Description = "Regular - Balanced expectations and fair evaluation",
            FlavorText = "Satya Nadella transformed Microsoft through empathy and strategic vision. Your board values sustainable growth and long-term thinking."
        },
        Difficulty.TheIcahn => new DifficultySettings
        {
            RetirementThreshold = 180,
            TenureDecayStartQuarter = 6,  // Year 1.5
            TenureDecayEnabled = true,
            SuccessRewardBonus = -1,      // -1 to success rewards
            StartingFavorability = 65,
            Name = "The Icahn",
            Description = "Hard - An activist investor demanding immediate results",
            FlavorText = "Carl Icahn is known for hostile takeovers and relentlessly pressuring management. Your board includes activist investors who measure success in quarters, not years."
        },
        _ => ForDifficulty(Difficulty.TheNadella)
    };

    /// <summary>
    /// Gets all available difficulties for UI display.
    /// </summary>
    public static IReadOnlyList<Difficulty> AllDifficulties => new[]
    {
        Difficulty.TheWelch,
        Difficulty.TheNadella,
        Difficulty.TheIcahn
    };
}
