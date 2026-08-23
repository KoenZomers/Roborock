using System.Text.Json;
using KoenZomers.RoboRock.Api.Enums;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Represents the status returned by the Roborock <c>get_status</c> command.
/// </summary>
public sealed class RoborockStatus
{
    /// <summary>
    /// Gets the Roborock state.
    /// </summary>
    public RoborockStateCode State { get; private init; }

    /// <summary>
    /// Gets the battery percentage.
    /// </summary>
    public int Battery { get; private init; }

    /// <summary>
    /// Gets the Roborock error code. A value of <see cref="RoborockErrorCode.None"/> means no error.
    /// </summary>
    public RoborockErrorCode ErrorCode { get; private init; }

    /// <summary>
    /// Gets the fan power mode.
    /// </summary>
    public RoborockFanPower FanPower { get; private init; }

    /// <summary>
    /// Gets the cleaned area reported by the vacuum in square meters.
    /// </summary>
    public double SquareMeterCleanArea { get; private init; }

    /// <summary>
    /// Gets the current cleaning duration.
    /// </summary>
    public TimeSpan CleanDuration { get; private init; }

    /// <summary>
    /// Gets whether a map is present on the device.
    /// </summary>
    public bool HasMap { get; private init; }

    /// <summary>
    /// Gets the water box mode.
    /// </summary>
    public RoborockWaterBoxMode WaterBoxMode { get; private init; }

    /// <summary>
    /// Gets a synthesized dock status derived from <see cref="State"/> and <see cref="Battery"/>.
    /// </summary>
    public RoborockDockStatus DockStatus { get; private init; }

    /// <summary>
    /// Gets the Roborock status message version.
    /// </summary>
    public int MessageVersion { get; private init; }

    /// <summary>
    /// Gets the Roborock status message sequence number.
    /// </summary>
    public int MessageSequence { get; private init; }

    /// <summary>
    /// Gets a friendly name for <see cref="State"/>.
    /// </summary>
    public string StateName =>
        State switch
        {
            RoborockStateCode.Starting => "starting",
            RoborockStateCode.ChargerDisconnected => "charger_disconnected",
            RoborockStateCode.Idle => "idle",
            RoborockStateCode.RemoteControlActive => "remote_control_active",
            RoborockStateCode.Cleaning => "cleaning",
            RoborockStateCode.ReturningHome => "returning_home",
            RoborockStateCode.ManualMode => "manual_mode",
            RoborockStateCode.Charging => "charging",
            RoborockStateCode.ChargingProblem => "charging_problem",
            RoborockStateCode.Paused => "paused",
            RoborockStateCode.SpotCleaning => "spot_cleaning",
            RoborockStateCode.Error => "error",
            RoborockStateCode.ShuttingDown => "shutting_down",
            RoborockStateCode.Updating => "updating",
            RoborockStateCode.Docking => "docking",
            RoborockStateCode.GoingToTarget => "going_to_target",
            RoborockStateCode.ZonedCleaning => "zoned_cleaning",
            RoborockStateCode.SegmentCleaning => "segment_cleaning",
            RoborockStateCode.EmptyingTheBin => "emptying_the_bin",
            RoborockStateCode.WashingTheMop or RoborockStateCode.WashingTheMopAlternate => "washing_the_mop",
            RoborockStateCode.GoingToWashTheMop => "going_to_wash_the_mop",
            RoborockStateCode.ChargingComplete => "charging_complete",
            RoborockStateCode.DeviceOffline => "device_offline",
            _ => $"unknown({(int)State})"
        };

    /// <summary>
    /// Converts a Roborock status JSON object to a clean typed status model.
    /// </summary>
    public static RoborockStatus FromJson(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Roborock returned an invalid status response: {value.GetRawText()}.");
        }

        RoborockStateCode state = (RoborockStateCode)(GetInt32(value, "state") ?? 0);
        int battery = GetInt32(value, "battery") ?? 0;

        return new RoborockStatus
        {
            State = state,
            Battery = battery,
            ErrorCode = (RoborockErrorCode)(GetInt32(value, "error_code") ?? 0),
            FanPower = (RoborockFanPower)(GetInt32(value, "fan_power") ?? 0),
            SquareMeterCleanArea = Math.Round((GetInt64(value, "clean_area") ?? 0) / 1_000_000d, 1),
            CleanDuration = TimeSpan.FromSeconds(GetInt32(value, "clean_time") ?? 0),
            HasMap = (GetInt32(value, "map_present") ?? 0) != 0,
            WaterBoxMode = (RoborockWaterBoxMode)(GetInt32(value, "water_box_mode") ?? 0),
            DockStatus = GetDockStatus(state, battery),
            MessageVersion = GetInt32(value, "msg_ver") ?? 0,
            MessageSequence = GetInt32(value, "msg_seq") ?? 0
        };
    }

    /// <summary>
    /// Derives a dock status from the Roborock state and battery level.
    /// </summary>
    /// <param name="state">The current Roborock state.</param>
    /// <param name="battery">The current battery percentage.</param>
    /// <returns>The derived dock status.</returns>
    private static RoborockDockStatus GetDockStatus(RoborockStateCode state, int battery) =>
        state switch
        {
            RoborockStateCode.EmptyingTheBin => RoborockDockStatus.Dusting,
            RoborockStateCode.ChargingComplete => RoborockDockStatus.Full,
            RoborockStateCode.Charging when battery == 100 => RoborockDockStatus.Full,
            RoborockStateCode.Charging => RoborockDockStatus.Charging,
            RoborockStateCode.ReturningHome or RoborockStateCode.Docking => RoborockDockStatus.Returning,
            RoborockStateCode.Unknown => RoborockDockStatus.Unknown,
            _ => RoborockDockStatus.Idle
        };

    /// <summary>
    /// Reads an optional integer property from a status object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static int? GetInt32(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt32OrNull() : null;

    /// <summary>
    /// Reads an optional long integer property from a status object.
    /// </summary>
    /// <param name="value">The JSON object to read.</param>
    /// <param name="propertyName">The property name to read.</param>
    /// <returns>The long integer value, or <see langword="null" /> when missing or incompatible.</returns>
    private static long? GetInt64(JsonElement value, string propertyName) =>
        value.TryGetProperty(propertyName, out JsonElement property) ? property.GetInt64OrNull() : null;
}
