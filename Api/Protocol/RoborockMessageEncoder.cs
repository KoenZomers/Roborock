using System.Buffers.Binary;
using System.Text;
using KoenZomers.RoboRock.Api.Utils;

namespace KoenZomers.RoboRock.Api.Protocol;

/// <summary>
/// Encodes and decodes Roborock local protocol packets.
/// </summary>
internal static class RoborockMessageEncoder
{
    /// <summary>
    /// Encodes a Roborock message into a wire-format packet.
    /// </summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="localKey">The Roborock local key.</param>
    /// <param name="prefixed">Whether to prefix the body with its byte length.</param>
    /// <returns>The encoded packet bytes.</returns>
    public static byte[] Encode(RoborockMessage message, string localKey, bool prefixed = true)
    {
        byte[] body = BuildBody(message, localKey);
        if (!prefixed)
        {
            return body;
        }

        byte[] packet = new byte[4 + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), (uint)body.Length);
        body.CopyTo(packet.AsSpan(4));
        return packet;
    }

    /// <summary>
    /// Decodes a Roborock wire-format packet into a message.
    /// </summary>
    /// <param name="data">The packet bytes, with or without a length prefix.</param>
    /// <param name="localKey">The Roborock local key.</param>
    /// <returns>The decoded Roborock message.</returns>
    public static RoborockMessage Decode(ReadOnlySpan<byte> data, string localKey)
    {
        if (data.Length >= 7 && !IsVersion(data[..3]))
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(data[..4]);
            if (length > data.Length - 4)
            {
                throw new InvalidDataException($"Incomplete Roborock packet. Expected {length} bytes, got {data.Length - 4}.");
            }

            data = data.Slice(4, (int)length);
        }

        if (data.Length < 17)
        {
            throw new InvalidDataException("Roborock packet is too short.");
        }

        string version = Encoding.ASCII.GetString(data[..3]);
        uint sequence = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(3, 4));
        uint random = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(7, 4));
        uint timestamp = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(11, 4));
        var protocol = (RoborockMessageProtocol)BinaryPrimitives.ReadUInt16BigEndian(data.Slice(15, 2));

        byte[]? payload = null;
        int offset = 17;
        if (data.Length > offset)
        {
            if (data.Length < offset + 2)
            {
                throw new InvalidDataException("Roborock payload length is missing.");
            }

            ushort encryptedLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            offset += 2;
            if (data.Length < offset + encryptedLength)
            {
                throw new InvalidDataException("Roborock payload is incomplete.");
            }

            ReadOnlySpan<byte> encryptedPayload = data.Slice(offset, encryptedLength);
            offset += encryptedLength;

            if (data.Length >= offset + 4)
            {
                uint expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
                uint actualCrc = Crc32.Compute(data[..offset]);
                if (expectedCrc != actualCrc)
                {
                    throw new InvalidDataException($"Roborock CRC mismatch. Expected 0x{expectedCrc:x8}, computed 0x{actualCrc:x8}.");
                }
            }

            payload = RoborockCrypto.DecryptPayload(encryptedPayload.ToArray(), localKey, timestamp);
        }

        return new RoborockMessage
        {
            Version = version,
            Sequence = sequence,
            Random = random,
            Timestamp = timestamp,
            Protocol = protocol,
            Payload = payload
        };
    }

    /// <summary>
    /// Builds the unprefixed Roborock message body.
    /// </summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="localKey">The Roborock local key.</param>
    /// <returns>The encoded message body.</returns>
    private static byte[] BuildBody(RoborockMessage message, string localKey)
    {
        byte[] version = Encoding.ASCII.GetBytes(message.Version);
        if (version.Length != 3)
        {
            throw new InvalidOperationException("Roborock protocol version must be exactly 3 bytes.");
        }

        byte[] payload = message.Payload is { Length: > 0 }
            ? RoborockCrypto.EncryptPayload(message.Payload, localKey, message.Timestamp)
            : Array.Empty<byte>();

        int payloadFieldLength = payload.Length > 0 ? 2 + payload.Length : 0;
        int checksumLength = payload.Length > 0 ? 4 : 0;
        byte[] body = new byte[17 + payloadFieldLength + checksumLength];

        version.CopyTo(body.AsSpan(0, 3));
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(3, 4), message.Sequence);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(7, 4), message.Random);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(11, 4), message.Timestamp);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(15, 2), (ushort)message.Protocol);

        if (payload.Length > 0)
        {
            BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(17, 2), (ushort)payload.Length);
            payload.CopyTo(body.AsSpan(19));
            uint crc = Crc32.Compute(body.AsSpan(0, 19 + payload.Length));
            BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(19 + payload.Length, 4), crc);
        }

        return body;
    }

    /// <summary>
    /// Determines whether a byte span contains a known Roborock protocol version prefix.
    /// </summary>
    /// <param name="value">The bytes to inspect.</param>
    /// <returns><see langword="true" /> when the bytes are a known protocol version; otherwise, <see langword="false" />.</returns>
    private static bool IsVersion(ReadOnlySpan<byte> value) =>
        value.Length == 3 &&
        ((value[0] == (byte)'1' && value[1] == (byte)'.' && value[2] == (byte)'0') ||
         (value[0] == (byte)'A' && value[1] == (byte)'0' && value[2] == (byte)'1'));
}
