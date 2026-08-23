namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes fan power / suction mode codes used by Roborock V1 vacuums.
/// </summary>
public enum RoborockFanPower
{
    /// <summary>The fan power is unknown or was not reported.</summary>
    Unknown = 0,

    /// <summary>Old V1 quiet/silent suction mode.</summary>
    OldQuiet = 38,

    /// <summary>Old V1 balanced or standard suction mode.</summary>
    OldBalanced = 60,

    /// <summary>Old V1 turbo suction mode.</summary>
    OldTurbo = 75,

    /// <summary>Old V1 maximum suction mode.</summary>
    OldMax = 100,

    /// <summary>Quiet suction mode.</summary>
    Quiet = 101,

    /// <summary>Balanced suction mode.</summary>
    Balanced = 102,

    /// <summary>Turbo suction mode.</summary>
    Turbo = 103,

    /// <summary>Maximum suction mode.</summary>
    Max = 104,

    /// <summary>Gentle or mop-only mode depending on device capabilities.</summary>
    Gentle = 105,

    /// <summary>Customized suction mode.</summary>
    Custom = 106,

    /// <summary>Maximum-plus suction mode on supported models.</summary>
    MaxPlus = 108,

    /// <summary>Vacuum off with raised main brush on supported models.</summary>
    OffRaiseMainBrush = 109,

    /// <summary>Smart suction mode on supported models.</summary>
    SmartMode = 110
}
