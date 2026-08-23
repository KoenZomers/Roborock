namespace KoenZomers.RoboRock.Api.Utils;

/// <summary>
/// Calculates CRC-32 checksums used by Roborock and PNG payloads.
/// </summary>
internal static class Crc32
{
    private static readonly uint[] Table = CreateTable();

    /// <summary>
    /// Computes the CRC-32 checksum for the supplied bytes.
    /// </summary>
    /// <param name="data">The bytes to checksum.</param>
    /// <returns>The computed CRC-32 value.</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xffffffffu;
        foreach (byte value in data)
        {
            crc = Table[(crc ^ value) & 0xff] ^ (crc >> 8);
        }

        return crc ^ 0xffffffffu;
    }

    /// <summary>
    /// Creates the CRC-32 lookup table.
    /// </summary>
    /// <returns>The initialized CRC-32 lookup table.</returns>
    private static uint[] CreateTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                crc = (crc & 1) == 1 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }

            table[i] = crc;
        }

        return table;
    }
}
