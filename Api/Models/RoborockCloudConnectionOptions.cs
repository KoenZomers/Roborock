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
    /// Gets or sets the Roborock RRiot <c>h</c> value used to sign cloud home requests.
    /// </summary>
    public string? Hash { get; init; }

    /// <summary>
    /// Gets or sets the Roborock cloud API URL from RRiot <c>r.a</c>, for example <c>https://api-eu.roborock.com</c>.
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// Gets or sets the Roborock account base URL, for example <c>https://euiot.roborock.com</c>.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Gets or sets the Roborock account token used to resolve the home id when <see cref="HomeId" /> is not provided.
    /// </summary>
    public string? UserToken { get; init; }

    /// <summary>
    /// Gets or sets the Roborock home id. When omitted, <see cref="BaseUrl" /> and <see cref="UserToken" /> are used to resolve it.
    /// </summary>
    public long? HomeId { get; init; }

    /// <summary>
    /// Gets whether the options include enough data to retrieve cloud room names.
    /// </summary>
    public bool HasHomeDataConfig =>
        !string.IsNullOrWhiteSpace(Hash) &&
        !string.IsNullOrWhiteSpace(ApiUrl) &&
        (HomeId is not null || (!string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(UserToken)));

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

    /// <summary>
    /// Validates options required for cloud home metadata requests.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a required value is missing or invalid.</exception>
    public void ValidateHomeData()
    {
        if (string.IsNullOrWhiteSpace(User))
        {
            throw new ArgumentException("Roborock RRiot user is required.", nameof(User));
        }

        if (string.IsNullOrWhiteSpace(Secret))
        {
            throw new ArgumentException("Roborock RRiot secret is required.", nameof(Secret));
        }

        if (string.IsNullOrWhiteSpace(Hash))
        {
            throw new ArgumentException("Roborock RRiot hash is required.", nameof(Hash));
        }

        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out Uri? apiUri) || string.IsNullOrWhiteSpace(apiUri.Host))
        {
            throw new ArgumentException("Roborock RRiot API URL must include a scheme and host.", nameof(ApiUrl));
        }

        if (HomeId is null)
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? baseUri) || string.IsNullOrWhiteSpace(baseUri.Host))
            {
                throw new ArgumentException("Roborock account base URL must include a scheme and host when HomeId is not set.", nameof(BaseUrl));
            }

            if (string.IsNullOrWhiteSpace(UserToken))
            {
                throw new ArgumentException("Roborock account token is required when HomeId is not set.", nameof(UserToken));
            }
        }
    }

    private Uri MqttUri => Uri.TryCreate(MqttUrl, UriKind.Absolute, out Uri? uri)
        ? uri
        : throw new InvalidOperationException("Roborock RRiot MQTT URL is invalid.");

    private static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
