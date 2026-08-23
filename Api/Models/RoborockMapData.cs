using KoenZomers.RoboRock.Api.Utils;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Contains the raw Roborock map payload returned by <c>get_map_v1</c>.
/// </summary>
/// <remarks>
/// The payload is decrypted and decompressed RRMap data. Use <see cref="ToImage" /> or <see cref="ToPng" />
/// to render it as a directly usable PNG image.
/// </remarks>
public sealed class RoborockMapData
{
    /// <summary>
    /// Initializes a new raw map payload instance.
    /// </summary>
    /// <param name="content">The decrypted and decompressed Roborock map bytes.</param>
    public RoborockMapData(byte[] content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the decrypted and decompressed Roborock map bytes.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Renders the Roborock map payload to a PNG image.
    /// </summary>
    /// <returns>A rendered PNG map image with dimensions and content metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when the map payload is not a valid RRMap image.</exception>
    public RoborockMapImage ToImage() => RoborockMapRenderer.RenderPng(Content);

    /// <summary>
    /// Renders the Roborock map payload to PNG bytes.
    /// </summary>
    /// <returns>The PNG-encoded map image bytes.</returns>
    /// <exception cref="InvalidDataException">Thrown when the map payload is not a valid RRMap image.</exception>
    public byte[] ToPng() => ToImage().PngContent;

    /// <summary>
    /// Gets the vacuum position embedded in the Roborock map payload.
    /// </summary>
    /// <returns>The vacuum position, or <see langword="null" /> when the map does not contain one.</returns>
    /// <exception cref="InvalidDataException">Thrown when the map payload is not a valid RRMap file.</exception>
    public RoborockMapPosition? GetVacuumPosition() => RoborockMapParser.GetVacuumPosition(Content);

    /// <summary>
    /// Resolves the room segment currently containing the vacuum from the Roborock map payload.
    /// </summary>
    /// <param name="roomMappings">Optional room segment to IoT room mappings returned by <c>get_room_mapping</c>.</param>
    /// <param name="rooms">Optional Roborock cloud rooms used to resolve friendly room names.</param>
    /// <returns>The current room, or <see langword="null" /> when the map does not contain a resolvable room.</returns>
    /// <exception cref="InvalidDataException">Thrown when the map payload is not a valid RRMap file.</exception>
    public RoborockCurrentRoom? GetCurrentRoom(
        IReadOnlyList<RoborockRoomMapping>? roomMappings = null,
        IReadOnlyList<RoborockRoomInfo>? rooms = null) =>
        RoborockMapParser.GetCurrentRoom(Content, roomMappings, rooms);
}
