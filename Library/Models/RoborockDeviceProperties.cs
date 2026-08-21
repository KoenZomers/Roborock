using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Library.Models;

/// <summary>
/// Aggregates the same local diagnostic data that Home Assistant obtains through python-roborock's <c>get_prop()</c>.
/// </summary>
public sealed class RoborockDeviceProperties
{
    /// <summary>
    /// Gets or sets the current status from <c>get_status</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public RoborockStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets total cleaning history from <c>get_clean_summary</c>.
    /// </summary>
    [JsonPropertyName("clean_summary")]
    public RoborockCleanSummary? CleanSummary { get; set; }

    /// <summary>
    /// Gets or sets consumable usage from <c>get_consumable</c>.
    /// </summary>
    [JsonPropertyName("consumable")]
    public RoborockConsumable? Consumable { get; set; }

    /// <summary>
    /// Gets or sets the newest cleaning record from <c>get_clean_record</c>.
    /// </summary>
    [JsonPropertyName("last_clean_record")]
    public RoborockCleanRecord? LastCleanRecord { get; set; }
}
