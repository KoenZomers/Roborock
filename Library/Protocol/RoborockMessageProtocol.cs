namespace KoenZomers.RoboRock.Library.Protocol;

/// <summary>
/// Defines Roborock local protocol message type identifiers.
/// </summary>
internal enum RoborockMessageProtocol : ushort
{
    /// <summary>A HELLO request message.</summary>
    HelloRequest = 0,

    /// <summary>A HELLO response message.</summary>
    HelloResponse = 1,

    /// <summary>A ping request message.</summary>
    PingRequest = 2,

    /// <summary>A ping response message.</summary>
    PingResponse = 3,

    /// <summary>A general DPS-wrapped request message.</summary>
    GeneralRequest = 4,

    /// <summary>A general DPS-wrapped response message.</summary>
    GeneralResponse = 5,

    /// <summary>A direct RPC request message.</summary>
    RpcRequest = 101,

    /// <summary>A direct RPC response message.</summary>
    RpcResponse = 102,

    /// <summary>A map payload response message.</summary>
    MapResponse = 301
}
