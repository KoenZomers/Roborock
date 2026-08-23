using System.Security.Cryptography;
using System.Text;

namespace KoenZomers.RoboRock.Api.Models;

/// <summary>
/// Contains the endpoint token and nonce used by Roborock map requests.
/// </summary>
internal sealed class RoborockMapSecurity
{
    /// <summary>
    /// Initializes a new map security payload.
    /// </summary>
    /// <param name="endpoint">The endpoint token sent with the map request.</param>
    /// <param name="nonce">The nonce used to decrypt the map response.</param>
    private RoborockMapSecurity(string endpoint, byte[] nonce)
    {
        Endpoint = endpoint;
        Nonce = nonce;
    }

    /// <summary>
    /// Gets the endpoint token sent with the map request.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets the nonce used to decrypt the map response.
    /// </summary>
    public byte[] Nonce { get; }

    /// <summary>
    /// Creates map security material from the Roborock RRiot security key.
    /// </summary>
    /// <param name="securityKey">The Roborock RRiot <c>k</c> value.</param>
    /// <returns>The generated map security material.</returns>
    public static RoborockMapSecurity Create(string securityKey)
    {
        if (string.IsNullOrWhiteSpace(securityKey))
        {
            throw new ArgumentException("Roborock map security key is required.", nameof(securityKey));
        }

        byte[] nonce = RandomNumberGenerator.GetBytes(16);
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(securityKey));
        string endpoint = Convert.ToBase64String(hash[8..14]);
        return new RoborockMapSecurity(endpoint, nonce);
    }
}
