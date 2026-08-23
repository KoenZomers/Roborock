namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Tracks local Roborock protocol session state.
/// </summary>
internal sealed class LocalSession
{
    private uint _sequence;

    /// <summary>
    /// Initializes a new local session.
    /// </summary>
    /// <param name="localKey">The Roborock local key.</param>
    /// <param name="duid">The optional Roborock device identifier.</param>
    public LocalSession(string localKey, string? duid = null)
    {
        if (string.IsNullOrWhiteSpace(localKey))
        {
            throw new ArgumentException("Roborock local key is required.", nameof(localKey));
        }

        LocalKey = localKey;
        Duid = string.IsNullOrWhiteSpace(duid) ? null : duid;
    }

    /// <summary>
    /// Gets the Roborock local key.
    /// </summary>
    public string LocalKey { get; }

    /// <summary>
    /// Gets the optional Roborock device identifier.
    /// </summary>
    public string? Duid { get; }

    /// <summary>
    /// Gets the next Roborock protocol sequence number.
    /// </summary>
    /// <returns>The next sequence number, wrapped to one after the Roborock upper bound.</returns>
    public uint NextSequence()
    {
        uint next = ++_sequence;
        if (next > 999999)
        {
            _sequence = 1;
            return _sequence;
        }

        return next;
    }
}
