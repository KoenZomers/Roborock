using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Library.Utils;

/// <summary>
/// Provides shared JSON serializer options for Roborock local protocol payloads.
/// </summary>
internal static class RoborockJson
{
    /// <summary>
    /// Gets indented JSON serializer options using Roborock snake_case naming.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Gets compact JSON serializer options using Roborock snake_case naming.
    /// </summary>
    public static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
