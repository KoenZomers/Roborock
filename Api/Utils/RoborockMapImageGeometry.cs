namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Provides geometry helpers for Roborock map image blocks.
/// </summary>
internal static class RoborockMapImageGeometry
{
    private const byte OutsidePixel = 0x00;

    /// <summary>
    /// Gets the smallest image bounds containing all non-outside map pixels.
    /// </summary>
    /// <param name="pixels">The raw RRMap image pixels.</param>
    /// <param name="width">The raw image width.</param>
    /// <param name="height">The raw image height.</param>
    /// <returns>The cropped bounds, or the full image bounds when the image is empty.</returns>
    public static RoborockMapImageBounds GetContentBounds(byte[] pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be greater than zero.");
        }

        if (pixels.Length < checked(width * height))
        {
            throw new ArgumentException("The pixel buffer is smaller than the declared image dimensions.", nameof(pixels));
        }

        int left = width;
        int top = height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsOutsidePixel(pixels[x + (y * width)]))
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < left || bottom < top
            ? new RoborockMapImageBounds(0, 0, width, height)
            : new RoborockMapImageBounds(left, top, right - left + 1, bottom - top + 1);
    }

    /// <summary>
    /// Gets whether a raw RRMap image pixel is outside the known map.
    /// </summary>
    /// <param name="pixel">The raw RRMap image pixel value.</param>
    /// <returns><see langword="true" /> when the pixel is outside the known map.</returns>
    public static bool IsOutsidePixel(byte pixel) => pixel == OutsidePixel;
}
