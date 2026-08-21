namespace KoenZomers.RoboRock.Library.Enums;

/// <summary>
/// Describes a synthesized dock-related status derived from the vacuum state and battery level.
/// </summary>
public enum RoborockDockStatus
{
    /// <summary>The dock status could not be determined.</summary>
    Unknown = 0,

    /// <summary>The vacuum is away from the dock or not doing a dock-specific action.</summary>
    Idle = 1,

    /// <summary>The vacuum is returning to the dock.</summary>
    Returning = 2,

    /// <summary>The vacuum is on the dock and charging.</summary>
    Charging = 3,

    /// <summary>The vacuum is on the dock and fully charged.</summary>
    Full = 4,

    /// <summary>The dock is emptying the dust bin.</summary>
    Dusting = 5
}
