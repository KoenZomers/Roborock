namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Describes a rectangular section of a Roborock map image block.
/// </summary>
internal readonly record struct RoborockMapImageBounds(int Left, int Top, int Width, int Height)
{
    /// <summary>
    /// Gets the exclusive right coordinate.
    /// </summary>
    public int Right => Left + Width;

    /// <summary>
    /// Gets the exclusive bottom coordinate.
    /// </summary>
    public int Bottom => Top + Height;
}
