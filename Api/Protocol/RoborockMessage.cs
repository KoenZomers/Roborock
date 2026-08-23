namespace KoenZomers.RoboRock.Api.Protocol;

/// <summary>
/// Represents a decoded Roborock local protocol message.
/// </summary>
internal sealed class RoborockMessage
{
    /// <summary>
    /// Gets or sets the protocol version prefix.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the message sequence number.
    /// </summary>
    public uint Sequence { get; set; }

    /// <summary>
    /// Gets or sets the random message value.
    /// </summary>
    public uint Random { get; set; }

    /// <summary>
    /// Gets or sets the message Unix timestamp.
    /// </summary>
    public uint Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the Roborock protocol message type.
    /// </summary>
    public RoborockMessageProtocol Protocol { get; set; }

    /// <summary>
    /// Gets or sets the optional decrypted payload bytes.
    /// </summary>
    public byte[]? Payload { get; set; }
}
