using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KoenZomers.RoboRock.Api.Models;
using KoenZomers.RoboRock.Api.Protocol;
using KoenZomers.RoboRock.Api.Utils;
using MQTTnet;

namespace KoenZomers.RoboRock.Api;

/// <summary>
/// Retrieves Roborock map content through the cloud MQTT map RPC channel used by Home Assistant/python-roborock.
/// </summary>
public sealed class RoborockCloudMapClient
{
    private static readonly TimeSpan MapCommandTimeout = TimeSpan.FromSeconds(60);

    private readonly RoborockCloudConnectionOptions _options;
    private readonly Action<string>? _trace;

    /// <summary>
    /// Initializes a new Roborock cloud map client.
    /// </summary>
    /// <param name="options">The Roborock cloud MQTT and RRiot options.</param>
    /// <param name="trace">An optional callback that receives diagnostic trace messages.</param>
    public RoborockCloudMapClient(RoborockCloudConnectionOptions options, Action<string>? trace = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _trace = trace;
    }

    /// <summary>
    /// Gets the current raw Roborock map payload through the cloud MQTT map RPC channel.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The decrypted and decompressed Roborock map payload.</returns>
    public async Task<RoborockMapData> GetRawMapDataAsync(CancellationToken cancellationToken = default)
    {
        RoborockMapSecurity security = RoborockMapSecurity.Create(_options.Key);
        byte[] content = await SendMapCommandAsync("get_map_v1", security, cancellationToken);
        return new RoborockMapData(content);
    }

    /// <summary>
    /// Gets the current Roborock map rendered as a directly usable PNG image through the cloud MQTT map RPC channel.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The rendered PNG map image.</returns>
    public async Task<RoborockMapImage> GetMapImageAsync(CancellationToken cancellationToken = default)
    {
        RoborockMapData mapData = await GetRawMapDataAsync(cancellationToken);
        return mapData.ToImage();
    }

    /// <summary>
    /// Gets the current Roborock map rendered as PNG through the cloud MQTT map channel together with local multi-map metadata.
    /// </summary>
    /// <param name="metadataClient">A connected local Roborock client used to retrieve <c>get_status</c> and <c>get_multi_maps_list</c>.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The rendered PNG map image and metadata for the currently selected map.</returns>
    public async Task<RoborockMapImageWithMetadata> GetMapImageWithMetadataAsync(RoborockClient metadataClient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadataClient);

        Task<RoborockMapData> mapDataTask = GetRawMapDataAsync(cancellationToken);
        Task<RoborockStatus> statusTask = metadataClient.GetStatusAsync(cancellationToken);
        Task<IReadOnlyList<RoborockMapInfo>> mapsTask = metadataClient.GetMultiMapsAsync(cancellationToken);
        Task<IReadOnlyList<RoborockRoomMapping>> roomMappingsTask = metadataClient.GetRoomMappingsAsync(cancellationToken);

        await Task.WhenAll(mapDataTask, statusTask, mapsTask, roomMappingsTask);
        RoborockMapData mapData = mapDataTask.Result;
        return RoborockMapImageWithMetadata.Create(
            mapData.ToImage(),
            statusTask.Result,
            mapsTask.Result,
            mapData.GetCurrentRoom(roomMappingsTask.Result));
    }

    /// <summary>
    /// Gets the room segment currently containing the vacuum through the cloud MQTT map channel.
    /// </summary>
    /// <param name="metadataClient">A connected local Roborock client used to retrieve <c>get_room_mapping</c>.</param>
    /// <param name="cancellationToken">A token that can cancel the command.</param>
    /// <returns>The current room, or <see langword="null" /> when the map does not contain a resolvable room.</returns>
    public async Task<RoborockCurrentRoom?> GetCurrentRoomAsync(RoborockClient metadataClient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadataClient);

        Task<RoborockMapData> mapDataTask = GetRawMapDataAsync(cancellationToken);
        Task<IReadOnlyList<RoborockRoomMapping>> roomMappingsTask = metadataClient.GetRoomMappingsAsync(cancellationToken);

        await Task.WhenAll(mapDataTask, roomMappingsTask);
        return mapDataTask.Result.GetCurrentRoom(roomMappingsTask.Result);
    }

    private async Task<byte[]> SendMapCommandAsync(
        string method,
        RoborockMapSecurity security,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(MapCommandTimeout);

        int requestId = RandomNumberGenerator.GetInt32(10000, 32768);
        var completion = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        var mqttFactory = new MqttClientFactory();
        using IMqttClient mqttClient = mqttFactory.CreateMqttClient();
        mqttClient.ApplicationMessageReceivedAsync += args =>
        {
            TryHandleMessage(ToArray(args.ApplicationMessage.Payload), requestId, security, completion);
            return Task.CompletedTask;
        };

        MqttClientOptions mqttOptions = CreateMqttOptions();
        try
        {
            Trace($"MQTT connecting host={_options.MqttHost} port={_options.MqttPort} tls={_options.UseTls}");
            await mqttClient.ConnectAsync(mqttOptions, timeout.Token);

            MqttClientSubscribeOptions subscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(_options.SubscribeTopic)
                .Build();
            await mqttClient.SubscribeAsync(subscribeOptions, timeout.Token);
            Trace($"MQTT subscribed topic={_options.SubscribeTopic}");

            byte[] payload = CreateRequestPayload(method, requestId, security);
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(_options.PublishTopic)
                .WithPayload(payload)
                .Build();

            Trace($"MQTT publishing topic={_options.PublishTopic} method={method} requestId={requestId}");
            await mqttClient.PublishAsync(applicationMessage, timeout.Token);

            return await completion.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for Roborock cloud map response to '{method}' after {MapCommandTimeout.TotalSeconds:n0} seconds.");
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync(cancellationToken: CancellationToken.None);
            }
        }
    }

    private MqttClientOptions CreateMqttOptions()
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId($"roborock-map-{Guid.NewGuid():N}")
            .WithTcpServer(_options.MqttHost, _options.MqttPort)
            .WithCredentials(_options.MqttUsername, _options.MqttPassword);

        if (_options.UseTls)
        {
            builder.WithTlsOptions(options => options.UseTls());
        }

        return builder.Build();
    }

    private byte[] CreateRequestPayload(string method, int requestId, RoborockMapSecurity security)
    {
        uint timestamp = UnixTimestamp();
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

        string innerJson = JsonSerializer.Serialize(request, RoborockJson.CompactOptions);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            new RoborockDpsPayload(new Dictionary<string, string> { ["101"] = innerJson }, timestamp),
            RoborockJson.CompactOptions);

        var message = new RoborockMessage
        {
            Version = "1.0",
            Sequence = RandomUInt32(),
            Random = RandomUInt32(),
            Timestamp = timestamp,
            Protocol = RoborockMessageProtocol.RpcRequest,
            Payload = payload
        };

        return RoborockMessageEncoder.Encode(message, _options.LocalKey, prefixed: false);
    }

    private void TryHandleMessage(
        byte[] payload,
        int requestId,
        RoborockMapSecurity security,
        TaskCompletionSource<byte[]> completion)
    {
        if (completion.Task.IsCompleted)
        {
            return;
        }

        try
        {
            RoborockMessage message = RoborockMessageEncoder.Decode(payload, _options.LocalKey);
            Trace($"MQTT RX protocol={message.Protocol} seq={message.Sequence} random={message.Random} payload={message.Payload?.Length ?? 0}");
            if (TryDecodeMapResponse(message, requestId, security, out byte[] mapData))
            {
                completion.TrySetResult(mapData);
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or CryptographicException)
        {
            Trace($"MQTT ignored undecodable message: {ex.Message}");
        }
    }

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

    private void Trace(string message) => _trace?.Invoke(message);

    private static byte[] ToArray(ReadOnlySequence<byte> payload)
    {
        byte[] bytes = new byte[payload.Length];
        payload.CopyTo(bytes);
        return bytes;
    }

    private static uint RandomUInt32()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    private static uint UnixTimestamp() => (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
