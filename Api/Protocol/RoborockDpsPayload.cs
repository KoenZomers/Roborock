using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Protocol;

/// <summary>
/// Represents the DPS wrapper used by Roborock V1 local messages.
/// </summary>
internal sealed class RoborockDpsPayload
{
    /// <summary>
    /// Initializes a new DPS payload wrapper.
    /// </summary>
    /// <param name="dps">The DPS key/value payload entries.</param>
    /// <param name="timestamp">The Unix timestamp assigned to the payload.</param>
    public RoborockDpsPayload(Dictionary<string, string> dps, uint timestamp)
    {
        Dps = dps;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the DPS key/value payload entries.
    /// </summary>
    [JsonPropertyName("dps")]
    public Dictionary<string, string> Dps { get; }

    /// <summary>
    /// Gets the Unix timestamp assigned to the payload.
    /// </summary>
    [JsonPropertyName("t")]
    public uint Timestamp { get; }
}
