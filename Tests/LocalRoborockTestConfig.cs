using System.Text.Json;
using System.Text.Json.Serialization;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Contains local Roborock integration test settings.
/// </summary>
internal sealed class LocalRoborockTestConfig
{
    public const string FileName = "roborock.json";
    public const string LocalFileName = "roborock.local.json";

    [JsonPropertyName("duid")]
    public string Duid { get; init; } = "";

    [JsonPropertyName("localKey")]
    public string LocalKey { get; init; } = "";

    [JsonPropertyName("mapSecurityKey")]
    public string? MapSecurityKey { get; init; }

    [JsonPropertyName("rriotUser")]
    public string? RriotUser { get; init; }

    [JsonPropertyName("rriotSecret")]
    public string? RriotSecret { get; init; }

    [JsonPropertyName("rriotKey")]
    public string? RriotKey { get; init; }

    [JsonPropertyName("mqttUrl")]
    public string? MqttUrl { get; init; }

    [JsonPropertyName("rriotHash")]
    public string? RriotHash { get; init; }

    [JsonPropertyName("apiUrl")]
    public string? ApiUrl { get; init; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; init; }

    [JsonPropertyName("userToken")]
    public string? UserToken { get; init; }

    [JsonPropertyName("homeId")]
    public long? HomeId { get; init; }

    [JsonPropertyName("model")]
    public string Model { get; init; } = "";

    [JsonPropertyName("host")]
    public string Host { get; init; } = "";

    [JsonPropertyName("port")]
    public int Port { get; init; }

    public static bool RunIntegrationTests =>
        string.Equals(
            Environment.GetEnvironmentVariable("ROBOROCK_RUN_INTEGRATION_TESTS"),
            "1",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loads and validates local Roborock integration test configuration.
    /// </summary>
    public static LocalRoborockTestConfig Load()
    {
        string directory = FindConfigDirectory() ?? throw new FileNotFoundException(
            $"Roborock test config '{FileName}' was not found. Create it in the Tests folder.");

        LocalRoborockTestConfig config = LoadFile(Path.Combine(directory, FileName));
        string localPath = Path.Combine(directory, LocalFileName);
        if (File.Exists(localPath))
        {
            config = Merge(config, LoadFile(localPath));
        }

        config.Validate();
        return config;
    }

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static LocalRoborockTestConfig LoadFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<LocalRoborockTestConfig>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Roborock test config '{path}' is empty or invalid.");
    }

    private static LocalRoborockTestConfig Merge(LocalRoborockTestConfig defaults, LocalRoborockTestConfig local) =>
        new()
        {
            Duid = UseLocalValue(local.Duid, defaults.Duid),
            LocalKey = UseLocalValue(local.LocalKey, defaults.LocalKey),
            MapSecurityKey = UseLocalValue(local.MapSecurityKey, defaults.MapSecurityKey),
            RriotUser = UseLocalValue(local.RriotUser, defaults.RriotUser),
            RriotSecret = UseLocalValue(local.RriotSecret, defaults.RriotSecret),
            RriotKey = UseLocalValue(local.RriotKey, defaults.RriotKey),
            MqttUrl = UseLocalValue(local.MqttUrl, defaults.MqttUrl),
            RriotHash = UseLocalValue(local.RriotHash, defaults.RriotHash),
            ApiUrl = UseLocalValue(local.ApiUrl, defaults.ApiUrl),
            BaseUrl = UseLocalValue(local.BaseUrl, defaults.BaseUrl),
            UserToken = UseLocalValue(local.UserToken, defaults.UserToken),
            HomeId = local.HomeId ?? defaults.HomeId,
            Model = UseLocalValue(local.Model, defaults.Model),
            Host = UseLocalValue(local.Host, defaults.Host),
            Port = local.Port > 0 ? local.Port : defaults.Port
        };

    public bool HasCloudMapConfig =>
        !string.IsNullOrWhiteSpace(RriotUser) &&
        !string.IsNullOrWhiteSpace(RriotSecret) &&
        !string.IsNullOrWhiteSpace(RriotKey ?? MapSecurityKey) &&
        !string.IsNullOrWhiteSpace(MqttUrl);

    public bool HasCloudRoomConfig =>
        HasCloudMapConfig &&
        !string.IsNullOrWhiteSpace(RriotHash) &&
        !string.IsNullOrWhiteSpace(ApiUrl) &&
        (HomeId is not null || (!string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(UserToken)));

    public RoborockCloudConnectionOptions ToCloudConnectionOptions() =>
        new()
        {
            Duid = Duid,
            LocalKey = LocalKey,
            User = RriotUser ?? string.Empty,
            Secret = RriotSecret ?? string.Empty,
            Key = RriotKey ?? MapSecurityKey ?? string.Empty,
            MqttUrl = MqttUrl ?? string.Empty,
            Hash = RriotHash,
            ApiUrl = ApiUrl,
            BaseUrl = BaseUrl,
            UserToken = UserToken,
            HomeId = HomeId
        };

    private static string UseLocalValue(string? localValue, string? defaultValue) =>
        string.IsNullOrWhiteSpace(localValue) ? defaultValue ?? "" : localValue;

    /// <summary>
    /// Finds the Roborock integration test configuration directory.
    /// </summary>
    private static string? FindConfigDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, FileName)))
            {
                return directory.FullName;
            }

            string testsCandidate = Path.Combine(directory.FullName, "Tests");
            if (File.Exists(Path.Combine(testsCandidate, FileName)))
            {
                return testsCandidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Validates required local Roborock integration test configuration values.
    /// </summary>
    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Duid))
        {
            throw new InvalidDataException($"Config value 'duid' is required. Set it in {LocalFileName}.");
        }

        if (string.IsNullOrWhiteSpace(LocalKey))
        {
            throw new InvalidDataException($"Config value 'localKey' is required. Set it in {LocalFileName}.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidDataException($"Config value 'model' is required. Set it in {LocalFileName}.");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidDataException($"Config value 'host' is required. Set it in {LocalFileName}.");
        }

        if (Port <= 0)
        {
            throw new InvalidDataException($"Config value 'port' must be greater than zero. Set it in {LocalFileName}.");
        }
    }
}
