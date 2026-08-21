using System.Buffers.Binary;
using KoenZomers.RoboRock.Library.Models;

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

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
}
