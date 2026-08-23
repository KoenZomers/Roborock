namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes the cleaning mode recorded in a Roborock cleaning history record.
/// </summary>
public enum RoborockCleanType
{
    /// <summary>The robot cleaned all reachable zones.</summary>
    AllZone = 1,

    /// <summary>The robot cleaned a drawn zone.</summary>
    DrawZone = 2,

    /// <summary>The robot cleaned selected rooms or zones.</summary>
    SelectZone = 3,

    /// <summary>The robot performed quick map building.</summary>
    QuickBuild = 4,

    /// <summary>The robot performed a video patrol.</summary>
    VideoPatrol = 5,

    /// <summary>The robot performed a pet patrol.</summary>
    PetPatrol = 6
}
