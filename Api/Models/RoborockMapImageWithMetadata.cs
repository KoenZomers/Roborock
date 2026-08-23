namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Contains a rendered Roborock map image together with the matching multi-map metadata.
/// </summary>
public sealed class RoborockMapImageWithMetadata
{
    /// <summary>
    /// Initializes a new rendered map image with metadata instance.
    /// </summary>
    /// <param name="image">The rendered PNG map image.</param>
    /// <param name="mapFlag">The current map flag reported by the vacuum.</param>
    /// <param name="map">The metadata entry matching <paramref name="mapFlag" />, when available.</param>
    /// <param name="maps">All map metadata entries returned by the vacuum.</param>
    public RoborockMapImageWithMetadata(
        RoborockMapImage image,
        int? mapFlag,
        RoborockMapInfo? map,
        IReadOnlyList<RoborockMapInfo> maps)
        : this(image, mapFlag, map, maps, currentRoom: null)
    {
    }

    /// <summary>
    /// Initializes a new rendered map image with metadata instance.
    /// </summary>
    /// <param name="image">The rendered PNG map image.</param>
    /// <param name="mapFlag">The current map flag reported by the vacuum.</param>
    /// <param name="map">The metadata entry matching <paramref name="mapFlag" />, when available.</param>
    /// <param name="maps">All map metadata entries returned by the vacuum.</param>
    /// <param name="currentRoom">The room segment currently containing the vacuum, when available.</param>
    public RoborockMapImageWithMetadata(
        RoborockMapImage image,
        int? mapFlag,
        RoborockMapInfo? map,
        IReadOnlyList<RoborockMapInfo> maps,
        RoborockCurrentRoom? currentRoom)
    {
        Image = image ?? throw new ArgumentNullException(nameof(image));
        MapFlag = mapFlag;
        Map = map;
        Maps = maps ?? throw new ArgumentNullException(nameof(maps));
        CurrentRoom = currentRoom;
    }

    /// <summary>
    /// Gets the rendered PNG map image.
    /// </summary>
    public RoborockMapImage Image { get; }

    /// <summary>
    /// Gets the current map flag reported by the vacuum.
    /// </summary>
    public int? MapFlag { get; }

    /// <summary>
    /// Gets the matching metadata entry for the rendered map, when available.
    /// </summary>
    public RoborockMapInfo? Map { get; }

    /// <summary>
    /// Gets all map metadata entries returned by the vacuum.
    /// </summary>
    public IReadOnlyList<RoborockMapInfo> Maps { get; }

    /// <summary>
    /// Gets the room segment currently containing the vacuum, when available.
    /// </summary>
    public RoborockCurrentRoom? CurrentRoom { get; }

    /// <summary>
    /// Gets the friendly name of the rendered map, when available.
    /// </summary>
    public string? Name => Map?.Name;

    /// <summary>
    /// Gets the PNG-encoded map image bytes.
    /// </summary>
    public byte[] PngContent => Image.PngContent;

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width => Image.Width;

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height => Image.Height;

    /// <summary>
    /// Gets the MIME content type for <see cref="PngContent" />.
    /// </summary>
    public string ContentType => Image.ContentType;

    /// <summary>
    /// Saves the PNG-encoded map image to disk.
    /// </summary>
    /// <param name="filePath">The path where the PNG image should be written.</param>
    public void Save(string filePath) => Image.Save(filePath);

    /// <summary>
    /// Creates a map image result by matching the current map flag to the known map list.
    /// </summary>
    /// <param name="image">The rendered PNG map image.</param>
    /// <param name="status">The current vacuum status.</param>
    /// <param name="maps">The available multi-map metadata entries.</param>
    /// <param name="currentRoom">The room segment currently containing the vacuum, when available.</param>
    /// <returns>The combined image and metadata result.</returns>
    public static RoborockMapImageWithMetadata Create(
        RoborockMapImage image,
        RoborockStatus status,
        IReadOnlyList<RoborockMapInfo> maps,
        RoborockCurrentRoom? currentRoom = null)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(maps);

        int? mapFlag = status.CurrentMap;
        RoborockMapInfo? map = mapFlag is null ? null : maps.FirstOrDefault(candidate => candidate.MapFlag == mapFlag.Value);
        return new RoborockMapImageWithMetadata(image, mapFlag, map, maps, currentRoom);
    }
}
