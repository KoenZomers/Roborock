using System.Text.Json;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests parsing of Roborock room mapping responses.
/// </summary>
public sealed class RoborockRoomMappingTests
{
    [Fact]
    /// <summary>
    /// Verifies that the common nested get_room_mapping response shape is parsed.
    /// </summary>
    public void FromJson_WhenResponseContainsNestedPairs_ReturnsRoomMappings()
    {
        using JsonDocument document = JsonDocument.Parse("""[[16,"100001"],[17,"100002"]]""");

        IReadOnlyList<RoborockRoomMapping> mappings = RoborockRoomMapping.FromJson(document.RootElement);

        Assert.Equal(2, mappings.Count);
        Assert.Equal(16, mappings[0].SegmentId);
        Assert.Equal("100001", mappings[0].IotId);
        Assert.Equal(17, mappings[1].SegmentId);
        Assert.Equal("100002", mappings[1].IotId);
    }

    [Fact]
    /// <summary>
    /// Verifies that the flat single-room get_room_mapping response shape is parsed.
    /// </summary>
    public void FromJson_WhenResponseContainsFlatPair_ReturnsSingleRoomMapping()
    {
        using JsonDocument document = JsonDocument.Parse("""[16,"100001"]""");

        IReadOnlyList<RoborockRoomMapping> mappings = RoborockRoomMapping.FromJson(document.RootElement);

        RoborockRoomMapping mapping = Assert.Single(mappings);
        Assert.Equal(16, mapping.SegmentId);
        Assert.Equal("100001", mapping.IotId);
    }
}
