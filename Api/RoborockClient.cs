using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KoenZomers.RoboRock.Api.Enums;
using KoenZomers.RoboRock.Api.Models;
using KoenZomers.RoboRock.Api.Protocol;
using KoenZomers.RoboRock.Api.Utils;

namespace KoenZomers.RoboRock.Api;

/// <summary>
/// Provides local TCP access to a Roborock V1 vacuum over port 58867.
/// </summary>
/// <remarks>
/// This client uses the Roborock local key obtained from the official Roborock account data.
/// For Roborock S6 MaxV and similar V1 devices, commands are sent as DPS-wrapped local requests.
/// </remarks>
public sealed class RoborockClient : IAsyncDisposable
{
    #region Connection settings

    // Roborock V1 devices listen for encrypted local TCP traffic on this fixed port.
    private const int Port = 58867;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan HelloTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MapCommandTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PingInterval = TimeSpan.FromSeconds(25);
    private const RoborockMessageProtocol CommandProtocol = RoborockMessageProtocol.GeneralRequest;

    #endregion

    #region Session state

    private readonly string _host;
    private readonly LocalSession _session;

    // The local protocol is a single ordered TCP stream, so reads and writes must be serialized.
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly Action<string>? _trace;

    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _keepAliveCancellation;
    private Task? _keepAliveTask;
    private string _protocolVersion = "1.0";

    #endregion

    #region Construction

    /// <summary>
    /// Initializes a new local Roborock client instance.
    /// </summary>
    /// <param name="host">The vacuum IP address or DNS host name.</param>
    /// <param name="localKey">The Roborock local key for the device.</param>
    /// <param name="duid">The Roborock device identifier. It is retained for compatibility with V1 session data.</param>
    /// <param name="trace">An optional callback that receives diagnostic protocol trace messages.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host"/> or <paramref name="localKey"/> is empty.</exception>
    public RoborockClient(string host, string localKey, string? duid = null, Action<string>? trace = null)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Roborock host is required.", nameof(host));
        }

        _host = host;
        _session = new LocalSession(localKey, duid);
        _trace = trace;
    }

    #endregion

    #region Connection lifecycle

    /// <summary>
    /// Opens the TCP connection, performs a best-effort HELLO handshake, and starts keep-alive pings.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the connect operation.</param>
    /// <exception cref="TimeoutException">Thrown when the device cannot be reached before the connect timeout.</exception>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is not null)
        {
            return;
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ConnectTimeout);

        _tcpClient = new TcpClient();
        try
        {
            await _tcpClient.ConnectAsync(_host, Port, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out connecting to Roborock at {_host}:{Port} after {ConnectTimeout.TotalSeconds:n0} seconds.");
        }

        _stream = _tcpClient.GetStream();

        await SendHelloAsync(cancellationToken);
        StartKeepAlive();
    }

    #endregion

    #region Device properties

    /// <summary>
    /// Gets the current vacuum status by sending the <c>get_status</c> command.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The current vacuum status.</returns>
    /// <exception cref="InvalidDataException">Thrown when the vacuum returns an empty or invalid status result.</exception>
    /// <exception cref="TimeoutException">Thrown when no matching command response is received before the command timeout.</exception>
    public async Task<RoborockStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        JsonElement result = await SendCommandAsync("get_status", cancellationToken: cancellationToken);
        return RoborockStatus.FromJson(result);
    }

    /// <summary>
    /// Gets total cleaning history by sending the <c>get_clean_summary</c> command.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The clean summary, including total duration, area, count and record identifiers when available.</returns>
    public async Task<RoborockCleanSummary> GetCleanSummaryAsync(CancellationToken cancellationToken = default)
    {
        JsonElement result = await SendCommandAsync("get_clean_summary", cancellationToken: cancellationToken);
        return RoborockCleanSummary.FromJson(result);
    }

    /// <summary>
    /// Gets consumable usage by sending the <c>get_consumable</c> command.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>Consumable work times and calculated remaining time values.</returns>
    public async Task<RoborockConsumable> GetConsumableAsync(CancellationToken cancellationToken = default)
    {
        JsonElement result = await SendCommandAsync("get_consumable", cancellationToken: cancellationToken);
        return RoborockConsumable.FromJson(result);
    }

    /// <summary>
    /// Gets one cleaning history record by sending <c>get_clean_record</c>.
    /// </summary>
    /// <param name="recordId">The clean record identifier returned by <see cref="GetCleanSummaryAsync" />.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The requested clean record, or <see langword="null" /> when the device returns an unsupported shape.</returns>
    public async Task<RoborockCleanRecord?> GetCleanRecordAsync(long recordId, CancellationToken cancellationToken = default)
    {
        JsonElement result = await SendCommandAsync("get_clean_record", new object[] { recordId }, cancellationToken);
        return RoborockCleanRecord.FromJson(result);
    }

    /// <summary>
    /// Gets the aggregated diagnostic properties used by Home Assistant's Roborock integration.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the commands.</param>
    /// <returns>Status, cleaning totals, consumables and the newest clean record when available.</returns>
    public async Task<RoborockDeviceProperties> GetDevicePropertiesAsync(CancellationToken cancellationToken = default)
    {
        RoborockStatus status = await GetStatusAsync(cancellationToken);
        RoborockCleanSummary cleanSummary = await GetCleanSummaryAsync(cancellationToken);
        RoborockConsumable consumable = await GetConsumableAsync(cancellationToken);

        RoborockCleanRecord? lastCleanRecord = null;
        long? lastRecordId = cleanSummary.Records?.FirstOrDefault();
        if (lastRecordId is > 0)
        {
            lastCleanRecord = await GetCleanRecordAsync(lastRecordId.Value, cancellationToken);
        }

        return new RoborockDeviceProperties
        {
            Status = status,
            CleanSummary = cleanSummary,
            Consumable = consumable,
            LastCleanRecord = lastCleanRecord
        };
    }

    #endregion

    #region Cleaning commands

    /// <summary>
    /// Starts cleaning.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> StartAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("app_start", cancellationToken: cancellationToken);

    /// <summary>
    /// Pauses the current cleaning job.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> PauseAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("app_pause", cancellationToken: cancellationToken);

    /// <summary>
    /// Sends the vacuum back to the dock.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> DockAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("app_charge", cancellationToken: cancellationToken);

    /// <summary>
    /// Stops the current cleaning job.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> StopAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("app_stop", cancellationToken: cancellationToken);

    /// <summary>
    /// Plays the vacuum's locate sound.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> FindMeAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("find_me", cancellationToken: cancellationToken);

    /// <summary>
    /// Sets the fan power mode.
    /// </summary>
    /// <param name="fanPower">The Roborock fan power mode.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> SetFanPowerAsync(RoborockFanPower fanPower, CancellationToken cancellationToken = default) =>
        SetFanPowerAsync((int)fanPower, cancellationToken);

    /// <summary>
    /// Sets the fan power mode.
    /// </summary>
    /// <param name="fanPower">The raw Roborock fan power value, for example <c>102</c>.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> SetFanPowerAsync(int fanPower, CancellationToken cancellationToken = default) =>
        SendCommandAsync("set_custom_mode", fanPower, cancellationToken);

    /// <summary>
    /// Gets the multi-map list known by the vacuum.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock multi-map list result.</returns>
    public Task<JsonElement> GetMultiMapsListAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("get_multi_maps_list", cancellationToken: cancellationToken);

    /// <summary>
    /// Gets typed multi-map metadata known by the vacuum.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>Map flags and their friendly map names.</returns>
    public async Task<IReadOnlyList<RoborockMapInfo>> GetMultiMapsAsync(CancellationToken cancellationToken = default)
    {
        JsonElement result = await GetMultiMapsListAsync(cancellationToken);
        return RoborockMapInfo.FromJson(result);
    }

    /// <summary>
    /// Gets the room mapping for the currently loaded map.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock room mapping result.</returns>
    public Task<JsonElement> GetRoomMappingAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("get_room_mapping", cancellationToken: cancellationToken);

    /// <summary>
    /// Gets typed room mappings for the currently loaded map.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>Room segment identifiers mapped to Roborock IoT room identifiers.</returns>
    public async Task<IReadOnlyList<RoborockRoomMapping>> GetRoomMappingsAsync(CancellationToken cancellationToken = default)
    {
        JsonElement result = await GetRoomMappingAsync(cancellationToken);
        return RoborockRoomMapping.FromJson(result);
    }

    #endregion

    #region Maps

    /// <summary>
    /// Loads a multi-map by its Roborock map flag.
    /// </summary>
    /// <param name="mapFlag">The map flag returned by <see cref="GetMultiMapsListAsync"/>.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> LoadMultiMapAsync(int mapFlag, CancellationToken cancellationToken = default) =>
        SendCommandAsync("load_multi_map", new object[] { mapFlag }, cancellationToken);

    /// <summary>
    /// Gets the current raw Roborock map payload through the V1 map channel.
    /// </summary>
    /// <param name="mapSecurityKey">The Roborock RRiot <c>k</c> value used to decrypt map responses.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The decrypted and decompressed Roborock map payload.</returns>
    /// <remarks>
    /// The returned data is not a PNG. It is Roborock's proprietary map format and still needs a renderer/parser.
    /// </remarks>
    public async Task<RoborockMapData> GetRawMapDataAsync(string mapSecurityKey, CancellationToken cancellationToken = default)
    {
        RoborockMapSecurity security = RoborockMapSecurity.Create(mapSecurityKey);
        byte[] content = await SendMapCommandAsync("get_map_v1", security, cancellationToken);
        return new RoborockMapData(content);
    }

    /// <summary>
    /// Gets the current Roborock map rendered as a directly usable PNG image.
    /// </summary>
    /// <param name="mapSecurityKey">The Roborock RRiot <c>k</c> value used to decrypt map responses.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The rendered PNG map image.</returns>
    /// <exception cref="InvalidDataException">Thrown when the map payload is not a valid RRMap image.</exception>
    public async Task<RoborockMapImage> GetMapImageAsync(string mapSecurityKey, CancellationToken cancellationToken = default)
    {
        RoborockMapData mapData = await GetRawMapDataAsync(mapSecurityKey, cancellationToken);
        return mapData.ToImage();
    }

    /// <summary>
    /// Gets the current Roborock map rendered as PNG together with the matching multi-map metadata.
    /// </summary>
    /// <param name="mapSecurityKey">The Roborock RRiot <c>k</c> value used to decrypt map responses.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The rendered PNG map image and metadata for the currently selected map.</returns>
    public async Task<RoborockMapImageWithMetadata> GetMapImageWithMetadataAsync(string mapSecurityKey, CancellationToken cancellationToken = default)
    {
        Task<RoborockStatus> statusTask = GetStatusAsync(cancellationToken);
        Task<IReadOnlyList<RoborockMapInfo>> mapsTask = GetMultiMapsAsync(cancellationToken);
        Task<RoborockMapImage> imageTask = GetMapImageAsync(mapSecurityKey, cancellationToken);

        await Task.WhenAll(statusTask, mapsTask, imageTask);
        return RoborockMapImageWithMetadata.Create(imageTask.Result, statusTask.Result, mapsTask.Result);
    }

    #endregion

    #region Camera commands

    /// <summary>
    /// Gets the camera status for devices with a built-in camera.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock camera status result.</returns>
    public Task<JsonElement> GetCameraStatusAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("get_camera_status", cancellationToken: cancellationToken);

    /// <summary>
    /// Enables or disables the camera feature on devices that support it.
    /// </summary>
    /// <param name="enabled">Whether the camera feature should be enabled.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> SetCameraStatusAsync(bool enabled, CancellationToken cancellationToken = default) =>
        SendCommandAsync("set_camera_status", enabled ? 1 : 0, cancellationToken);

    /// <summary>
    /// Starts the camera preview handshake for devices with a built-in camera.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> StartCameraPreviewAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("start_camera_preview", cancellationToken: cancellationToken);

    /// <summary>
    /// Stops the camera preview on devices with a built-in camera.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> StopCameraPreviewAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("stop_camera_preview", cancellationToken: cancellationToken);

    /// <summary>
    /// Gets TURN server information used by the Roborock camera WebRTC flow.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock TURN server result.</returns>
    public Task<JsonElement> GetTurnServerAsync(CancellationToken cancellationToken = default) =>
        SendCommandAsync("get_turn_server", cancellationToken: cancellationToken);

    /// <summary>
    /// Exchanges a WebRTC SDP payload with the robot camera flow.
    /// </summary>
    /// <param name="sdp">The SDP payload produced by the WebRTC peer or go2rtc.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result, typically containing the robot SDP answer.</returns>
    public Task<JsonElement> GetDeviceSdpAsync(object sdp, CancellationToken cancellationToken = default) =>
        SendCommandAsync("get_device_sdp", sdp, cancellationToken);

    /// <summary>
    /// Sends an ICE candidate payload to the robot camera flow.
    /// </summary>
    /// <param name="iceCandidate">The ICE candidate payload produced by the WebRTC peer or go2rtc.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The raw Roborock command result.</returns>
    public Task<JsonElement> SendIceToRobotAsync(object iceCandidate, CancellationToken cancellationToken = default) =>
        SendCommandAsync("send_ice_to_robot", iceCandidate, cancellationToken);

    #endregion

    #region Raw commands

    /// <summary>
    /// Sends a raw Roborock RPC command over the local connection.
    /// </summary>
    /// <param name="method">The Roborock RPC method name, such as <c>get_status</c>.</param>
    /// <param name="parameters">The command parameters. Use <see langword="null"/> for an empty parameter array.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The unwrapped Roborock RPC result.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="method"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Roborock returns an RPC error.</exception>
    /// <exception cref="TimeoutException">Thrown when no matching command response is received before the command timeout.</exception>
    public async Task<JsonElement> SendCommandAsync(
        string method,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("Roborock RPC method is required.", nameof(method));
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CommandTimeout);

        await _ioLock.WaitAsync(timeout.Token);
        try
        {
            NetworkStream stream = GetStream();
            uint timestamp = UnixTimestamp();
            int requestId = RandomNumberGenerator.GetInt32(10000, 32768);
            uint sequence = _session.NextSequence();

            var request = new RoborockRpcRequest
            {
                Id = requestId,
                Method = method,
                Params = NormalizeParameters(parameters)
            };

            // V1 local commands wrap the RPC request JSON inside DPS key 101.
            string innerJson = JsonSerializer.Serialize(request, RoborockJson.CompactOptions);
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                new RoborockDpsPayload(new Dictionary<string, string> { ["101"] = innerJson }, timestamp),
                RoborockJson.CompactOptions);
            var message = new RoborockMessage
            {
                Version = _protocolVersion,
                Sequence = sequence,
                Random = 12345,
                Timestamp = timestamp,
                Protocol = CommandProtocol,
                Payload = payload
            };

            await WriteMessageAsync(stream, message, timeout.Token);

            while (true)
            {
                RoborockMessage response = await ReadMessageAsync(timeout.Token);

                if (TryReadRpcResponse(response, requestId, out JsonElement result) ||
                    TryReadDpsResponse(response, requestId, out result))
                {
                    return result;
                }

                if (response.Protocol == RoborockMessageProtocol.PingRequest)
                {
                    await WriteMessageAsync(stream, CreateControlMessage(RoborockMessageProtocol.PingResponse), timeout.Token);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for Roborock response to '{method}' after {CommandTimeout.TotalSeconds:n0} seconds.");
        }
        finally
        {
            _ioLock.Release();
        }
    }

    #endregion

    #region Map protocol

    /// <summary>
    /// Sends a Roborock map command and returns decrypted map payload bytes.
    /// </summary>
    /// <param name="method">The Roborock map command method.</param>
    /// <param name="security">The map security material for the request.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The decrypted and decompressed map payload bytes.</returns>
    private async Task<byte[]> SendMapCommandAsync(
        string method,
        RoborockMapSecurity security,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MapCommandTimeout);

        await _ioLock.WaitAsync(timeout.Token);
        try
        {
            NetworkStream stream = GetStream();
            uint timestamp = UnixTimestamp();
            int requestId = RandomNumberGenerator.GetInt32(10000, 32768);
            uint sequence = _session.NextSequence();

            var request = new
            {
                id = requestId,
                method,
                @params = Array.Empty<object>(),
                security = new
                {
                    endpoint = security.Endpoint,
                    nonce = Convert.ToHexString(security.Nonce).ToLowerInvariant()
                }
            };

            // Map requests include RRiot security material and return encrypted gzip data on a separate protocol channel.
            string innerJson = JsonSerializer.Serialize(request, RoborockJson.CompactOptions);
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
                new RoborockDpsPayload(new Dictionary<string, string> { ["101"] = innerJson }, timestamp),
                RoborockJson.CompactOptions);
            var message = new RoborockMessage
            {
                Version = _protocolVersion,
                Sequence = sequence,
                Random = 12345,
                Timestamp = timestamp,
                Protocol = CommandProtocol,
                Payload = payload
            };

            await WriteMessageAsync(stream, message, timeout.Token);

            while (true)
            {
                RoborockMessage response = await ReadMessageAsync(timeout.Token);

                if (TryDecodeMapResponse(response, requestId, security, out byte[] mapData))
                {
                    return mapData;
                }

                if (TryReadRpcResponse(response, requestId, out JsonElement _) ||
                    TryReadDpsResponse(response, requestId, out _))
                {
                    continue;
                }

                if (response.Protocol == RoborockMessageProtocol.PingRequest)
                {
                    await WriteMessageAsync(stream, CreateControlMessage(RoborockMessageProtocol.PingResponse), timeout.Token);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for Roborock map response to '{method}' after {MapCommandTimeout.TotalSeconds:n0} seconds.");
        }
        finally
        {
            _ioLock.Release();
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Stops keep-alive pings and closes the TCP connection.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_keepAliveCancellation is not null)
        {
            await _keepAliveCancellation.CancelAsync();
        }

        if (_keepAliveTask is not null)
        {
            try
            {
                await _keepAliveTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
        }

        _keepAliveCancellation?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _ioLock.Dispose();
    }

    #endregion

    #region Connection helpers

    /// <summary>
    /// Sends the best-effort Roborock HELLO handshake.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the handshake.</param>
    private async Task SendHelloAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(HelloTimeout);

        await _ioLock.WaitAsync(timeout.Token);
        try
        {
            NetworkStream stream = GetStream();
            await WriteMessageAsync(stream, CreateControlMessage(RoborockMessageProtocol.HelloRequest, sequence: 1, random: 22), timeout.Token);

            try
            {
                await ReadMessageAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // python-roborock treats hello as best-effort; some local devices do not answer consistently.
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Starts the background keep-alive ping loop when it is not already running.
    /// </summary>
    private void StartKeepAlive()
    {
        if (_keepAliveTask is not null)
        {
            return;
        }

        _keepAliveCancellation = new CancellationTokenSource();
        _keepAliveTask = Task.Run(() => KeepAliveAsync(_keepAliveCancellation.Token));
    }

    /// <summary>
    /// Periodically sends keep-alive pings while the client is connected.
    /// </summary>
    /// <param name="cancellationToken">A token that stops the loop.</param>
    private async Task KeepAliveAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(PingInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await PingAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Keep-alive is opportunistic. The next command will surface connection failures with context.
            }
        }
    }

    /// <summary>
    /// Sends one ping request and waits for a ping response.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the ping.</param>
    private async Task PingAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PingTimeout);

        await _ioLock.WaitAsync(timeout.Token);
        try
        {
            NetworkStream stream = GetStream();
            await WriteMessageAsync(stream, CreateControlMessage(RoborockMessageProtocol.PingRequest), timeout.Token);

            while (true)
            {
                RoborockMessage response = await ReadMessageAsync(timeout.Token);
                if (response.Protocol == RoborockMessageProtocol.PingResponse)
                {
                    return;
                }
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    #endregion

    #region Message transport

    /// <summary>
    /// Encodes and writes a Roborock message to the network stream.
    /// </summary>
    /// <param name="stream">The connected network stream.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">A token that can cancel the write.</param>
    private async Task WriteMessageAsync(NetworkStream stream, RoborockMessage message, CancellationToken cancellationToken)
    {
        byte[] packet = RoborockMessageEncoder.Encode(message, _session.LocalKey);
        Trace($"TX protocol={message.Protocol} seq={message.Sequence} random={message.Random} ts={message.Timestamp} payload={message.Payload?.Length ?? 0} packet={packet.Length}");
        Trace($"TX hex={Convert.ToHexString(packet).ToLowerInvariant()}");
        await stream.WriteAsync(packet, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a Roborock control message for HELLO and ping traffic.
    /// </summary>
    /// <param name="protocol">The control protocol type.</param>
    /// <param name="sequence">The optional sequence number to use.</param>
    /// <param name="random">The optional random value to use.</param>
    /// <returns>The created control message.</returns>
    private RoborockMessage CreateControlMessage(
        RoborockMessageProtocol protocol,
        uint? sequence = null,
        uint? random = null) =>
        new()
        {
            Version = _protocolVersion,
            Sequence = sequence ?? _session.NextSequence(),
            Random = random ?? RandomUInt32(),
            Timestamp = UnixTimestamp(),
            Protocol = protocol
        };

    /// <summary>
    /// Reads and decodes one Roborock message from the network stream.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the read.</param>
    /// <returns>The decoded Roborock message.</returns>
    private async Task<RoborockMessage> ReadMessageAsync(CancellationToken cancellationToken)
    {
        NetworkStream stream = GetStream();
        byte[] prefix = await ReadExactlyAsync(stream, 4, cancellationToken);
        uint length = BinaryPrimitives.ReadUInt32BigEndian(prefix);
        if (length < 17 || length > 10 * 1024 * 1024)
        {
            throw new InvalidDataException($"Invalid Roborock packet length: {length}.");
        }

        byte[] body = await ReadExactlyAsync(stream, (int)length, cancellationToken);
        byte[] packet = new byte[prefix.Length + body.Length];
        prefix.CopyTo(packet, 0);
        body.CopyTo(packet, prefix.Length);
        RoborockMessage message = RoborockMessageEncoder.Decode(packet, _session.LocalKey);
        Trace($"RX protocol={message.Protocol} seq={message.Sequence} random={message.Random} ts={message.Timestamp} payload={message.Payload?.Length ?? 0}");
        if (message.Payload is { Length: > 0 })
        {
            Trace($"RX payload={Encoding.UTF8.GetString(message.Payload)}");
        }

        return message;
    }

    #endregion

    #region Response parsing

    /// <summary>
    /// Attempts to read a direct RPC response from a Roborock message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <param name="requestId">The request identifier to match.</param>
    /// <param name="result">The decoded result when a matching response is found.</param>
    /// <returns><see langword="true" /> when a matching RPC response was decoded; otherwise, <see langword="false" />.</returns>
    private static bool TryReadRpcResponse(RoborockMessage message, int requestId, out JsonElement result)
    {
        result = default;
        if (message.Payload is not { Length: > 0 } || message.Protocol != RoborockMessageProtocol.RpcResponse)
        {
            return false;
        }

        try
        {
            using JsonDocument responseDocument = JsonDocument.Parse(message.Payload);
            return TryReadRpcResult(responseDocument.RootElement, requestId, out result);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to read a DPS-wrapped RPC response from a Roborock message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <param name="requestId">The request identifier to match.</param>
    /// <param name="result">The decoded result when a matching response is found.</param>
    /// <returns><see langword="true" /> when a matching DPS response was decoded; otherwise, <see langword="false" />.</returns>
    private static bool TryReadDpsResponse(RoborockMessage message, int requestId, out JsonElement result)
    {
        result = default;
        if (message.Payload is not { Length: > 0 })
        {
            return false;
        }

        try
        {
            using JsonDocument payloadDocument = JsonDocument.Parse(message.Payload);
            if (!payloadDocument.RootElement.TryGetProperty("dps", out JsonElement dps))
            {
                return false;
            }

            if (dps.TryGetProperty("102", out JsonElement dps102) && dps102.ValueKind == JsonValueKind.String)
            {
                using JsonDocument responseDocument = JsonDocument.Parse(dps102.GetString() ?? "{}");
                return TryReadRpcResult(responseDocument.RootElement, requestId, out result);
            }

            foreach (JsonProperty property in dps.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                using JsonDocument responseDocument = JsonDocument.Parse(property.Value.GetString() ?? "{}");
                if (TryReadRpcResult(responseDocument.RootElement, requestId, out result))
                {
                    return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to decode a Roborock map response message.
    /// </summary>
    /// <param name="message">The message to inspect.</param>
    /// <param name="requestId">The request identifier to match.</param>
    /// <param name="security">The map security material used to validate and decrypt the response.</param>
    /// <param name="mapData">The decrypted map data when decoding succeeds.</param>
    /// <returns><see langword="true" /> when the map response was decoded; otherwise, <see langword="false" />.</returns>
    private static bool TryDecodeMapResponse(
        RoborockMessage message,
        int requestId,
        RoborockMapSecurity security,
        out byte[] mapData)
    {
        mapData = Array.Empty<byte>();
        if (message.Protocol != RoborockMessageProtocol.MapResponse || message.Payload is not { Length: >= 24 })
        {
            return false;
        }

        string endpoint = Encoding.ASCII.GetString(message.Payload.AsSpan(0, 8)).TrimEnd('\0');
        if (!endpoint.StartsWith(security.Endpoint, StringComparison.Ordinal))
        {
            return false;
        }

        ushort responseRequestId = BinaryPrimitives.ReadUInt16LittleEndian(message.Payload.AsSpan(16, 2));
        if (responseRequestId != requestId)
        {
            return false;
        }

        byte[] encryptedMapData = message.Payload[24..];
        byte[] decryptedMapData = RoborockCrypto.DecryptCbc(encryptedMapData, security.Nonce);
        mapData = RoborockCrypto.DecompressGzip(decryptedMapData);
        return true;
    }

    /// <summary>
    /// Attempts to read the matching result value from a Roborock RPC response JSON object.
    /// </summary>
    /// <param name="response">The RPC response JSON object.</param>
    /// <param name="requestId">The request identifier to match.</param>
    /// <param name="result">The cloned result value when present.</param>
    /// <returns><see langword="true" /> when a matching result or error was found; otherwise, <see langword="false" />.</returns>
    private static bool TryReadRpcResult(JsonElement response, int requestId, out JsonElement result)
    {
        result = default;
        if (!response.TryGetProperty("id", out JsonElement idElement) || idElement.GetInt32() != requestId)
        {
            return false;
        }

        if (response.TryGetProperty("error", out JsonElement error) && error.ValueKind != JsonValueKind.Null)
        {
            int code = error.TryGetProperty("code", out JsonElement codeElement) ? codeElement.GetInt32() : 0;
            string messageText = error.TryGetProperty("message", out JsonElement messageElement)
                ? messageElement.GetString() ?? "Unknown Roborock error"
                : "Unknown Roborock error";
            throw new InvalidOperationException($"Roborock command failed ({code}): {messageText}");
        }

        if (!response.TryGetProperty("result", out JsonElement resultElement))
        {
            result = JsonSerializer.Deserialize<JsonElement>("null");
            return true;
        }

        if (resultElement.ValueKind == JsonValueKind.Array && resultElement.GetArrayLength() == 1)
        {
            result = resultElement[0].Clone();
            return true;
        }

        result = resultElement.Clone();
        return true;
    }

    /// <summary>
    /// Normalizes null Roborock RPC parameters to an empty parameter array.
    /// </summary>
    /// <param name="parameters">The parameter value to normalize.</param>
    /// <returns>The normalized parameter value.</returns>
    private static object NormalizeParameters(object? parameters) =>
        parameters switch
        {
            null => Array.Empty<object>(),
            JsonElement { ValueKind: JsonValueKind.Undefined or JsonValueKind.Null } => Array.Empty<object>(),
            _ => parameters
        };

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the active network stream or throws when the client is disconnected.
    /// </summary>
    /// <returns>The active network stream.</returns>
    private NetworkStream GetStream() =>
        _stream ?? throw new InvalidOperationException("Not connected. Call ConnectAsync first.");

    /// <summary>
    /// Reads the requested number of bytes from a network stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="cancellationToken">A token that can cancel the read.</param>
    /// <returns>The bytes read from the stream.</returns>
    private static async Task<byte[]> ReadExactlyAsync(
        NetworkStream stream,
        int length,
        CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                throw new IOException("Roborock closed the TCP connection.");
            }

            offset += read;
        }

        return buffer;
    }

    /// <summary>
    /// Generates a cryptographically random unsigned 32-bit value.
    /// </summary>
    /// <returns>The generated random value.</returns>
    private static uint RandomUInt32()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    /// <summary>
    /// Gets the current Unix timestamp in seconds.
    /// </summary>
    /// <returns>The current Unix timestamp.</returns>
    private static uint UnixTimestamp() => (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Emits a trace message through the configured trace callback.
    /// </summary>
    /// <param name="message">The trace message to emit.</param>
    private void Trace(string message)
    {
        _trace?.Invoke(message);
    }

    #endregion
}

