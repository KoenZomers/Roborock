namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes Roborock V1 error codes returned by <c>get_status</c> and cleaning history.
/// </summary>
public enum RoborockErrorCode
{
    /// <summary>No error is currently reported.</summary>
    None = 0,

    /// <summary>The lidar turret is blocked.</summary>
    LidarBlocked = 1,

    /// <summary>The bumper is stuck.</summary>
    BumperStuck = 2,

    /// <summary>The wheels are suspended.</summary>
    WheelsSuspended = 3,

    /// <summary>A cliff sensor error was detected.</summary>
    CliffSensorError = 4,

    /// <summary>The main brush is jammed.</summary>
    MainBrushJammed = 5,

    /// <summary>The side brush is jammed.</summary>
    SideBrushJammed = 6,

    /// <summary>The wheels are jammed.</summary>
    WheelsJammed = 7,

    /// <summary>The robot is trapped.</summary>
    RobotTrapped = 8,

    /// <summary>The dustbin is missing.</summary>
    NoDustbin = 9,

    /// <summary>The filter or strainer is wet or blocked.</summary>
    StrainerError = 10,

    /// <summary>A strong magnetic field or compass error was detected.</summary>
    CompassError = 11,

    /// <summary>The battery is too low.</summary>
    LowBattery = 12,

    /// <summary>The robot reports a charging error.</summary>
    ChargingError = 13,

    /// <summary>The robot reports a battery error.</summary>
    BatteryError = 14,

    /// <summary>The wall sensor is dirty.</summary>
    WallSensorDirty = 15,

    /// <summary>The robot is tilted.</summary>
    RobotTilted = 16,

    /// <summary>The side brush reports an error.</summary>
    SideBrushError = 17,

    /// <summary>The fan reports an error.</summary>
    FanError = 18,

    /// <summary>The dock is not connected to power.</summary>
    Dock = 19,

    /// <summary>The optical flow sensor is dirty.</summary>
    OpticalFlowSensorDirt = 20,

    /// <summary>The vertical bumper is pressed.</summary>
    VerticalBumperPressed = 21,

    /// <summary>The dock locator reports an error.</summary>
    DockLocatorError = 22,

    /// <summary>The robot failed to return to the dock.</summary>
    ReturnToDockFail = 23,

    /// <summary>A no-go zone was detected.</summary>
    NogoZoneDetected = 24,

    /// <summary>The visual sensor or camera reports an error.</summary>
    VisualSensor = 25,

    /// <summary>The light-touch or wall sensor reports an error.</summary>
    LightTouch = 26,

    /// <summary>The VibraRise module is jammed.</summary>
    VibrariseJammed = 27,

    /// <summary>The robot is on carpet where it cannot continue.</summary>
    RobotOnCarpet = 28,

    /// <summary>The filter is blocked.</summary>
    FilterBlocked = 29,

    /// <summary>An invisible wall was detected.</summary>
    InvisibleWallDetected = 30,

    /// <summary>The robot cannot cross carpet.</summary>
    CannotCrossCarpet = 31,

    /// <summary>An internal robot error was reported.</summary>
    InternalError = 32,

    /// <summary>The auto-empty dock needs cleaning.</summary>
    CollectDustError = 34,

    /// <summary>The auto-empty dock reports a voltage error.</summary>
    AutoEmptyDockVoltageError = 35,

    /// <summary>The mop roller may be jammed.</summary>
    MoppingRoller = 36,

    /// <summary>The mop roller is not lowered properly.</summary>
    MoppingRollerNotLowered = 37,

    /// <summary>The clean water tank needs attention.</summary>
    ClearWaterBox = 38,

    /// <summary>The dirty water tank needs attention.</summary>
    DirtyWaterBox = 39,

    /// <summary>The dock water filter must be reinstalled.</summary>
    SinkStrainer = 40,

    /// <summary>The clean water tank is empty.</summary>
    ClearWaterBoxEmpty = 41,

    /// <summary>The cleaning brush or water filter needs attention.</summary>
    ClearBrush = 42,

    /// <summary>The positioning button reports an error.</summary>
    PositioningButton = 43,

    /// <summary>The dock water filter screen needs cleaning.</summary>
    FilterScreen = 44,

    /// <summary>The mop roller may be jammed; alternate code used by some models.</summary>
    MoppingRollerAlternate = 45,

    /// <summary>The water supply reports an exception.</summary>
    UpWaterException = 48,

    /// <summary>The water drain reports an exception.</summary>
    DrainWaterException = 49,

    /// <summary>The unit temperature protection was triggered.</summary>
    TemperatureProtection = 51,

    /// <summary>The cleaning carousel reports an exception.</summary>
    CleanCarouselException = 52,

    /// <summary>The cleaning carousel water tank is full.</summary>
    CleanCarouselWaterFull = 53,

    /// <summary>The water carriage dropped.</summary>
    WaterCarriageDrop = 54,

    /// <summary>The cleaning carousel needs checking.</summary>
    CheckCleanCarousel = 55,

    /// <summary>The robot reports an audio error.</summary>
    AudioError = 56
}
