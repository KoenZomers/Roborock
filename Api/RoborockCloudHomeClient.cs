using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KoenZomers.RoboRock.Api.Models;
using KoenZomers.RoboRock.Api.Utils;

namespace KoenZomers.RoboRock.Api;

/// <summary>
/// Retrieves Roborock cloud home metadata such as friendly room names.
/// </summary>
public sealed class RoborockCloudHomeClient : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly RoborockCloudConnectionOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly string _apiUrl;
    private readonly string? _baseUrl;
    private readonly string _hash;
    private readonly string? _userToken;

    /// <summary>
    /// Initializes a new Roborock cloud home client.
    /// </summary>
    /// <param name="options">The Roborock cloud options.</param>
    /// <param name="httpClient">An optional HTTP client to use for requests.</param>
    public RoborockCloudHomeClient(RoborockCloudConnectionOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.ValidateHomeData();
        _apiUrl = _options.ApiUrl ?? throw new InvalidOperationException("Roborock RRiot API URL is required.");
        _baseUrl = _options.BaseUrl;
        _hash = _options.Hash ?? throw new InvalidOperationException("Roborock RRiot hash is required.");
        _userToken = _options.UserToken;
        _httpClient = httpClient ?? new HttpClient { Timeout = RequestTimeout };
        _disposeHttpClient = httpClient is null;
    }

    /// <summary>
    /// Gets the friendly rooms configured for the Roborock home.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The configured Roborock cloud rooms.</returns>
    public async Task<IReadOnlyList<RoborockRoomInfo>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        long homeId = await GetHomeIdAsync(cancellationToken);
        string path = $"/user/homes/{homeId.ToString(CultureInfo.InvariantCulture)}/rooms";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_apiUrl), path));
        request.Headers.TryAddWithoutValidation("Authorization", CreateHawkAuthentication(path));

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        if (root.TryGetProperty("success", out JsonElement success) && success.ValueKind == JsonValueKind.False)
        {
            throw new InvalidOperationException($"Roborock cloud rooms request failed: {content}");
        }

        return RoborockRoomInfo.FromJson(root);
    }

    /// <summary>
    /// Gets the Roborock home id used by cloud home APIs.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The Roborock home id.</returns>
    public async Task<long> GetHomeIdAsync(CancellationToken cancellationToken = default)
    {
        if (_options.HomeId is { } configuredHomeId)
        {
            return configuredHomeId;
        }

        string path = "/api/v1/getHomeDetail";
        string baseUrl = _baseUrl ?? throw new InvalidOperationException("Roborock account base URL is required when HomeId is not set.");
        string userToken = _userToken ?? throw new InvalidOperationException("Roborock account token is required when HomeId is not set.");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), path));
        request.Headers.TryAddWithoutValidation("Authorization", userToken);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement root = document.RootElement;
        int? code = root.TryGetProperty("code", out JsonElement codeElement) ? codeElement.GetInt32OrNull() : null;
        if (code is not null && code != 200)
        {
            throw new InvalidOperationException($"Roborock getHomeDetail request failed: {content}");
        }

        if (!root.TryGetProperty("data", out JsonElement data) || !data.TryGetProperty("rrHomeId", out JsonElement homeId))
        {
            throw new InvalidDataException($"Roborock getHomeDetail response did not contain rrHomeId: {content}");
        }

        if (homeId.ValueKind == JsonValueKind.Number && homeId.TryGetInt64(out long numericHomeId))
        {
            return numericHomeId;
        }

        if (homeId.ValueKind == JsonValueKind.String && long.TryParse(homeId.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long stringHomeId))
        {
            return stringHomeId;
        }

        throw new InvalidDataException($"Roborock getHomeDetail response contained an invalid rrHomeId: {content}");
    }

    /// <summary>
    /// Releases resources held by this client when it owns its HTTP client.
    /// </summary>
    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private string CreateHawkAuthentication(string path)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nonce = CreateNonce();
        string payload = string.Join(':', _options.User, _options.Secret, nonce, timestamp.ToString(CultureInfo.InvariantCulture), Md5Hex(path), string.Empty, string.Empty);
        string mac = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_hash), Encoding.UTF8.GetBytes(payload)));
        return $"Hawk id=\"{_options.User}\",s=\"{_options.Secret}\",ts=\"{timestamp.ToString(CultureInfo.InvariantCulture)}\",nonce=\"{nonce}\",mac=\"{mac}\"";
    }

    private static string CreateNonce() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(9))
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');

    private static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
