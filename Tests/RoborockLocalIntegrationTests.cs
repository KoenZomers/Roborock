using System.Text.Json;
using KoenZomers.RoboRock.Api;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Contains opt-in local integration tests against a Roborock device.
/// </summary>
public sealed class RoborockLocalIntegrationTests
{
    [Fact]
    /// <summary>
    /// Verifies that status can be retrieved from a local Roborock device when integration tests are enabled.
    /// </summary>
    public async Task GetStatusAsync_WhenIntegrationTestsEnabled_ReturnsStatus()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        RoborockStatus status = await client.GetStatusAsync();

        Assert.InRange(status.Battery, 0, 100);
        Assert.True(status.State >= 0);
    }

    [Fact]
    /// <summary>
    /// Verifies that aggregated diagnostics can be retrieved from a local Roborock device when integration tests are enabled.
    /// </summary>
    public async Task GetDevicePropertiesAsync_WhenIntegrationTestsEnabled_ReturnsHomeAssistantDiagnostics()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        RoborockDeviceProperties properties = await client.GetDevicePropertiesAsync();

        Assert.NotNull(properties.Status);
        Assert.NotNull(properties.CleanSummary);
        Assert.NotNull(properties.Consumable);
        Assert.InRange(properties.Status.Battery, 0, 100);
    }

    [Fact]
    /// <summary>
    /// Verifies that map and room metadata can be retrieved when integration tests are enabled.
    /// </summary>
    public async Task GetMapMetadataAsync_WhenIntegrationTestsEnabled_ReturnsMapsAndRooms()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        JsonElement maps = await client.GetMultiMapsListAsync();
        JsonElement rooms = await client.GetRoomMappingAsync();

        Assert.NotEqual(JsonValueKind.Undefined, maps.ValueKind);
        Assert.NotEqual(JsonValueKind.Undefined, rooms.ValueKind);
    }

    [Fact]
    /// <summary>
    /// Verifies that raw map data can be retrieved and rendered when map security is configured.
    /// </summary>
    public async Task GetRawMapDataAsync_WhenIntegrationTestsEnabledAndMapSecurityKeyConfigured_ReturnsPayload()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();
        if (string.IsNullOrWhiteSpace(config.MapSecurityKey))
        {
            return;
        }

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        RoborockMapData map = await client.GetRawMapDataAsync(config.MapSecurityKey);
        RoborockMapImage image = map.ToImage();

        Assert.NotEmpty(map.Content);
        Assert.NotEmpty(image.PngContent);
    }

    [Fact]
    /// <summary>
    /// Verifies that a rendered map image can be retrieved when map security is configured.
    /// </summary>
    public async Task GetMapImageAsync_WhenIntegrationTestsEnabledAndMapSecurityKeyConfigured_ReturnsPngImage()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();
        if (string.IsNullOrWhiteSpace(config.MapSecurityKey))
        {
            return;
        }

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        RoborockMapImage image = await client.GetMapImageAsync(config.MapSecurityKey);

        Assert.Equal("image/png", image.ContentType);
        Assert.NotEmpty(image.PngContent);
    }

    [Fact]
    /// <summary>
    /// Verifies that a rendered map image can be retrieved through the cloud MQTT map channel used by Home Assistant.
    /// </summary>
    public async Task GetMapImageAsync_WhenCloudMapConfigConfigured_ReturnsPngImage()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();
        if (!config.HasCloudMapConfig)
        {
            return;
        }

        var client = new RoborockCloudMapClient(config.ToCloudConnectionOptions());
        await using var metadataClient = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await metadataClient.ConnectAsync();

        RoborockMapImageWithMetadata image = await client.GetMapImageWithMetadataAsync(metadataClient);

        Assert.Equal("image/png", image.ContentType);
        Assert.NotEmpty(image.PngContent);
        Assert.NotNull(image.MapFlag);
        Assert.False(string.IsNullOrWhiteSpace(image.Name));
        Assert.NotNull(image.CurrentRoom);
        Assert.True(image.CurrentRoom.SegmentId > 0);
        Assert.NotNull(image.CurrentRoom.VacuumPosition.RenderedX);
        Assert.NotNull(image.CurrentRoom.VacuumPosition.RenderedY);
        Assert.InRange(image.CurrentRoom.VacuumPosition.RenderedX.Value, 0, image.Width - 1);
        Assert.InRange(image.CurrentRoom.VacuumPosition.RenderedY.Value, 0, image.Height - 1);
    }

    [Fact]
    /// <summary>
    /// Verifies that camera status can be retrieved from a local Roborock device when integration tests are enabled.
    /// </summary>
    public async Task GetCameraStatusAsync_WhenIntegrationTestsEnabled_ReturnsCameraStatus()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();

        await using var client = new RoborockClient(config.Host, config.LocalKey, config.Duid);
        await client.ConnectAsync();

        JsonElement cameraStatus = await client.GetCameraStatusAsync();

        Assert.NotEqual(JsonValueKind.Undefined, cameraStatus.ValueKind);
    }
}
