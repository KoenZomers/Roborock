namespace KoenZomers.RoboRock.Library.Models;

/// <summary>
/// Contains a rendered Roborock map image that can be saved, streamed or embedded directly.
/// </summary>
public sealed class RoborockMapImage
{
    /// <summary>
    /// Initializes a new rendered map image instance.
    /// </summary>
    /// <param name="pngContent">The PNG-encoded map image bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    public RoborockMapImage(byte[] pngContent, int width, int height)
    {
        PngContent = pngContent ?? throw new ArgumentNullException(nameof(pngContent));
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Map image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Map image height must be greater than zero.");
        }

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the PNG-encoded map image bytes.
    /// </summary>
    public byte[] PngContent { get; }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the MIME content type for <see cref="PngContent" />.
    /// </summary>
    public string ContentType => "image/png";
}
