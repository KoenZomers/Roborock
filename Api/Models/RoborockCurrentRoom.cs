namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Describes the room segment that currently contains the vacuum according to the map payload.
/// </summary>
public sealed class RoborockCurrentRoom
{
    /// <summary>
    /// Initializes a current-room result.
    /// </summary>
    /// <param name="segmentId">The map segment identifier containing the vacuum.</param>
    /// <param name="iotId">The Roborock IoT room identifier matched through <c>get_room_mapping</c>, when available.</param>
    /// <param name="vacuumPosition">The vacuum position used to resolve the segment.</param>
    public RoborockCurrentRoom(int segmentId, string? iotId, RoborockMapPosition vacuumPosition)
    {
        SegmentId = segmentId;
        IotId = iotId;
        VacuumPosition = vacuumPosition ?? throw new ArgumentNullException(nameof(vacuumPosition));
    }

    /// <summary>
    /// Gets the map segment identifier containing the vacuum.
    /// </summary>
    public int SegmentId { get; }

    /// <summary>
    /// Gets the Roborock IoT room identifier matched through <c>get_room_mapping</c>, when available.
    /// </summary>
    public string? IotId { get; }

    /// <summary>
    /// Gets the vacuum position used to resolve the segment, including rendered PNG coordinates when available.
    /// </summary>
    public RoborockMapPosition VacuumPosition { get; }
}
