using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Provides Roborock local protocol cryptographic and compression helpers.
/// </summary>
internal static class RoborockCrypto
{
    private static readonly byte[] Salt = Encoding.ASCII.GetBytes("TXdfu$jyZ#TZHsg4");

    /// <summary>
    /// Creates the AES token used by Roborock local payload encryption.
    /// </summary>
    /// <param name="localKey">The Roborock local key.</param>
    /// <param name="timestamp">The message Unix timestamp.</param>
    /// <returns>The MD5-derived AES key bytes.</returns>
    public static byte[] CreateToken(string localKey, uint timestamp)
    {
        byte[] timestampBytes = EncodeTimestamp(timestamp);
        byte[] localKeyBytes = Encoding.ASCII.GetBytes(localKey);
        byte[] input = new byte[timestampBytes.Length + localKeyBytes.Length + Salt.Length];

        Buffer.BlockCopy(timestampBytes, 0, input, 0, timestampBytes.Length);
        Buffer.BlockCopy(localKeyBytes, 0, input, timestampBytes.Length, localKeyBytes.Length);
        Buffer.BlockCopy(Salt, 0, input, timestampBytes.Length + localKeyBytes.Length, Salt.Length);

        return MD5.HashData(input);
    }

    /// <summary>
    /// Encrypts a local protocol payload using Roborock AES-ECB payload encryption.
    /// </summary>
    /// <param name="payload">The plaintext payload bytes.</param>
    /// <param name="localKey">The Roborock local key.</param>
    /// <param name="timestamp">The message Unix timestamp.</param>
    /// <returns>The encrypted payload bytes.</returns>
    public static byte[] EncryptPayload(byte[] payload, string localKey, uint timestamp)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = CreateToken(localKey, timestamp);

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(payload, 0, payload.Length);
    }

    /// <summary>
    /// Decrypts a local protocol payload using Roborock AES-ECB payload encryption.
    /// </summary>
    /// <param name="payload">The encrypted payload bytes.</param>
    /// <param name="localKey">The Roborock local key.</param>
    /// <param name="timestamp">The message Unix timestamp.</param>
    /// <returns>The decrypted payload bytes.</returns>
    public static byte[] DecryptPayload(byte[] payload, string localKey, uint timestamp)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = CreateToken(localKey, timestamp);

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(payload, 0, payload.Length);
    }

    /// <summary>
    /// Decrypts AES-CBC ciphertext with a local key string and a zero IV.
    /// </summary>
    /// <param name="ciphertext">The encrypted bytes to decrypt.</param>
    /// <param name="localKey">The ASCII key used for decryption.</param>
    /// <returns>The decrypted bytes.</returns>
    public static byte[] DecryptCbc(byte[] ciphertext, string localKey) =>
        DecryptCbc(ciphertext, Encoding.ASCII.GetBytes(localKey));

    /// <summary>
    /// Decrypts AES-CBC ciphertext with key bytes and a zero IV.
    /// </summary>
    /// <param name="ciphertext">The encrypted bytes to decrypt.</param>
    /// <param name="key">The AES key bytes used for decryption.</param>
    /// <returns>The decrypted bytes.</returns>
    public static byte[] DecryptCbc(byte[] ciphertext, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = new byte[16];

        using ICryptoTransform decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <summary>
    /// Decompresses GZip-compressed Roborock payload bytes.
    /// </summary>
    /// <param name="compressedData">The compressed bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    public static byte[] DecompressGzip(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Encodes the timestamp using Roborock token byte ordering.
    /// </summary>
    /// <param name="timestamp">The message Unix timestamp.</param>
    /// <returns>The reordered ASCII hexadecimal timestamp bytes.</returns>
    private static byte[] EncodeTimestamp(uint timestamp)
    {
        string hex = timestamp.ToString("x8");
        return Encoding.ASCII.GetBytes($"{hex[5]}{hex[6]}{hex[3]}{hex[7]}{hex[1]}{hex[2]}{hex[0]}{hex[4]}");
    }
}
