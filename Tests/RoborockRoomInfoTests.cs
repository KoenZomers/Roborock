using System.Text.Json;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests parsing of Roborock cloud room name responses.
/// </summary>
public sealed class RoborockRoomInfoTests
{
    [Fact]
    /// <summary>
    /// Verifies that cloud rooms are parsed from a standard response object.
    /// </summary>
    public void FromJson_WhenResponseContainsResultArray_ReturnsRoomNames()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {
              "success": true,
              "result": [
                { "id": 26021351, "name": "Keuken" },
                { "id": 26021352, "name": "Woonkamer" }
              ]
            }
            """);

        IReadOnlyList<RoborockRoomInfo> rooms = RoborockRoomInfo.FromJson(document.RootElement);

        Assert.Equal(2, rooms.Count);
        Assert.Equal(26021351, rooms[0].Id);
        Assert.Equal("26021351", rooms[0].IotId);
        Assert.Equal("Keuken", rooms[0].Name);
        Assert.Equal("Woonkamer", rooms[1].Name);
    }

    [Fact]
    /// <summary>
    /// Verifies that shared-device room responses using roomId are parsed.
    /// </summary>
    public void FromJson_WhenRoomUsesRoomId_ReturnsRoomName()
    {
        using JsonDocument document = JsonDocument.Parse("""[{ "roomId": 26021351, "name": "Keuken" }]""");

        RoborockRoomInfo room = Assert.Single(RoborockRoomInfo.FromJson(document.RootElement));

        Assert.Equal(26021351, room.Id);
        Assert.Equal("Keuken", room.Name);
    }
}
