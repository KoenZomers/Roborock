# RoboRock S6 MaxV Library

Native C# local client for Roborock V1 vacuums such as the Roborock S6 MaxV.

Note: I have created this so I can control my Roborock S6 MaxV vacuum cleaner from my own home automation software. I've tried to keep it as generic as possible, but this is my sole purpose. If it works for you as well, awesome, feel free to use it, if not, feel free to fork the code and try to make it work for yours.

## Change Log

[Full version history](https://github.com/KoenZomers/Roborock/blob/main/CHANGELOG.md)

## Usage

```csharp
using KoenZomers.RoboRock.Api;
using KoenZomers.RoboRock.Api.Enums;
using KoenZomers.RoboRock.Api.Models;

await using var client = new RoborockClient(
	host: "192.168.1.50",
	localKey: "your-local-key",
	duid: "optional-device-duid");

await client.ConnectAsync();

RoborockStatus status = await client.GetStatusAsync();
Console.WriteLine($"State: {status.State} ({status.StateName}), Dock: {status.DockStatus}, Battery: {status.Battery}%");
Console.WriteLine($"Current clean duration: {status.CleanDuration}");

RoborockDeviceProperties properties = await client.GetDevicePropertiesAsync();
Console.WriteLine($"Total area: {properties.CleanSummary?.SquareMeterCleanArea} m^2");
Console.WriteLine($"Total clean duration: {properties.CleanSummary?.CleanDuration}");
Console.WriteLine($"Main brush left: {properties.Consumable?.MainBrushTimeLeft}");
Console.WriteLine($"Last clean: {properties.LastCleanRecord?.BeginDateTime} - {properties.LastCleanRecord?.EndDateTime}");
```

`GetDevicePropertiesAsync()` mirrors the local part of Home Assistant's Roborock diagnostics. It combines:

- `get_status` for battery, current status, current clean time/area, error state, current map flag, mop attachment, water-box attachment and water-shortage diagnostics.
- `get_clean_summary` for total cleaning time, total area, total count and history record IDs.
- `get_consumable` for brush/filter/sensor usage and calculated time-left values.
- `get_clean_record` for the newest record from the clean summary.

Home Assistant derives the current room from parsed map content (`vacuum_room`) plus room metadata from `get_room_mapping`/cloud home data; it is not returned directly by `get_status`. This library exposes the current map flag through `RoborockStatus.CurrentMap` and typed room mappings through `GetRoomMappingsAsync()`, but room names still require account home-data from outside the local status command.

Optional protocol diagnostics can be captured with a trace callback:

```csharp
await using var client = new RoborockClient(
	host: "192.168.1.50",
	localKey: "your-local-key",
	trace: message => Console.Error.WriteLine(message));
```

## Commands

```csharp
await client.StartAsync();
await client.PauseAsync();
await client.DockAsync();
await client.StopAsync();
await client.FindMeAsync();
await client.SetFanPowerAsync(RoborockFanPower.Balanced);
```

## Maps and camera

The library can fetch the map list, current room mapping, raw V1 map payload and a directly usable PNG map image:

```csharp
JsonElement maps = await client.GetMultiMapsListAsync();
JsonElement rooms = await client.GetRoomMappingAsync();
IReadOnlyList<RoborockRoomMapping> roomMappings = await client.GetRoomMappingsAsync();
RoborockMapData map = await client.GetRawMapDataAsync(mapSecurityKey: "your-rriot-k-value");
await File.WriteAllBytesAsync("roborock-map.bin", map.Content);
await File.WriteAllBytesAsync("roborock-map.png", map.ToPng());

RoborockMapImage image = await client.GetMapImageAsync(mapSecurityKey: "your-rriot-k-value");
Console.WriteLine($"Map size: {image.Width}x{image.Height}, type: {image.ContentType}");
await File.WriteAllBytesAsync("roborock-map-direct.png", image.PngContent);
```

`GetRawMapDataAsync()` uses Roborock's protocol-301 map channel and needs the Roborock RRiot `k` value from account/session data to decrypt the map response. The returned bytes are decrypted and decompressed RRMap data. `ToPng()`, `ToImage()` and `GetMapImageAsync()` render that payload to PNG bytes without additional imaging dependencies. Home Assistant/python-roborock fetch map content through the cloud MQTT map RPC channel rather than the local TCP command channel; devices that do not emit protocol-301 map payloads locally can therefore still time out with this local-only client.

For vacuums with a built-in camera, the library exposes the Roborock commands used by WebRTC/go2rtc integrations:

```csharp
JsonElement status = await client.GetCameraStatusAsync();
await client.SetCameraStatusAsync(true);
await client.StartCameraPreviewAsync();
JsonElement turn = await client.GetTurnServerAsync();
JsonElement robotSdp = await client.GetDeviceSdpAsync(localSdpPayload);
await client.SendIceToRobotAsync(localIceCandidatePayload);
await client.StopCameraPreviewAsync();
```

These methods perform the Roborock signaling calls only; consuming the camera feed still requires a WebRTC peer or a bridge such as go2rtc to handle SDP/ICE negotiation and media decoding.

For unsupported commands, use the raw RPC API:

```csharp
JsonElement result = await client.SendCommandAsync("get_status");
```

## Tests

The test project reads local device settings from `Tests\roborock.json`, which is committed with empty values to document the expected shape. Put machine-specific credentials in `Tests\roborock.local.json`; this file overrides the default config and is intentionally ignored by `.gitignore`. Add `mapSecurityKey` with the Roborock RRiot `k` value to enable the raw map payload integration test.

Live tests are opt-in so normal test runs do not require the vacuum to be reachable:

```powershell
$env:ROBOROCK_RUN_INTEGRATION_TESTS="1"
dotnet test
```

## Notes

- The client connects locally over TCP port `58867`.
- The device `local_key` is required.
- The `duid` is accepted for compatibility with Roborock V1 session data.
- For the tested S6 MaxV path, commands are sent as DPS-wrapped local requests using `GENERAL_REQUEST`.
- Time values from the device are exposed as `TimeSpan` values instead of raw seconds.
- Numeric Roborock status values are exposed as enums in the `KoenZomers.RoboRock.Api.Enums` namespace where known.

## Sources and thanks

This library was built with help from the open-source Roborock ecosystem. Thanks to:

- [Home Assistant's Roborock integration](https://github.com/home-assistant/core/tree/dev/homeassistant/components/roborock) for the diagnostic-property shape and integration behavior used as a reference.
- [python-roborock](https://github.com/humbertogontijo/python-roborock) for Roborock V1 constants, status/error mappings, consumable replacement intervals, map-channel behavior and command naming references.
- [XiaomiRobotVacuumProtocol](https://github.com/marcelrv/XiaomiRobotVacuumProtocol) for Roborock RRMap binary format documentation and Kaitai definitions.
- [openHAB Add-ons](https://github.com/openhab/openhab-addons) for Roborock map parsing and rendering behavior references.
- [Valetudo](https://github.com/Hypfer/Valetudo) for Roborock map parser behavior, especially v1.1 image block and room segment handling.
- [Xiaomi Cloud Map Extractor](https://github.com/PiotrMachowski/Home-Assistant-custom-components-Xiaomi-Cloud-Map-Extractor) for map pixel color and rendering conventions.
- [go2rtc](https://github.com/AlexxIT/go2rtc) and the Roborock/Home Assistant community for documenting the WebRTC camera signaling path used by camera-capable vacuums.
- The broader Home Assistant and Roborock community for documenting and validating local Roborock protocol behavior.
