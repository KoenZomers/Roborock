using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents a Roborock cloud room with its friendly name.
/// </summary>
public sealed class RoborockRoomInfo
{
    /// <summary>
    /// Gets the Roborock cloud room identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// Gets the friendly room name configured in Roborock.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Roborock IoT room identifier used by <c>get_room_mapping</c>.
    /// </summary>
    [JsonIgnore]
    public string IotId => Id.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a Roborock cloud rooms response to typed room information.
    /// </summary>
    /// <param name="value">The JSON response returned by the cloud rooms endpoint, or its <c>result</c> array.</param>
    /// <returns>The parsed cloud rooms.</returns>
    /// <exception cref="InvalidDataException">Thrown when the response is not a Roborock rooms response.</exception>
    public static IReadOnlyList<RoborockRoomInfo> FromJson(JsonElement value)
    {
        JsonElement rooms = value;
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (!value.TryGetProperty("result", out rooms))
            {
                throw new InvalidDataException($"Roborock returned a rooms response without result: {value.GetRawText()}.");
            }
        }

        if (rooms.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Roborock returned an invalid rooms response: {value.GetRawText()}.");
        }

        var result = new List<RoborockRoomInfo>();
        foreach (JsonElement room in rooms.EnumerateArray())
        {
            result.Add(ParseRoom(room));
        }

        return result;
    }

    private static RoborockRoomInfo ParseRoom(JsonElement room)
    {
        if (room.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Roborock returned an invalid room entry: {room.GetRawText()}.");
        }

        int? id = GetInt32(room, "id") ?? GetInt32(room, "roomId");
        if (id is null)
        {
            throw new InvalidDataException($"Roborock returned a room entry without a numeric id: {room.GetRawText()}.");
        }

        return new RoborockRoomInfo
        {
            Id = id.Value,
            Name = GetString(room, "name") ?? string.Empty
        };
    }

    private static int? GetInt32(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt32OrNull() : null;

    private static string? GetString(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
}
