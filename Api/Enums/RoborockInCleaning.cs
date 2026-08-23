namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes the current cleaning session type reported by Roborock V1 vacuums.
/// </summary>
public enum RoborockInCleaning
{
    /// <summary>No incomplete cleaning session is active.</summary>
    Complete = 0,

    /// <summary>A global clean is active or can be resumed.</summary>
    GlobalCleanNotComplete = 1,

    /// <summary>A zone clean is active or can be resumed.</summary>
    ZoneCleanNotComplete = 2,

    /// <summary>A segment clean is active or can be resumed.</summary>
    SegmentCleanNotComplete = 3,

    /// <summary>A map-building run is active or can be resumed.</summary>
    MapBuildNotComplete = 4
}
