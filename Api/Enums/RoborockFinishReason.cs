namespace KoenZomers.RoboRock.Api.Enums;

/// <summary>
/// Describes why a Roborock cleaning history record ended.
/// </summary>
public enum RoborockFinishReason
{
    /// <summary>Cleaning was manually interrupted by the user.</summary>
    ManualInterrupt = 21,

    /// <summary>Cleaning was interrupted.</summary>
    CleanupInterrupted = 24,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt29 = 29,

    /// <summary>The robot could not continue cleaning.</summary>
    Breakpoint = 32,

    /// <summary>The robot could not continue cleaning; alternate code.</summary>
    Breakpoint33 = 33,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted34 = 34,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt35 = 35,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt36 = 36,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt37 = 37,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt43 = 43,

    /// <summary>The robot failed to locate itself.</summary>
    LocateFail = 45,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt48 = 48,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt49 = 49,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt50 = 50,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted51 = 51,

    /// <summary>The robot finished cleaning successfully.</summary>
    FinishedCleaning = 52,

    /// <summary>The robot finished cleaning successfully; alternate code.</summary>
    FinishedCleaning54 = 54,

    /// <summary>The robot finished cleaning successfully; alternate code.</summary>
    FinishedCleaning55 = 55,

    /// <summary>The robot finished cleaning successfully; alternate code.</summary>
    FinishedCleaning56 = 56,

    /// <summary>The robot finished cleaning successfully; alternate code.</summary>
    FinishedCleaning57 = 57,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt60 = 60,

    /// <summary>The requested area was unreachable.</summary>
    AreaUnreachable = 61,

    /// <summary>The requested area was unreachable; alternate code.</summary>
    AreaUnreachable62 = 62,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted64 = 64,

    /// <summary>The robot failed to locate itself; alternate code.</summary>
    LocateFail65 = 65,

    /// <summary>The dock reported a washing error.</summary>
    WashingError = 67,

    /// <summary>The robot failed to return to the dock for washing.</summary>
    BackToWashFailure = 68,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted101 = 101,

    /// <summary>The robot could not continue cleaning; alternate code.</summary>
    Breakpoint102 = 102,

    /// <summary>Cleaning was manually interrupted by the user; alternate code.</summary>
    ManualInterrupt103 = 103,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted104 = 104,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted105 = 105,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted106 = 106,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted107 = 107,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted109 = 109,

    /// <summary>Cleaning was interrupted; alternate code.</summary>
    CleanupInterrupted110 = 110,

    /// <summary>The patrol completed successfully.</summary>
    PatrolSuccess = 114,

    /// <summary>The patrol failed.</summary>
    PatrolFail = 115,

    /// <summary>The pet patrol found the pet.</summary>
    PetPatrolSuccess = 116,

    /// <summary>The pet patrol failed to find the pet.</summary>
    PetPatrolFail = 117
}
