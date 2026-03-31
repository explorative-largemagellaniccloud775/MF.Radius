using System.Buffers;
using System.Net;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Interfaces;

/// <summary>
/// Represents an interface for sending RADIUS requests and receiving responses from remote endpoints.
/// Implementations of this interface should handle creation, transmission, and processing of RADIUS packets,
/// including communication with remote servers and handling shared secrets for authentication.
/// </summary>
public interface IRadiusSender
{
    
    /// <summary>
    /// Sends a RADIUS request and awaits a response from the specified remote endpoint.
    /// This method handles sending the request, waiting for the response, and returning the parsed RADIUS packet.
    /// </summary>
    Task<RadiusPacket?> SendAndReceiveAsync(
        IMemoryOwner<byte> requestDataOwner,
        IPEndPoint remoteEp,
        string sharedSecret,
        CancellationToken ct
    );

}