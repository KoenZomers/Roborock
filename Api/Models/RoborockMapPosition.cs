namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents a position reported in Roborock map coordinates and, when available, rendered PNG coordinates.
/// </summary>
public sealed class RoborockMapPosition
{
    /// <summary>
    /// Initializes a new Roborock map position.
    /// </summary>
    /// <param name="x">The X coordinate in Roborock map units.</param>
    /// <param name="y">The Y coordinate in Roborock map units.</param>
    /// <param name="angle">The optional orientation angle reported by the vacuum.</param>
    /// <param name="renderedX">The X coordinate in the rendered PNG image, where the origin is the top-left corner.</param>
    /// <param name="renderedY">The Y coordinate in the rendered PNG image, where the origin is the top-left corner.</param>
    public RoborockMapPosition(int x, int y, int? angle = null, int? renderedX = null, int? renderedY = null)
    {
        X = x;
        Y = y;
        Angle = angle;
        RenderedX = renderedX;
        RenderedY = renderedY;
    }

    /// <summary>
    /// Gets the X coordinate in Roborock map units.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y coordinate in Roborock map units.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the optional orientation angle reported by the vacuum.
    /// </summary>
    public int? Angle { get; }

    /// <summary>
    /// Gets the X coordinate in the rendered PNG image, where the origin is the top-left corner.
    /// </summary>
    public int? RenderedX { get; }

    /// <summary>
    /// Gets the Y coordinate in the rendered PNG image, where the origin is the top-left corner.
    /// </summary>
    public int? RenderedY { get; }
}
