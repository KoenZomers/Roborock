using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests;

/// <summary>
/// Contains local Roborock integration test settings.
/// </summary>
internal sealed class LocalRoborockTestConfig
{
    public const string FileName = "roborock.local.json";

    [JsonPropertyName("duid")]
    public string Duid { get; init; } = "";

    [JsonPropertyName("localKey")]
    public string LocalKey { get; init; } = "";

    [JsonPropertyName("mapSecurityKey")]
    public string? MapSecurityKey { get; init; }

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
        string path = FindConfigFile() ?? throw new FileNotFoundException(
            $"Local Roborock test config '{FileName}' was not found. Create it in the Library.Tests folder.");

        using FileStream stream = File.OpenRead(path);
        LocalRoborockTestConfig config = JsonSerializer.Deserialize<LocalRoborockTestConfig>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Local Roborock test config '{path}' is empty or invalid.");

        config.Validate();
        return config;
    }

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Finds the local Roborock integration test configuration file.
    /// </summary>
    private static string? FindConfigFile()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            string projectCandidate = Path.Combine(directory.FullName, "Library.Tests", FileName);
            if (File.Exists(projectCandidate))
            {
                return projectCandidate;
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
            throw new InvalidDataException("Config value 'duid' is required.");
        }

        if (string.IsNullOrWhiteSpace(LocalKey))
        {
            throw new InvalidDataException("Config value 'localKey' is required.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidDataException("Config value 'model' is required.");
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new InvalidDataException("Config value 'host' is required.");
        }

        if (Port <= 0)
        {
            throw new InvalidDataException("Config value 'port' must be greater than zero.");
        }
    }
}
