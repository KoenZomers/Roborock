using System.Text.Json;

namespace KoenZomers.RoboRock.Library.Models;

/// <summary>
/// Provides safe conversion helpers for optional Roborock JSON values.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Reads a JSON number as an integer when possible.
    /// </summary>
    /// <param name="value">The JSON value to read.</param>
    /// <returns>The integer value, or <see langword="null" /> when it is not a compatible number.</returns>
    public static int? GetInt32OrNull(this JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : null;

    /// <summary>
    /// Reads a JSON number as a long integer when possible.
    /// </summary>
    /// <param name="value">The JSON value to read.</param>
    /// <returns>The long integer value, or <see langword="null" /> when it is not a compatible number.</returns>
    public static long? GetInt64OrNull(this JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long result) ? result : null;

    /// <summary>
    /// Reads a JSON array as a list of long integers when possible.
    /// </summary>
    /// <param name="value">The JSON value to read.</param>
    /// <returns>The integer list, or <see langword="null" /> when the value is not an array.</returns>
    public static IReadOnlyList<long>? GetInt64ListOrNull(this JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var records = new List<long>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.GetInt64OrNull() is { } record)
            {
                records.Add(record);
            }
        }

        return records;
    }
}
