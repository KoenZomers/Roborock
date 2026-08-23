using System.Text.Json;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests parsing of Roborock multi-map metadata responses.
/// </summary>
public sealed class RoborockMapInfoTests
{
    [Fact]
    /// <summary>
    /// Verifies that a get_multi_maps_list response is parsed to typed map metadata.
    /// </summary>
    public void FromJson_WhenResponseContainsMapInfo_ReturnsMapsWithNames()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {
              "max_multi_map": 4,
              "multi_map_count": 3,
              "map_info": [
                { "mapFlag": 0, "add_time": 1787488303, "length": 7, "name": "Beneden", "bak_maps": [] },
                { "mapFlag": 1, "add_time": 1655410638, "length": 17, "name": "Eerste verdieping", "bak_maps": [] },
                { "mapFlag": 2, "add_time": 1684840771, "length": 6, "name": "Zolder", "bak_maps": [] }
              ]
            }
            """);

        IReadOnlyList<RoborockMapInfo> maps = RoborockMapInfo.FromJson(document.RootElement);

        Assert.Equal(3, maps.Count);
        Assert.Equal(0, maps[0].MapFlag);
        Assert.Equal("Beneden", maps[0].Name);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1787488303), maps[0].AddedAt);
        Assert.Equal(1, maps[1].MapFlag);
        Assert.Equal("Eerste verdieping", maps[1].Name);
        Assert.Equal(2, maps[2].MapFlag);
        Assert.Equal("Zolder", maps[2].Name);
    }

    [Fact]
    /// <summary>
    /// Verifies that a raw map_info array can be parsed directly.
    /// </summary>
    public void FromJson_WhenResponseIsMapInfoArray_ReturnsMaps()
    {
        using JsonDocument document = JsonDocument.Parse("""[{ "mapFlag": 2, "name": "Zolder" }]""");

        RoborockMapInfo map = Assert.Single(RoborockMapInfo.FromJson(document.RootElement));

        Assert.Equal(2, map.MapFlag);
        Assert.Equal("Zolder", map.Name);
    }

    [Fact]
    /// <summary>
    /// Verifies that image metadata uses the current map flag to expose the matching friendly map name.
    /// </summary>
    public void Create_WhenStatusCurrentMapMatchesMapInfo_ReturnsNamedMapImage()
    {
        using JsonDocument statusDocument = JsonDocument.Parse("""{ "battery": 100, "map_status": 8 }""");
        RoborockStatus status = RoborockStatus.FromJson(statusDocument.RootElement);
        var image = new RoborockMapImage([1, 2, 3], 1, 1);
        RoborockMapInfo[] maps =
        [
            new RoborockMapInfo { MapFlag = 0, Name = "Beneden" },
            new RoborockMapInfo { MapFlag = 2, Name = "Zolder" }
        ];

        RoborockMapImageWithMetadata result = RoborockMapImageWithMetadata.Create(image, status, maps);

        Assert.Equal(2, result.MapFlag);
        Assert.Equal("Zolder", result.Name);
        Assert.Same(image, result.Image);
        Assert.Same(maps, result.Maps);
    }
}
