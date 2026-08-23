using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents one Roborock room segment to IoT room identifier mapping returned by <c>get_room_mapping</c>.
/// </summary>
public sealed class RoborockRoomMapping
{
    /// <summary>
    /// Gets or sets the map segment identifier used by room-cleaning commands and map data.
    /// </summary>
    [JsonPropertyName("segment_id")]
    public int SegmentId { get; set; }

    /// <summary>
    /// Gets or sets the Roborock IoT room identifier used to resolve room names from account home data.
    /// </summary>
    [JsonPropertyName("iot_id")]
    public string IotId { get; set; } = string.Empty;

    /// <summary>
    /// Converts a <c>get_room_mapping</c> response to typed room mappings.
    /// </summary>
    /// <param name="value">The JSON response returned by <c>get_room_mapping</c>.</param>
    /// <returns>The normalized room mappings.</returns>
    /// <exception cref="InvalidDataException">Thrown when the response is not a Roborock room-mapping array.</exception>
    public static IReadOnlyList<RoborockRoomMapping> FromJson(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Roborock returned an invalid room mapping response: {value.GetRawText()}.");
        }

        if (value.GetArrayLength() == 2 && value[0].ValueKind != JsonValueKind.Array)
        {
            return [ParseEntry(value)];
        }

        var mappings = new List<RoborockRoomMapping>();
        foreach (JsonElement entry in value.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() < 2)
            {
                throw new InvalidDataException($"Roborock returned an invalid room mapping entry: {entry.GetRawText()}.");
            }

            mappings.Add(ParseEntry(entry));
        }

        return mappings;
    }

    /// <summary>
    /// Parses one room mapping entry in <c>[segment_id, iot_id]</c> format.
    /// </summary>
    /// <param name="entry">The JSON entry to parse.</param>
    /// <returns>The parsed room mapping.</returns>
    private static RoborockRoomMapping ParseEntry(JsonElement entry)
    {
        int? segmentId = entry[0].GetInt32OrNull();
        if (segmentId is null)
        {
            throw new InvalidDataException($"Roborock returned a room mapping without a numeric segment id: {entry.GetRawText()}.");
        }

        return new RoborockRoomMapping
        {
            SegmentId = segmentId.Value,
            IotId = entry[1].ValueKind == JsonValueKind.String ? entry[1].GetString() ?? string.Empty : entry[1].GetRawText()
        };
    }
}
