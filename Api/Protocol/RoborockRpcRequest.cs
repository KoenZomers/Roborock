using System.Text.Json.Serialization;

namespace KoenZomers.RoboRock.Api.Protocol;

/// <summary>
/// Represents a Roborock RPC request body before it is wrapped in the local DPS payload.
/// </summary>
internal sealed class RoborockRpcRequest
{
    /// <summary>
    /// Gets the request identifier used to match the response.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// Gets the Roborock RPC method name.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; init; } = "";

    /// <summary>
    /// Gets the RPC parameters sent with the request.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; init; } = Array.Empty<object>();
}
