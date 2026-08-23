using System.Buffers.Binary;
using System.IO.Compression;
using KoenZomers.RoboRock.Api.Models;

namespace Tests;

/// <summary>
/// Tests Roborock map data model and rendering behavior.
/// </summary>
public sealed class RoborockMapDataTests
{
    [Fact]
    /// <summary>
    /// Verifies that map data exposes the supplied payload bytes.
    /// </summary>
    public void Constructor_WhenContentProvided_ExposesSameContent()
    {
        byte[] content = [1, 2, 3, 4];

        var mapData = new RoborockMapData(content);

        Assert.Same(content, mapData.Content);
    }

    [Fact]
    /// <summary>
    /// Verifies that map data rejects null payload bytes.
    /// </summary>
    public void Constructor_WhenContentIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RoborockMapData(null!));
    }

    [Fact]
    /// <summary>
    /// Verifies that a minimal RRMap image block renders to a PNG image.
    /// </summary>
    public void ToImage_WhenRrMapContainsImageBlock_ReturnsPngImage()
    {
        var mapData = new RoborockMapData(CreateMinimalRrMap());

        RoborockMapImage image = mapData.ToImage();

        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal("image/png", image.ContentType);
        Assert.Equal(PngSignature, image.PngContent[..PngSignature.Length]);
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(image.PngContent.AsSpan(16, 4)));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(image.PngContent.AsSpan(20, 4)));
    }

    [Fact]
    /// <summary>
    /// Verifies that outside map pixels render transparently when they remain inside the cropped image.
    /// </summary>
    public void ToImage_WhenRrMapContainsOutsidePixels_RendersTransparentBackground()
    {
        var mapData = new RoborockMapData(CreateMinimalRrMap());

        RoborockMapImage image = mapData.ToImage();
        byte[] scanlines = DecompressPngIdat(image.PngContent);

        Assert.Equal(255, scanlines[10]);
        Assert.Equal(255, scanlines[11]);
        Assert.Equal(255, scanlines[12]);
        Assert.Equal(0, scanlines[13]);
    }

    [Fact]
    /// <summary>
    /// Verifies that empty space around the actual map is cropped from the rendered PNG.
    /// </summary>
    public void ToImage_WhenRrMapHasOutsideBorder_CropsToKnownMapBounds()
    {
        var mapData = new RoborockMapData(CreateBorderedRrMapWithRobotPosition());

        RoborockMapImage image = mapData.ToImage();

        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);
    }

    [Fact]
    /// <summary>
    /// Verifies that a minimal RRMap image block renders to PNG bytes.
    /// </summary>
    public void ToPng_WhenRrMapContainsImageBlock_ReturnsPngBytes()
    {
        var mapData = new RoborockMapData(CreateMinimalRrMap());

        byte[] png = mapData.ToPng();

        Assert.Equal(PngSignature, png[..PngSignature.Length]);
    }

    [Fact]
    /// <summary>
    /// Verifies that the vacuum position can be read from an RRMap robot-position block.
    /// </summary>
    public void GetVacuumPosition_WhenRrMapContainsRobotPosition_ReturnsPosition()
    {
        var mapData = new RoborockMapData(CreateMinimalRrMapWithRobotPosition());

        RoborockMapPosition? position = mapData.GetVacuumPosition();

        Assert.NotNull(position);
        Assert.Equal(50, position.X);
        Assert.Equal(0, position.Y);
        Assert.Equal(90, position.Angle);
        Assert.Equal(1, position.RenderedX);
        Assert.Equal(1, position.RenderedY);
    }

    [Fact]
    /// <summary>
    /// Verifies that rendered robot coordinates are relative to the cropped PNG.
    /// </summary>
    public void GetVacuumPosition_WhenRrMapIsCropped_ReturnsCroppedRenderedPosition()
    {
        var mapData = new RoborockMapData(CreateBorderedRrMapWithRobotPosition());

        RoborockMapPosition? position = mapData.GetVacuumPosition();

        Assert.NotNull(position);
        Assert.Equal(100, position.X);
        Assert.Equal(50, position.Y);
        Assert.Equal(1, position.RenderedX);
        Assert.Equal(1, position.RenderedY);
    }

    [Fact]
    /// <summary>
    /// Verifies that the current room is resolved from the robot position and room segment pixel.
    /// </summary>
    public void GetCurrentRoom_WhenRobotPositionIsInRoomSegment_ReturnsRoomSegmentAndMapping()
    {
        var mapData = new RoborockMapData(CreateMinimalRrMapWithRobotPosition());
        RoborockRoomMapping[] mappings = [new RoborockRoomMapping { SegmentId = 16, IotId = "100001" }];
        RoborockRoomInfo[] rooms = [new RoborockRoomInfo { Id = 100001, Name = "Keuken" }];

        RoborockCurrentRoom? room = mapData.GetCurrentRoom(mappings, rooms);

        Assert.NotNull(room);
        Assert.Equal(16, room.SegmentId);
        Assert.Equal("100001", room.IotId);
        Assert.Equal("Keuken", room.Name);
        Assert.Equal(50, room.VacuumPosition.X);
        Assert.Equal(0, room.VacuumPosition.Y);
        Assert.Equal(1, room.VacuumPosition.RenderedX);
        Assert.Equal(1, room.VacuumPosition.RenderedY);
    }

    [Fact]
    /// <summary>
    /// Verifies that invalid RRMap payloads are rejected.
    /// </summary>
    public void ToImage_WhenPayloadIsNotRrMap_ThrowsInvalidDataException()
    {
        var mapData = new RoborockMapData([1, 2, 3, 4]);

        Assert.Throws<InvalidDataException>(() => mapData.ToImage());
    }

    /// <summary>
    /// Creates a minimal RRMap payload with a 2x2 image block for renderer tests.
    /// </summary>
    private static byte[] CreateMinimalRrMap()
    {
        byte[] content = new byte[48];
        content[0] = (byte)'r';
        content[1] = (byte)'r';
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(2, 2), 20);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(4, 4), (uint)content.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(8, 2), 1);

        const int blockOffset = 20;
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(blockOffset, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(blockOffset + 2, 2), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(blockOffset + 4, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(blockOffset + 8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(blockOffset + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(blockOffset + 16, 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(blockOffset + 20, 4), 2);
        content[blockOffset + 24] = 0x00;
        content[blockOffset + 25] = 0x01;
        content[blockOffset + 26] = 0xff;
        content[blockOffset + 27] = 0x07;

        return content;
    }

    /// <summary>
    /// Creates a bordered RRMap payload that should crop to its 2x2 center.
    /// </summary>
    private static byte[] CreateBorderedRrMapWithRobotPosition()
    {
        byte[] content = new byte[80];
        content[0] = (byte)'r';
        content[1] = (byte)'r';
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(2, 2), 20);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(4, 4), (uint)content.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(8, 2), 1);

        const int imageBlockOffset = 20;
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(imageBlockOffset, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(imageBlockOffset + 2, 2), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 4, 4), 16);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 16, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 20, 4), 4);
        content[imageBlockOffset + 24 + 5] = 0xff;
        content[imageBlockOffset + 24 + 6] = (16 << 3) | 0x07;
        content[imageBlockOffset + 24 + 9] = 0xff;
        content[imageBlockOffset + 24 + 10] = 0x07;

        const int robotBlockOffset = 60;
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(robotBlockOffset, 2), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(robotBlockOffset + 2, 2), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 4, 4), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 8, 4), 100);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 12, 4), 50);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 16, 4), 90);

        return content;
    }

    /// <summary>
    /// Decompresses the first PNG IDAT chunk for pixel-level assertions.
    /// </summary>
    private static byte[] DecompressPngIdat(byte[] png)
    {
        int offset = PngSignature.Length;
        while (offset + 12 <= png.Length)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4)));
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "IDAT")
            {
                using var input = new MemoryStream(png, offset + 8, length);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                zlib.CopyTo(output);
                return output.ToArray();
            }

            offset += 12 + length;
        }

        throw new InvalidDataException("PNG does not contain an IDAT chunk.");
    }

    /// <summary>
    /// Creates a minimal RRMap payload with an image block and robot-position block.
    /// </summary>
    private static byte[] CreateMinimalRrMapWithRobotPosition()
    {
        byte[] content = new byte[68];
        content[0] = (byte)'r';
        content[1] = (byte)'r';
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(2, 2), 20);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(4, 4), (uint)content.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(8, 2), 1);

        const int imageBlockOffset = 20;
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(imageBlockOffset, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(imageBlockOffset + 2, 2), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 4, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 8, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 16, 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(imageBlockOffset + 20, 4), 2);
        content[imageBlockOffset + 24] = 0x00;
        content[imageBlockOffset + 25] = (16 << 3) | 0x07;
        content[imageBlockOffset + 26] = 0xff;
        content[imageBlockOffset + 27] = 0x07;

        const int robotBlockOffset = 48;
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(robotBlockOffset, 2), 8);
        BinaryPrimitives.WriteUInt16LittleEndian(content.AsSpan(robotBlockOffset + 2, 2), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 4, 4), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 8, 4), 50);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(content.AsSpan(robotBlockOffset + 16, 4), 90);

        return content;
    }

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
}
