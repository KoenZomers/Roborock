using System.Text.Json;
using KoenZomers.RoboRock.Api.Enums;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents a single Roborock cleaning history record returned by <c>get_clean_record</c>.
/// </summary>
public sealed class RoborockCleanRecord
{
    /// <summary>
    /// Gets the local date and time when cleaning started.
    /// </summary>
    public DateTimeOffset? BeginDateTime { get; private init; }

    /// <summary>
    /// Gets the local date and time when cleaning ended.
    /// </summary>
    public DateTimeOffset? EndDateTime { get; private init; }

    /// <summary>
    /// Gets the cleaning duration.
    /// </summary>
    public TimeSpan? CleaningDuration { get; private init; }

    /// <summary>
    /// Gets the cleaned area in square meters.
    /// </summary>
    public double? SquareMeterArea { get; private init; }

    /// <summary>
    /// Gets the Roborock error reported for the cleaning record.
    /// </summary>
    public RoborockErrorCode? Error { get; private init; }

    /// <summary>
    /// Gets whether the cleaning run completed.
    /// </summary>
    public bool? IsComplete { get; private init; }

    /// <summary>
    /// Gets how the cleaning run was started.
    /// </summary>
    public RoborockCleanStartType? StartType { get; private init; }

    /// <summary>
    /// Gets the cleaning mode recorded for the run.
    /// </summary>
    public RoborockCleanType? CleanType { get; private init; }

    /// <summary>
    /// Gets why the cleaning run ended.
    /// </summary>
    public RoborockFinishReason? FinishReason { get; private init; }

    /// <summary>
    /// Gets the dust collection status reported with the record.
    /// </summary>
    public int? DustCollectionStatus { get; private init; }

    /// <summary>
    /// Gets the count of obstacle avoidance events.
    /// </summary>
    public int? AvoidCount { get; private init; }

    /// <summary>
    /// Gets the count of mop washing events.
    /// </summary>
    public int? WashCount { get; private init; }

    /// <summary>
    /// Gets the map flag associated with the cleaning record.
    /// </summary>
    public int? MapFlag { get; private init; }

    /// <summary>
    /// Converts Roborock's flexible clean-record response shape to a typed record.
    /// </summary>
    public static RoborockCleanRecord? FromJson(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => FromObject(value),
            JsonValueKind.Array => FromArray(value),
            _ => null
        };
    }

    /// <summary>
    /// Converts an object-shaped clean record response to a typed record.
    /// </summary>
    /// <param name="value">The object-shaped clean record JSON value.</param>
    /// <returns>The typed clean record.</returns>
    private static RoborockCleanRecord FromObject(JsonElement value)
    {
        return Create(
            GetInt64(value, "begin"),
            GetInt64(value, "end"),
            GetInt32(value, "duration"),
            GetInt64(value, "area"),
            GetInt32(value, "error"),
            GetInt32(value, "complete"),
            GetInt32(value, "start_type"),
            GetInt32(value, "clean_type"),
            GetInt32(value, "finish_reason"),
            GetInt32(value, "dust_collection_status"),
            GetInt32(value, "avoid_count"),
            GetInt32(value, "wash_count"),
            GetInt32(value, "map_flag"));
    }

    /// <summary>
    /// Converts an array-shaped clean record response to a typed record.
    /// </summary>
    /// <param name="value">The array-shaped clean record JSON value.</param>
    /// <returns>The typed clean record, or <see langword="null" /> when the array is empty.</returns>
    private static RoborockCleanRecord? FromArray(JsonElement value)
    {
        if (value.GetArrayLength() == 0)
        {
            return null;
        }

        if (value[0].ValueKind == JsonValueKind.Object)
        {
            var records = value.EnumerateArray()
                .Select(FromJson)
                .Where(record => record is not null)
                .Cast<RoborockCleanRecord>()
                .ToList();

            return records.Count == 0 ? null : CombineSegmentedRecord(records);
        }

        return Create(
            value.GetArrayLength() > 0 ? value[0].GetInt64OrNull() : null,
            value.GetArrayLength() > 1 ? value[1].GetInt64OrNull() : null,
            value.GetArrayLength() > 2 ? value[2].GetInt32OrNull() : null,
            value.GetArrayLength() > 3 ? value[3].GetInt64OrNull() : null,
            value.GetArrayLength() > 4 ? value[4].GetInt32OrNull() : null,
            value.GetArrayLength() > 5 ? value[5].GetInt32OrNull() : null,
            value.GetArrayLength() > 6 ? value[6].GetInt32OrNull() : null,
            value.GetArrayLength() > 7 ? value[7].GetInt32OrNull() : null,
            value.GetArrayLength() > 8 ? value[8].GetInt32OrNull() : null,
            value.GetArrayLength() > 9 ? value[9].GetInt32OrNull() : null,
            value.GetArrayLength() > 10 ? value[10].GetInt32OrNull() : null,
            value.GetArrayLength() > 11 ? value[11].GetInt32OrNull() : null,
            value.GetArrayLength() > 12 ? value[12].GetInt32OrNull() : null);
    }

    /// <summary>
    /// Creates a clean record from raw Roborock scalar values.
    /// </summary>
    /// <param name="begin">The cleaning start Unix timestamp.</param>
    /// <param name="end">The cleaning end Unix timestamp.</param>
    /// <param name="duration">The cleaning duration in seconds.</param>
    /// <param name="area">The cleaned area in square millimeters.</param>
    /// <param name="error">The Roborock error code.</param>
    /// <param name="complete">The completion flag.</param>
    /// <param name="startType">The raw cleaning start type.</param>
    /// <param name="cleanType">The raw cleaning type.</param>
    /// <param name="finishReason">The raw finish reason.</param>
    /// <param name="dustCollectionStatus">The dust collection status.</param>
    /// <param name="avoidCount">The obstacle avoidance count.</param>
    /// <param name="washCount">The mop wash count.</param>
    /// <param name="mapFlag">The map flag.</param>
    /// <returns>The typed clean record.</returns>
    private static RoborockCleanRecord Create(
        long? begin,
        long? end,
        int? duration,
        long? area,
        int? error,
        int? complete,
        int? startType,
        int? cleanType,
        int? finishReason,
        int? dustCollectionStatus,
        int? avoidCount,
        int? washCount,
        int? mapFlag)
    {
        return new RoborockCleanRecord
        {
            BeginDateTime = ToLocalDateTime(begin),
            EndDateTime = ToLocalDateTime(end),
            CleaningDuration = ToDuration(duration),
            SquareMeterArea = ToSquareMeters(area),
            Error = error is null ? null : (RoborockErrorCode)error.Value,
            IsComplete = complete is null ? null : complete != 0,
            StartType = startType is null ? null : (RoborockCleanStartType)startType.Value,
            CleanType = cleanType is null ? null : (RoborockCleanType)cleanType.Value,
            FinishReason = finishReason is null ? null : (RoborockFinishReason)finishReason.Value,
            DustCollectionStatus = dustCollectionStatus,
            AvoidCount = avoidCount,
            WashCount = washCount,
            MapFlag = mapFlag
        };
    }

    /// <summary>
    /// Combines segmented clean records into one aggregate record.
    /// </summary>
    /// <param name="records">The segmented records to combine.</param>
    /// <returns>The aggregate clean record.</returns>
    private static RoborockCleanRecord CombineSegmentedRecord(IReadOnlyList<RoborockCleanRecord> records)
    {
        RoborockCleanRecord first = records[0];
        RoborockCleanRecord last = records[^1];
        return new RoborockCleanRecord
        {
            BeginDateTime = first.BeginDateTime,
            EndDateTime = last.EndDateTime,
            CleaningDuration = records.Aggregate(TimeSpan.Zero, (total, record) => total + (record.CleaningDuration ?? TimeSpan.Zero)),
            SquareMeterArea = Math.Round(records.Sum(record => record.SquareMeterArea ?? 0), 1),
            Error = last.Error,
            IsComplete = last.IsComplete,
            StartType = first.StartType,
            CleanType = last.CleanType,
            FinishReason = last.FinishReason,
            DustCollectionStatus = last.DustCollectionStatus,
            AvoidCount = records.Sum(record => record.AvoidCount ?? 0),
            WashCount = records.Sum(record => record.WashCount ?? 0),
            MapFlag = last.MapFlag
        };
    }

    /// <summary>
    /// Reads an optional integer property from a clean record object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static int? GetInt32(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt32OrNull() : null;

    /// <summary>
    /// Reads an optional long integer property from a clean record object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The long integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static long? GetInt64(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt64OrNull() : null;

    /// <summary>
    /// Converts an optional Unix timestamp to local date and time.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp to convert.</param>
    /// <returns>The local date and time, or <see langword="null" /> when no timestamp is available.</returns>
    private static DateTimeOffset? ToLocalDateTime(long? unixTimestamp) =>
        unixTimestamp is null ? null : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value).ToLocalTime();

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
}
