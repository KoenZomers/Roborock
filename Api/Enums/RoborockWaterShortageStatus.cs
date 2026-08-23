namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes the clean-water shortage state reported by Roborock V1 vacuums.
/// </summary>
public enum RoborockWaterShortageStatus
{
    /// <summary>No water shortage is reported.</summary>
    None = 0,

    /// <summary>The vacuum reports a water shortage.</summary>
    Shortage = 1
}
