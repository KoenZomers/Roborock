using System.Security.Cryptography;
using System.Text;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Contains Roborock cloud MQTT connection details needed for cloud map retrieval.
/// </summary>
public sealed class RoborockCloudConnectionOptions
{
    /// <summary>
    /// Gets or sets the Roborock device identifier.
    /// </summary>
    public string Duid { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Roborock device local key used to encrypt MQTT payloads.
    /// </summary>
    public string LocalKey { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Roborock RRiot <c>u</c> value.
    /// </summary>
    public string User { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Roborock RRiot <c>s</c> value.
    /// </summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Roborock RRiot <c>k</c> value.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Roborock RRiot MQTT URL, for example <c>ssl://mqtt.example.com:8883</c>.
    /// </summary>
    public string MqttUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the MQTT host parsed from <see cref="MqttUrl"/>.
    /// </summary>
    public string MqttHost => MqttUri.Host;

    /// <summary>
    /// Gets the MQTT port parsed from <see cref="MqttUrl"/>.
    /// </summary>
    public int MqttPort => MqttUri.Port;

    /// <summary>
    /// Gets whether the MQTT connection should use TLS.
    /// </summary>
    public bool UseTls => string.Equals(MqttUri.Scheme, "ssl", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(MqttUri.Scheme, "mqtts", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the MQTT username derived from the RRiot user and key.
    /// </summary>
    public string MqttUsername => Md5Hex($"{User}:{Key}")[2..10];

    /// <summary>
    /// Gets the MQTT password derived from the RRiot secret and key.
    /// </summary>
    public string MqttPassword => Md5Hex($"{Secret}:{Key}")[16..];

    /// <summary>
    /// Gets the topic used to publish commands to the device.
    /// </summary>
    public string PublishTopic => $"rr/m/i/{User}/{MqttUsername}/{Duid}";

    /// <summary>
    /// Gets the topic used to subscribe to device responses.
    /// </summary>
    public string SubscribeTopic => $"rr/m/o/{User}/{MqttUsername}/{Duid}";

    /// <summary>
    /// Validates the required cloud connection options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a required value is missing or invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Duid))
        {
            throw new ArgumentException("Roborock device id is required.", nameof(Duid));
        }

        if (string.IsNullOrWhiteSpace(LocalKey))
        {
            throw new ArgumentException("Roborock local key is required.", nameof(LocalKey));
        }

        if (string.IsNullOrWhiteSpace(User))
        {
            throw new ArgumentException("Roborock RRiot user is required.", nameof(User));
        }

        if (string.IsNullOrWhiteSpace(Secret))
        {
            throw new ArgumentException("Roborock RRiot secret is required.", nameof(Secret));
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new ArgumentException("Roborock RRiot key is required.", nameof(Key));
        }

        if (!Uri.TryCreate(MqttUrl, UriKind.Absolute, out Uri? uri) || string.IsNullOrWhiteSpace(uri.Host) || uri.Port <= 0)
        {
            throw new ArgumentException("Roborock RRiot MQTT URL must include a scheme, host and port.", nameof(MqttUrl));
        }
    }

    private Uri MqttUri => Uri.TryCreate(MqttUrl, UriKind.Absolute, out Uri? uri)
        ? uri
        : throw new InvalidOperationException("Roborock RRiot MQTT URL is invalid.");

    private static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
