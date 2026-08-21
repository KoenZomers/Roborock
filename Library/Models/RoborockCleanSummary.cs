using System.Text.Json;

namespace KoenZomers.RoboRock.Library.Models;

/// <summary>
/// Represents totals returned by the Roborock <c>get_clean_summary</c> command.
/// </summary>
public sealed class RoborockCleanSummary
{
    /// <summary>
    /// Gets the total cleaning time.
    /// </summary>
    public TimeSpan? CleanDuration { get; private init; }

    /// <summary>
    /// Gets the total cleaned area in square meters.
    /// </summary>
    public double? SquareMeterCleanArea { get; private init; }

    /// <summary>
    /// Gets the total clean count.
    /// </summary>
    public int? CleanCount { get; private init; }

    /// <summary>
    /// Gets the total dust collection count, if the device reports it.
    /// </summary>
    public int? DustCollectionCount { get; private init; }

    /// <summary>
    /// Gets the clean record identifiers, newest first on V1 vacuums.
    /// </summary>
    public IReadOnlyList<long>? Records { get; private init; }

    /// <summary>
    /// Gets the timestamp of the last clean as a local date and time.
    /// </summary>
    public DateTimeOffset? LastCleanDateTime { get; private init; }

    /// <summary>
    /// Converts Roborock's flexible summary response shape to a typed summary.
    /// </summary>
    public static RoborockCleanSummary FromJson(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return new RoborockCleanSummary
            {
                CleanDuration = ToDuration(GetInt32(value, "clean_time")),
                SquareMeterCleanArea = ToSquareMeters(GetInt64(value, "clean_area")),
                CleanCount = GetInt32(value, "clean_count"),
                DustCollectionCount = GetInt32(value, "dust_collection_count"),
                Records = GetProperty(value, "records")?.GetInt64ListOrNull(),
                LastCleanDateTime = ToLocalDateTime(GetInt64(value, "last_clean_t"))
            };
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return new RoborockCleanSummary
            {
                CleanDuration = value.GetArrayLength() > 0 ? ToDuration(value[0].GetInt32OrNull()) : null,
                SquareMeterCleanArea = value.GetArrayLength() > 1 ? ToSquareMeters(value[1].GetInt64OrNull()) : null,
                CleanCount = value.GetArrayLength() > 2 ? value[2].GetInt32OrNull() : null,
                Records = value.GetArrayLength() > 3 ? value[3].GetInt64ListOrNull() : null,
                DustCollectionCount = value.GetArrayLength() > 4 ? value[4].GetInt32OrNull() : null
            };
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return new RoborockCleanSummary { CleanDuration = ToDuration(value.GetInt32OrNull()) };
        }

        return new RoborockCleanSummary();
    }

    /// <summary>
    /// Reads an optional property from a clean summary object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The property value, or <see langword="null" /> when missing.</returns>
    private static JsonElement? GetProperty(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property : null;

    /// <summary>
    /// Reads an optional integer property from a clean summary object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static int? GetInt32(JsonElement value, string propertyName) =>
        GetProperty(value, propertyName)?.GetInt32OrNull();

    /// <summary>
    /// Reads an optional long integer property from a clean summary object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The long integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static long? GetInt64(JsonElement value, string propertyName) =>
        GetProperty(value, propertyName)?.GetInt64OrNull();

    /// <summary>
    /// Converts an optional seconds value to a duration.
    /// </summary>
    /// <param name="seconds">The seconds to convert.</param>
    /// <returns>The duration, or <see langword="null" /> when no value is available.</returns>
    private static TimeSpan? ToDuration(int? seconds) =>
        seconds is null ? null : TimeSpan.FromSeconds(seconds.Value);

    /// <summary>
    /// Converts optional square millimeters to square meters.
    /// </summary>
    /// <param name="squareMillimeters">The square millimeters to convert.</param>
    /// <returns>The rounded square meter value, or <see langword="null" /> when no value is available.</returns>
    private static double? ToSquareMeters(long? squareMillimeters) =>
        squareMillimeters is null ? null : Math.Round(squareMillimeters.Value / 1_000_000d, 1);

    /// <summary>
    /// Converts an optional Unix timestamp to local date and time.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp to convert.</param>
    /// <returns>The local date and time, or <see langword="null" /> when no timestamp is available.</returns>
    private static DateTimeOffset? ToLocalDateTime(long? unixTimestamp) =>
        unixTimestamp is null ? null : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value).ToLocalTime();
}
