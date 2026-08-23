using System.Buffers.Binary;
using KoenZomers.RoboRock.Api.Models;

namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Parses metadata from Roborock RRMap payloads.
/// </summary>
internal static class RoborockMapParser
{
    private const int FileHeaderLength = 20;
    private const int ImageBlockType = 2;
    private const int RobotPositionBlockType = 8;
    private const int DigestBlockType = 1024;
    private const byte MapInside = 0xff;
    private const byte MapScan = 0x07;
    private const int RoomObstacleMask = 0x07;
    private const int RoomObstacleValue = 7;

    /// <summary>
    /// Resolves the room segment currently containing the vacuum.
    /// </summary>
    /// <param name="content">The decrypted and decompressed RRMap payload bytes.</param>
    /// <param name="roomMappings">Room segment to IoT room mappings.</param>
    /// <returns>The current room, or <see langword="null" /> when the map does not expose a resolvable room.</returns>
    public static RoborockCurrentRoom? GetCurrentRoom(byte[] content, IReadOnlyList<RoborockRoomMapping>? roomMappings = null)
    {
        ParsedMap parsedMap = Parse(content);
        if (parsedMap.Image is null || parsedMap.VacuumPosition is null)
        {
            return null;
        }

        int? segmentId = GetRoomAtVacuumPosition(parsedMap.Image, parsedMap.VacuumPosition);
        if (segmentId is null)
        {
            return null;
        }

        string? iotId = roomMappings?.FirstOrDefault(mapping => mapping.SegmentId == segmentId.Value)?.IotId;
        return new RoborockCurrentRoom(segmentId.Value, iotId, parsedMap.VacuumPosition);
    }

    /// <summary>
    /// Gets the vacuum position from the RRMap payload.
    /// </summary>
    /// <param name="content">The decrypted and decompressed RRMap payload bytes.</param>
    /// <returns>The vacuum position, or <see langword="null" /> when the map does not contain one.</returns>
    public static RoborockMapPosition? GetVacuumPosition(byte[] content) => Parse(content).VacuumPosition;

    private static ParsedMap Parse(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length < FileHeaderLength || content[0] != 'r' || content[1] != 'r')
        {
            throw new InvalidDataException("The Roborock map payload is not a valid RRMap file.");
        }

        ushort headerLength = BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(2, 2));
        if (headerLength < FileHeaderLength || headerLength > content.Length)
        {
            throw new InvalidDataException($"Invalid Roborock map header length: {headerLength}.");
        }

        uint dataLength = BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(4, 4));
        int endOffset = GetDataEndOffset(content.Length, headerLength, dataLength);

        ParsedImageBlock? image = null;
        RoborockMapPosition? vacuumPosition = null;
        for (int position = headerLength; position + 8 <= endOffset;)
        {
            ushort blockType = BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(position, 2));
            ushort blockHeaderLength = BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(position + 2, 2));
            uint blockDataLength = BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(position + 4, 4));

            if (blockType == DigestBlockType)
            {
                break;
            }

            if (blockHeaderLength < 8)
            {
                throw new InvalidDataException($"Invalid Roborock map block header length: {blockHeaderLength}.");
            }

            long dataStart = (long)position + blockHeaderLength;
            long nextPosition = dataStart + blockDataLength;
            if (dataStart > endOffset || nextPosition > endOffset)
            {
                throw new InvalidDataException("Roborock map block extends beyond the payload length.");
            }

            if (blockType == ImageBlockType)
            {
                image = ParseImageBlock(content, position, blockHeaderLength, (int)blockDataLength);
            }
            else if (blockType == RobotPositionBlockType)
            {
                vacuumPosition = ParseObjectPosition(content, (int)dataStart, (int)blockDataLength);
            }

            position = (int)nextPosition;
        }

        if (image is not null && vacuumPosition is not null)
        {
            vacuumPosition = WithRenderedPosition(image, vacuumPosition);
        }

        return new ParsedMap(image, vacuumPosition);
    }

    private static int GetDataEndOffset(int contentLength, int headerLength, uint dataLength)
    {
        if (dataLength >= headerLength && dataLength <= contentLength)
        {
            return (int)dataLength;
        }

        if (dataLength <= contentLength - headerLength)
        {
            return headerLength + (int)dataLength;
        }

        return contentLength;
    }

    private static ParsedImageBlock ParseImageBlock(byte[] content, int blockStart, int blockHeaderLength, int blockDataLength)
    {
        if (blockHeaderLength < 24)
        {
            throw new InvalidDataException($"Invalid Roborock image block header length: {blockHeaderLength}.");
        }

        int top = ReadInt32(content, blockStart + blockHeaderLength - 16);
        int left = ReadInt32(content, blockStart + blockHeaderLength - 12);
        int height = ReadInt32(content, blockStart + blockHeaderLength - 8);
        int width = ReadInt32(content, blockStart + blockHeaderLength - 4);

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"Invalid Roborock image dimensions: {width}x{height}.");
        }

        int expectedPixelCount = checked(width * height);
        if (blockDataLength < expectedPixelCount)
        {
            throw new InvalidDataException($"Roborock image block contains {blockDataLength} bytes, but {expectedPixelCount} pixels were expected.");
        }

        int pixelStart = blockStart + blockHeaderLength;
        byte[] pixels = content[pixelStart..(pixelStart + expectedPixelCount)];
        return new ParsedImageBlock(top, left, width, height, pixels);
    }

    private static RoborockMapPosition ParseObjectPosition(byte[] content, int dataStart, int blockDataLength)
    {
        if (blockDataLength < 8)
        {
            throw new InvalidDataException($"Invalid Roborock object position length: {blockDataLength}.");
        }

        int? angle = null;
        if (blockDataLength > 8)
        {
            angle = ReadInt32(content, dataStart + 8);
            if (angle > 0xff)
            {
                angle = (angle & 0xff) - 256;
            }
        }

        return new RoborockMapPosition(ReadInt32(content, dataStart), ReadInt32(content, dataStart + 4), angle);
    }

    private static int? GetRoomAtVacuumPosition(ParsedImageBlock image, RoborockMapPosition vacuumPosition)
    {
        (int x, int y) = GetRawImagePixelPosition(image, vacuumPosition);
        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
        {
            return null;
        }

        byte pixel = image.Pixels[x + (image.Width * y)];
        if (pixel is MapInside or MapScan || (pixel & RoomObstacleMask) != RoomObstacleValue)
        {
            return null;
        }

        return pixel >> 3;
    }

    private static RoborockMapPosition WithRenderedPosition(ParsedImageBlock image, RoborockMapPosition vacuumPosition)
    {
        (int x, int y) = GetRawImagePixelPosition(image, vacuumPosition);
        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
        {
            return vacuumPosition;
        }

        RoborockMapImageBounds bounds = RoborockMapImageGeometry.GetContentBounds(image.Pixels, image.Width, image.Height);
        if (x < bounds.Left || y < bounds.Top || x >= bounds.Right || y >= bounds.Bottom)
        {
            return vacuumPosition;
        }

        return new RoborockMapPosition(
            vacuumPosition.X,
            vacuumPosition.Y,
            vacuumPosition.Angle,
            x - bounds.Left,
            bounds.Height - 1 - (y - bounds.Top));
    }

    private static (int X, int Y) GetRawImagePixelPosition(ParsedImageBlock image, RoborockMapPosition vacuumPosition) =>
        ((int)Math.Round((vacuumPosition.X / 50d) - image.Left, MidpointRounding.AwayFromZero),
            (int)Math.Round((vacuumPosition.Y / 50d) - image.Top, MidpointRounding.AwayFromZero));

    private static int ReadInt32(byte[] content, int offset) =>
        checked((int)BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(offset, 4)));

    private sealed record ParsedMap(ParsedImageBlock? Image, RoborockMapPosition? VacuumPosition);

    private sealed record ParsedImageBlock(int Top, int Left, int Width, int Height, byte[] Pixels);
}
