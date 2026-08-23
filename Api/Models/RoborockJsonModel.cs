using System.Text.Json;
using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Provides shared JSON serializer options for Roborock model serialization.
/// </summary>
internal static class RoborockJsonModel
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
}
