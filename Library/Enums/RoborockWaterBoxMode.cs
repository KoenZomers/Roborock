namespace KoenZomers.RoboRock.Library.Enums;

/// <summary>
/// Describes water-flow or mop-intensity modes used by Roborock V1 vacuums.
/// </summary>
public enum RoborockWaterBoxMode
{
    /// <summary>The water box mode is unknown or was not reported.</summary>
    Unknown = 0,

    /// <summary>Water flow is disabled.</summary>
    Off = 200,

    /// <summary>Low, mild or slight water flow depending on model.</summary>
    Low = 201,

    /// <summary>Medium or standard water flow depending on model.</summary>
    Medium = 202,

    /// <summary>High or intense water flow depending on model.</summary>
    High = 203,

    /// <summary>Customized water flow.</summary>
    Custom = 204,

    /// <summary>Minimum water flow on models with extended water levels.</summary>
    Min = 205,

    /// <summary>Maximum water flow on models with extended water levels.</summary>
    Max = 206,

    /// <summary>Custom water-flow percentage on supported models.</summary>
    CustomWaterFlow = 207,

    /// <summary>Extreme water flow on supported models.</summary>
    Extreme = 208,

    /// <summary>Smart water-flow mode on supported models.</summary>
    SmartMode = 209,

    /// <summary>Start of pure-water slide flow range.</summary>
    PureWaterFlowStart = 221,

    /// <summary>Small pure-water slide flow.</summary>
    PureWaterFlowSmall = 225,

    /// <summary>Middle pure-water slide flow.</summary>
    PureWaterFlowMiddle = 235,

    /// <summary>Large pure-water slide flow.</summary>
    PureWaterFlowLarge = 245,

    /// <summary>High pure-water slide flow.</summary>
    PureWaterFlowHigh = 248,

    /// <summary>Extreme pure-water slide flow.</summary>
    PureWaterFlowExtreme = 250
}
