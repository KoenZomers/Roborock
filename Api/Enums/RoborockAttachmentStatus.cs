namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes whether a removable Roborock accessory is detected by the vacuum.
/// </summary>
public enum RoborockAttachmentStatus
{
    /// <summary>The accessory is not attached or not detected.</summary>
    Detached = 0,

    /// <summary>The accessory is attached and detected.</summary>
    Attached = 1
}
