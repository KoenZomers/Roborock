using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents one Roborock multi-map entry returned by <c>get_multi_maps_list</c>.
/// </summary>
public sealed class RoborockMapInfo
{
    /// <summary>
    /// Gets the Roborock map flag used to load or identify this map.
    /// </summary>
    [JsonPropertyName("mapFlag")]
    public int MapFlag { get; init; }

    /// <summary>
    /// Gets the friendly map name configured in Roborock.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the raw Unix timestamp reported by Roborock for when this map was added.
    /// </summary>
    [JsonPropertyName("add_time")]
    public long? AddedAtUnixTime { get; init; }

    /// <summary>
    /// Gets the time this map was added, when Roborock reports it.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? AddedAt => AddedAtUnixTime is null ? null : DateTimeOffset.FromUnixTimeSeconds(AddedAtUnixTime.Value);

    /// <summary>
    /// Gets the name length reported by Roborock.
    /// </summary>
    [JsonPropertyName("length")]
    public int? NameLength { get; init; }

    /// <summary>
    /// Gets backup maps associated with this map, when Roborock reports them.
    /// </summary>
    [JsonPropertyName("bak_maps")]
    public IReadOnlyList<RoborockMapInfo> BackupMaps { get; init; } = [];

    /// <summary>
    /// Converts a <c>get_multi_maps_list</c> response to typed map entries.
    /// </summary>
    /// <param name="value">The JSON response returned by <c>get_multi_maps_list</c>, or its <c>map_info</c> array.</param>
    /// <returns>The normalized map entries.</returns>
    /// <exception cref="InvalidDataException">Thrown when the response is not a Roborock multi-map response.</exception>
    public static IReadOnlyList<RoborockMapInfo> FromJson(JsonElement value)
    {
        JsonElement mapInfo = value;
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (!value.TryGetProperty("map_info", out mapInfo))
            {
                throw new InvalidDataException($"Roborock returned a multi-map response without map_info: {value.GetRawText()}.");
            }
        }

        if (mapInfo.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"Roborock returned an invalid multi-map response: {value.GetRawText()}.");
        }

        var maps = new List<RoborockMapInfo>();
        foreach (JsonElement entry in mapInfo.EnumerateArray())
        {
            maps.Add(ParseEntry(entry));
        }

        return maps;
    }

    private static RoborockMapInfo ParseEntry(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Roborock returned an invalid multi-map entry: {entry.GetRawText()}.");
        }

        int? mapFlag = GetInt32(entry, "mapFlag") ?? GetInt32(entry, "map_flag");
        if (mapFlag is null)
        {
            throw new InvalidDataException($"Roborock returned a multi-map entry without a numeric map flag: {entry.GetRawText()}.");
        }

        return new RoborockMapInfo
        {
            MapFlag = mapFlag.Value,
            Name = GetString(entry, "name") ?? string.Empty,
            AddedAtUnixTime = GetInt64(entry, "add_time"),
            NameLength = GetInt32(entry, "length"),
            BackupMaps = TryGetProperty(entry, "bak_maps", out JsonElement backupMaps) && backupMaps.ValueKind == JsonValueKind.Array
                ? FromJson(backupMaps)
                : []
        };
    }

    private static int? GetInt32(JsonElement value, string propertyName) =>
        TryGetProperty(value, propertyName, out JsonElement property) ? property.GetInt32OrNull() : null;

    private static long? GetInt64(JsonElement value, string propertyName) =>
        TryGetProperty(value, propertyName, out JsonElement property) ? property.GetInt64OrNull() : null;

    private static string? GetString(JsonElement value, string propertyName) =>
        TryGetProperty(value, propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

    private static bool TryGetProperty(JsonElement value, string propertyName, out JsonElement property) =>
        value.TryGetProperty(propertyName, out property);
}
