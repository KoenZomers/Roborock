using System.Security.Cryptography;
using System.Text;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests Roborock cloud MQTT connection option derivation.
/// </summary>
public sealed class RoborockCloudConnectionOptionsTests
{
    [Fact]
    /// <summary>
    /// Verifies that MQTT credentials and topics are derived like python-roborock.
    /// </summary>
    public void MqttProperties_WhenRriotValuesProvided_DeriveHomeAssistantCompatibleValues()
    {
        var options = new RoborockCloudConnectionOptions
        {
            Duid = "device-1",
            LocalKey = "1234567890123456",
            User = "rr-user",
            Secret = "rr-secret",
            Key = "rr-key",
            MqttUrl = "ssl://mqtt.example.com:8883"
        };

        Assert.Equal("mqtt.example.com", options.MqttHost);
        Assert.Equal(8883, options.MqttPort);
        Assert.True(options.UseTls);
        Assert.Equal(Md5Hex("rr-user:rr-key")[2..10], options.MqttUsername);
        Assert.Equal(Md5Hex("rr-secret:rr-key")[16..], options.MqttPassword);
        Assert.Equal($"rr/m/i/rr-user/{options.MqttUsername}/device-1", options.PublishTopic);
        Assert.Equal($"rr/m/o/rr-user/{options.MqttUsername}/device-1", options.SubscribeTopic);
    }

    [Fact]
    /// <summary>
    /// Verifies that cloud home data can be configured with a known home id.
    /// </summary>
    public void HasHomeDataConfig_WhenHomeIdAndRriotHashConfigured_ReturnsTrue()
    {
        var options = new RoborockCloudConnectionOptions
        {
            Duid = "device-1",
            LocalKey = "1234567890123456",
            User = "rr-user",
            Secret = "rr-secret",
            Key = "rr-key",
            MqttUrl = "ssl://mqtt.example.com:8883",
            Hash = "rr-hash",
            ApiUrl = "https://api.example.com",
            HomeId = 12345
        };

        Assert.True(options.HasHomeDataConfig);
        options.ValidateHomeData();
    }

    private static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
