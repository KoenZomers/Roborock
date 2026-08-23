using System.Text.Json;
using KoenZomers.RoboRock.Api.Enums;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests parsing of Roborock status responses.
/// </summary>
public sealed class RoborockStatusTests
{
    [Fact]
    /// <summary>
    /// Verifies that get_status diagnostics shown by Home Assistant are exposed as typed properties.
    /// </summary>
    public void FromJson_WhenGetStatusContainsWaterDiagnostics_ExposesTypedProperties()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {
              "msg_ver": 2,
              "msg_seq": 123,
              "state": 5,
              "battery": 84,
              "error_code": 0,
              "fan_power": 102,
              "clean_area": 23500000,
              "clean_time": 1800,
              "map_present": 1,
              "map_status": 28,
              "in_cleaning": 3,
              "in_returning": 0,
              "water_box_mode": 202,
              "water_box_status": 1,
              "water_box_carriage_status": 1,
              "water_shortage_status": 0,
              "mop_mode": 300
            }
            """);

        RoborockStatus status = RoborockStatus.FromJson(document.RootElement);

        Assert.Equal(RoborockStateCode.Cleaning, status.State);
        Assert.Equal(84, status.Battery);
        Assert.Equal(23.5, status.SquareMeterCleanArea);
        Assert.Equal(TimeSpan.FromMinutes(30), status.CleanDuration);
        Assert.True(status.HasMap);
        Assert.Equal(28, status.MapStatus);
        Assert.Equal(7, status.CurrentMap);
        Assert.Equal(RoborockInCleaning.SegmentCleanNotComplete, status.InCleaning);
        Assert.Equal(0, status.InReturning);
        Assert.Equal(RoborockWaterBoxMode.Medium, status.WaterBoxMode);
        Assert.Equal(RoborockAttachmentStatus.Attached, status.WaterBoxStatus);
        Assert.Equal(RoborockAttachmentStatus.Attached, status.WaterBoxCarriageStatus);
        Assert.Equal(RoborockWaterShortageStatus.None, status.WaterShortageStatus);
        Assert.True(status.IsWaterBoxAttached);
        Assert.True(status.IsMopAttached);
        Assert.False(status.HasWaterShortage);
        Assert.Equal(300, status.MopMode);
    }

    [Fact]
    /// <summary>
    /// Verifies that single-item get_prop style status arrays are accepted.
    /// </summary>
    public void FromJson_WhenGetPropReturnsSingleStatusObjectArray_UnwrapsStatus()
    {
        using JsonDocument document = JsonDocument.Parse("""
            [
              {
                "state": 3,
                "battery": 100,
                "error_code": 0,
                "fan_power": 102,
                "clean_area": 0,
                "clean_time": 0,
                "map_present": 1,
                "map_status": 252,
                "water_shortage_status": 1
              }
            ]
            """);

        RoborockStatus status = RoborockStatus.FromJson(document.RootElement);

        Assert.Equal(RoborockStateCode.Idle, status.State);
        Assert.Null(status.CurrentMap);
        Assert.Equal(RoborockWaterShortageStatus.Shortage, status.WaterShortageStatus);
        Assert.True(status.HasWaterShortage);
    }

    [Fact]
    /// <summary>
    /// Verifies that missing optional diagnostics remain unknown instead of being forced to false.
    /// </summary>
    public void FromJson_WhenOptionalDiagnosticsAreMissing_LeavesNullablePropertiesUnset()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {
              "state": 8,
              "battery": 90,
              "error_code": 0,
              "fan_power": 102,
              "clean_area": 0,
              "clean_time": 0,
              "map_present": 0,
              "water_box_mode": 0
            }
            """);

        RoborockStatus status = RoborockStatus.FromJson(document.RootElement);

        Assert.Null(status.WaterBoxStatus);
        Assert.Null(status.WaterBoxCarriageStatus);
        Assert.Null(status.WaterShortageStatus);
        Assert.Null(status.IsWaterBoxAttached);
        Assert.Null(status.IsMopAttached);
        Assert.Null(status.HasWaterShortage);
        Assert.Null(status.CurrentMap);
    }
}
