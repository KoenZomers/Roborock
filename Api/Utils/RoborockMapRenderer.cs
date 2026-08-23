using System.Buffers.Binary;
using System.IO.Compression;
using KoenZomers.RoboRock.Api.Models;

namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Renders Roborock RRMap image blocks into PNG images.
/// </summary>
internal static class RoborockMapRenderer
{
    private const int FileHeaderLength = 20;
    private const int ImageBlockType = 2;
    private const int DigestBlockType = 1024;
    private const int PngCompressionLevel = 6;

    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    private static readonly RgbaColor[] RoomColors =
    [
        new(240, 178, 122), new(133, 193, 233), new(217, 136, 128), new(52, 152, 219),
        new(205, 97, 85), new(243, 156, 18), new(88, 214, 141), new(245, 176, 65),
        new(252, 212, 81), new(72, 201, 176), new(84, 153, 199), new(133, 193, 233),
        new(245, 176, 65), new(82, 190, 128), new(72, 201, 176), new(165, 105, 189)
    ];

    /// <summary>
    /// Renders a Roborock RRMap payload as a PNG image.
    /// </summary>
    /// <param name="content">The decrypted and decompressed RRMap payload bytes.</param>
    /// <returns>The rendered PNG map image.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is not a valid RRMap image.</exception>
    public static RoborockMapImage RenderPng(byte[] content)
    {
        ParsedImageBlock image = ParseImageBlock(content);
        byte[] rgba = RenderRgba(image);
        byte[] png = EncodePng(image.Width, image.Height, rgba);
        return new RoborockMapImage(png, image.Width, image.Height);
    }

    /// <summary>
    /// Finds and parses the image block inside a Roborock RRMap payload.
    /// </summary>
    /// <param name="content">The RRMap payload bytes.</param>
    /// <returns>The parsed image block.</returns>
    private static ParsedImageBlock ParseImageBlock(byte[] content)
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
                return ParseImageBlock(content, position, blockHeaderLength, (int)blockDataLength);
            }

            position = (int)nextPosition;
        }

        throw new InvalidDataException("Roborock map payload does not contain an image block.");
    }

    /// <summary>
    /// Determines the effective end offset for RRMap block data.
    /// </summary>
    /// <param name="contentLength">The total payload length.</param>
    /// <param name="headerLength">The RRMap file header length.</param>
    /// <param name="dataLength">The data length reported by the RRMap header.</param>
    /// <returns>The offset where block parsing should stop.</returns>
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

    /// <summary>
    /// Parses an RRMap image block at the supplied payload offset.
    /// </summary>
    /// <param name="content">The RRMap payload bytes.</param>
    /// <param name="blockStart">The image block start offset.</param>
    /// <param name="blockHeaderLength">The image block header length.</param>
    /// <param name="blockDataLength">The image block data length.</param>
    /// <returns>The parsed image block.</returns>
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

    /// <summary>
    /// Reads a little-endian unsigned 32-bit value as a checked integer.
    /// </summary>
    /// <param name="content">The source bytes.</param>
    /// <param name="offset">The offset to read from.</param>
    /// <returns>The parsed integer value.</returns>
    private static int ReadInt32(byte[] content, int offset) =>
        checked((int)BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(offset, 4)));

    /// <summary>
    /// Converts RRMap image pixels to bottom-up RGBA image bytes.
    /// </summary>
    /// <param name="image">The parsed image block to render.</param>
    /// <returns>The rendered RGBA bytes.</returns>
    private static byte[] RenderRgba(ParsedImageBlock image)
    {
        byte[] rgba = new byte[checked(image.Width * image.Height * 4)];
        for (int sourceY = 0; sourceY < image.Height; sourceY++)
        {
            int targetY = image.Height - 1 - sourceY;
            for (int x = 0; x < image.Width; x++)
            {
                RgbaColor color = PixelToColor(image.Pixels[x + (sourceY * image.Width)]);
                int offset = ((targetY * image.Width) + x) * 4;
                rgba[offset] = color.R;
                rgba[offset + 1] = color.G;
                rgba[offset + 2] = color.B;
                rgba[offset + 3] = color.A;
            }
        }

        return rgba;
    }

    /// <summary>
    /// Converts one Roborock map pixel value to an RGBA color.
    /// </summary>
    /// <param name="value">The encoded Roborock map pixel value.</param>
    /// <returns>The RGBA color for the pixel.</returns>
    private static RgbaColor PixelToColor(byte value) =>
        value switch
        {
            0x00 => new RgbaColor(19, 87, 148),
            0x01 => new RgbaColor(100, 196, 254),
            0xff => new RgbaColor(32, 115, 185),
            0x07 => new RgbaColor(221, 221, 221),
            _ => DecodeSegmentPixel(value)
        };

    /// <summary>
    /// Decodes segment and obstacle bits from a Roborock map pixel.
    /// </summary>
    /// <param name="value">The encoded segment pixel value.</param>
    /// <returns>The RGBA color for the segment pixel.</returns>
    private static RgbaColor DecodeSegmentPixel(byte value)
    {
        int obstacle = value & 0x07;
        int segmentId = value >> 3;
        return obstacle switch
        {
            0 => new RgbaColor(93, 109, 126),
            1 => new RgbaColor(100, 196, 254),
            7 when segmentId > 0 => RoomColors[(segmentId - 1) % RoomColors.Length],
            _ => new RgbaColor(0, 0, 0)
        };
    }

    /// <summary>
    /// Encodes RGBA image bytes as a PNG file.
    /// </summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="rgba">The RGBA image bytes.</param>
    /// <returns>The PNG-encoded image bytes.</returns>
    private static byte[] EncodePng(int width, int height, byte[] rgba)
    {
        using var output = new MemoryStream();
        output.Write(PngSignature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr[0..4], (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr[4..8], (uint)height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(output, "IHDR"u8, ihdr);

        byte[] scanlines = CreateScanlines(width, height, rgba);
        byte[] compressed = Compress(scanlines);
        WriteChunk(output, "IDAT"u8, compressed);
        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);

        return output.ToArray();
    }

    /// <summary>
    /// Creates unfiltered PNG scanlines from RGBA image bytes.
    /// </summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="rgba">The RGBA image bytes.</param>
    /// <returns>The PNG scanline bytes.</returns>
    private static byte[] CreateScanlines(int width, int height, byte[] rgba)
    {
        int rowLength = checked(width * 4);
        byte[] scanlines = new byte[checked((rowLength + 1) * height)];
        for (int y = 0; y < height; y++)
        {
            int targetOffset = y * (rowLength + 1);
            scanlines[targetOffset] = 0;
            Buffer.BlockCopy(rgba, y * rowLength, scanlines, targetOffset + 1, rowLength);
        }

        return scanlines;
    }

    /// <summary>
    /// Compresses PNG scanline bytes with zlib compression.
    /// </summary>
    /// <param name="data">The raw scanline bytes.</param>
    /// <returns>The compressed IDAT bytes.</returns>
    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Writes a PNG chunk with length and CRC fields.
    /// </summary>
    /// <param name="stream">The output PNG stream.</param>
    /// <param name="type">The four-byte PNG chunk type.</param>
    /// <param name="data">The PNG chunk payload bytes.</param>
    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        stream.Write(length);
        stream.Write(type);
        stream.Write(data);

        byte[] crcInput = new byte[type.Length + data.Length];
        type.CopyTo(crcInput);
        data.CopyTo(crcInput.AsSpan(type.Length));

        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32.Compute(crcInput));
        stream.Write(crc);
    }

    /// <summary>
    /// Describes an RRMap image block and its pixel data.
    /// </summary>
    private sealed record ParsedImageBlock(int Top, int Left, int Width, int Height, byte[] Pixels);

    /// <summary>
    /// Describes an RGBA pixel color.
    /// </summary>
    private readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255);
}
