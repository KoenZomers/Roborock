namespace KoenZomers.RoboRock.Library.Enums;

/// <summary>
/// Describes the current high-level state reported by a Roborock V1 vacuum.
/// </summary>
public enum RoborockStateCode
{
    /// <summary>The device returned an unknown or unmapped state code.</summary>
    Unknown = 0,

    /// <summary>The vacuum is starting.</summary>
    Starting = 1,

    /// <summary>The charger is disconnected from the vacuum.</summary>
    ChargerDisconnected = 2,

    /// <summary>The vacuum is idle.</summary>
    Idle = 3,

    /// <summary>The vacuum is being driven through remote control.</summary>
    RemoteControlActive = 4,

    /// <summary>The vacuum is cleaning.</summary>
    Cleaning = 5,

    /// <summary>The vacuum is returning to the dock.</summary>
    ReturningHome = 6,

    /// <summary>The vacuum is in manual mode.</summary>
    ManualMode = 7,

    /// <summary>The vacuum is charging.</summary>
    Charging = 8,

    /// <summary>The vacuum reports a charging problem.</summary>
    ChargingProblem = 9,

    /// <summary>The current cleaning job is paused.</summary>
    Paused = 10,

    /// <summary>The vacuum is spot cleaning.</summary>
    SpotCleaning = 11,

    /// <summary>The vacuum is in an error state.</summary>
    Error = 12,

    /// <summary>The vacuum is shutting down.</summary>
    ShuttingDown = 13,

    /// <summary>The vacuum is updating firmware or data.</summary>
    Updating = 14,

    /// <summary>The vacuum is docking.</summary>
    Docking = 15,

    /// <summary>The vacuum is going to a target point.</summary>
    GoingToTarget = 16,

    /// <summary>The vacuum is cleaning a zone.</summary>
    ZonedCleaning = 17,

    /// <summary>The vacuum is cleaning selected room segments.</summary>
    SegmentCleaning = 18,

    /// <summary>The dock is emptying the dust bin.</summary>
    EmptyingTheBin = 22,

    /// <summary>The dock is washing the mop.</summary>
    WashingTheMop = 23,

    /// <summary>The dock is washing the mop; alias used by some models.</summary>
    WashingTheMopAlternate = 25,

    /// <summary>The vacuum is returning to the dock to wash the mop.</summary>
    GoingToWashTheMop = 26,

    /// <summary>The vacuum camera call feature is active.</summary>
    InCall = 28,

    /// <summary>The vacuum is mapping.</summary>
    Mapping = 29,

    /// <summary>The vacuum is in a vendor-specific easter-egg mode.</summary>
    EggAttack = 30,

    /// <summary>The vacuum is patrolling.</summary>
    Patrol = 32,

    /// <summary>The dock is attaching the mop.</summary>
    AttachingTheMop = 33,

    /// <summary>The dock is detaching the mop.</summary>
    DetachingTheMop = 34,

    /// <summary>The vacuum is fully charged.</summary>
    ChargingComplete = 100,

    /// <summary>The device is offline.</summary>
    DeviceOffline = 101,

    /// <summary>The vacuum is locked.</summary>
    Locked = 103,

    /// <summary>The dock is stopping air drying.</summary>
    AirDryingStopping = 202,

    /// <summary>The robot is mopping.</summary>
    RobotStatusMopping = 6301,

    /// <summary>The robot is cleaning the mop while cleaning.</summary>
    CleanMopCleaning = 6302,

    /// <summary>The robot is mopping after cleaning the mop.</summary>
    CleanMopMopping = 6303,

    /// <summary>The robot is mopping selected room segments.</summary>
    SegmentMopping = 6304,

    /// <summary>The robot is cleaning and washing the mop for selected segments.</summary>
    SegmentCleanMopCleaning = 6305,

    /// <summary>The robot is mopping selected segments after washing the mop.</summary>
    SegmentCleanMopMopping = 6306,

    /// <summary>The robot is mopping a zone.</summary>
    ZonedMopping = 6307,

    /// <summary>The robot is cleaning and washing the mop for a zone.</summary>
    ZonedCleanMopCleaning = 6308,

    /// <summary>The robot is mopping a zone after washing the mop.</summary>
    ZonedCleanMopMopping = 6309,

    /// <summary>The robot is returning to the dock to wash the duster.</summary>
    BackToDockWashingDuster = 6310
}
