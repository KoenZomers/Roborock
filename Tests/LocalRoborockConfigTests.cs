namespace Tests;

/// <summary>
/// Tests loading local Roborock integration test configuration.
/// </summary>
public sealed class LocalRoborockConfigTests
{
    [Fact]
    /// <summary>
    /// Verifies that the local integration test configuration contains required metadata when enabled.
    /// </summary>
    public void LocalConfig_WhenIntegrationTestsEnabled_LoadsValidLocalDeviceMetadata()
    {
        if (!LocalRoborockTestConfig.RunIntegrationTests)
        {
            return;
        }

        LocalRoborockTestConfig config = LocalRoborockTestConfig.Load();

        Assert.False(string.IsNullOrWhiteSpace(config.Duid));
        Assert.False(string.IsNullOrWhiteSpace(config.Model));
        Assert.False(string.IsNullOrWhiteSpace(config.Host));
        Assert.Equal(58867, config.Port);
        Assert.Equal(16, config.LocalKey.Length);
    }
}
