namespace InertiCorp.Core.Situation;

/// <summary>
/// The type of response available for handling a situation.
/// </summary>
public enum ResponseType
{
    /// <summary>Spend Political Capital for better odds (70% Good outcome)</summary>
    PC,

    /// <summary>Roll the dice - standard risk (40% Good, 40% Expected, 20% Bad)</summary>
    Risk,

    /// <summary>Use ethically questionable tactics (+Evil score, 60% Good)</summary>
    Evil,

    /// <summary>Put it aside - may resurface later with escalated severity</summary>
    Defer
}
