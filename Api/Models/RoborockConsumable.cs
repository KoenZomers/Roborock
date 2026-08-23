using System.Text.Json;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents consumable usage returned by <c>get_consumable</c>.
/// </summary>
public sealed class RoborockConsumable
{
    private const int MainBrushReplaceTime = 1_080_000;
    private const int SideBrushReplaceTime = 720_000;
    private const int FilterReplaceTime = 540_000;
    private const int SensorDirtyReplaceTime = 108_000;
    private const int MopRollerReplaceTime = 1_080_000;
    private const int StrainerReplaceTime = 540_000;
    private const int CleaningBrushReplaceTime = 1_080_000;
    private const int DustCollectionReplaceTime = 81_000;

    /// <summary>
    /// Gets the main brush work duration.
    /// </summary>
    public TimeSpan? MainBrushWorkDuration { get; private init; }

    /// <summary>
    /// Gets the side brush work duration.
    /// </summary>
    public TimeSpan? SideBrushWorkDuration { get; private init; }

    /// <summary>
    /// Gets the filter work duration.
    /// </summary>
    public TimeSpan? FilterWorkDuration { get; private init; }

    /// <summary>
    /// Gets the filter element work duration.
    /// </summary>
    public TimeSpan? FilterElementWorkDuration { get; private init; }

    /// <summary>
    /// Gets the sensor dirty duration.
    /// </summary>
    public TimeSpan? SensorDirtyDuration { get; private init; }

    /// <summary>
    /// Gets the strainer work duration.
    /// </summary>
    public TimeSpan? StrainerWorkDuration { get; private init; }

    /// <summary>
    /// Gets the dust collection work duration.
    /// </summary>
    public TimeSpan? DustCollectionWorkDuration { get; private init; }

    /// <summary>
    /// Gets the cleaning brush work duration.
    /// </summary>
    public TimeSpan? CleaningBrushWorkDuration { get; private init; }

    /// <summary>
    /// Gets the mop roller work duration.
    /// </summary>
    public TimeSpan? MopRollerWorkDuration { get; private init; }

    /// <summary>
    /// Gets the remaining main brush lifetime.
    /// </summary>
    public TimeSpan? MainBrushTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining side brush lifetime.
    /// </summary>
    public TimeSpan? SideBrushTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining filter lifetime.
    /// </summary>
    public TimeSpan? FilterTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining sensor cleaning interval.
    /// </summary>
    public TimeSpan? SensorTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining strainer lifetime.
    /// </summary>
    public TimeSpan? StrainerTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining dust collection maintenance interval.
    /// </summary>
    public TimeSpan? DustCollectionTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining cleaning brush lifetime.
    /// </summary>
    public TimeSpan? CleaningBrushTimeLeft { get; private init; }

    /// <summary>
    /// Gets the remaining mop roller lifetime.
    /// </summary>
    public TimeSpan? MopRollerTimeLeft { get; private init; }

    /// <summary>
    /// Converts a Roborock consumable JSON object to a clean typed consumable model.
    /// </summary>
    public static RoborockConsumable FromJson(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Roborock returned an invalid consumable response: {value.GetRawText()}.");
        }

        int? mainBrushWorkTime = GetInt32(value, "main_brush_work_time");
        int? sideBrushWorkTime = GetInt32(value, "side_brush_work_time");
        int? filterWorkTime = GetInt32(value, "filter_work_time");
        int? filterElementWorkTime = GetInt32(value, "filter_element_work_time");
        int? sensorDirtyTime = GetInt32(value, "sensor_dirty_time");
        int? strainerWorkTimes = GetInt32(value, "strainer_work_times");
        int? dustCollectionWorkTimes = GetInt32(value, "dust_collection_work_times");
        int? cleaningBrushWorkTimes = GetInt32(value, "cleaning_brush_work_times");
        int? mopRollerWorkTime = GetInt32(value, "moproller_work_time");

        return new RoborockConsumable
        {
            MainBrushWorkDuration = ToDuration(mainBrushWorkTime),
            SideBrushWorkDuration = ToDuration(sideBrushWorkTime),
            FilterWorkDuration = ToDuration(filterWorkTime),
            FilterElementWorkDuration = ToDuration(filterElementWorkTime),
            SensorDirtyDuration = ToDuration(sensorDirtyTime),
            StrainerWorkDuration = ToDuration(strainerWorkTimes),
            DustCollectionWorkDuration = ToDuration(dustCollectionWorkTimes),
            CleaningBrushWorkDuration = ToDuration(cleaningBrushWorkTimes),
            MopRollerWorkDuration = ToDuration(mopRollerWorkTime),
            MainBrushTimeLeft = ToDuration(Remaining(MainBrushReplaceTime, mainBrushWorkTime)),
            SideBrushTimeLeft = ToDuration(Remaining(SideBrushReplaceTime, sideBrushWorkTime)),
            FilterTimeLeft = ToDuration(Remaining(FilterReplaceTime, filterWorkTime)),
            SensorTimeLeft = ToDuration(Remaining(SensorDirtyReplaceTime, sensorDirtyTime)),
            StrainerTimeLeft = ToDuration(Remaining(StrainerReplaceTime, strainerWorkTimes)),
            DustCollectionTimeLeft = ToDuration(Remaining(DustCollectionReplaceTime, dustCollectionWorkTimes)),
            CleaningBrushTimeLeft = ToDuration(Remaining(CleaningBrushReplaceTime, cleaningBrushWorkTimes)),
            MopRollerTimeLeft = ToDuration(Remaining(MopRollerReplaceTime, mopRollerWorkTime))
        };
    }

    /// <summary>
    /// Reads an optional integer property from a consumable object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static int? GetInt32(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt32OrNull() : null;

    /// <summary>
    /// Calculates remaining consumable lifetime in seconds.
    /// </summary>
    /// <param name="replaceTime">The replacement interval in seconds.</param>
    /// <param name="workTime">The consumed work time in seconds.</param>
    /// <returns>The remaining lifetime in seconds, or <see langword="null" /> when work time is unavailable.</returns>
    private static int? Remaining(int replaceTime, int? workTime) => workTime is null ? null : replaceTime - workTime.Value;

    /// <summary>
    /// Converts an optional seconds value to a duration.
    /// </summary>
    /// <param name="seconds">The seconds to convert.</param>
    /// <returns>The duration, or <see langword="null" /> when no value is available.</returns>
    private static TimeSpan? ToDuration(int? seconds) => seconds is null ? null : TimeSpan.FromSeconds(seconds.Value);
}
