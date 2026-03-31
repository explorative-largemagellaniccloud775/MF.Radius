using System.Buffers;
using System.Net;
using MF.Radius.Core.Models;

namespace MF.Radius.Core.Interfaces;

/// <summary>
/// Defines a high-performance processor for handling incoming RADIUS packets.
/// Implement this interface to provide custom authentication, authorization, or accounting logic.
/// </summary>
public interface IRadiusProcessor
{
    
    /// <summary>
    /// Processes an incoming RADIUS request and generates a serialized RADIUS response packet.
    /// </summary>
    ValueTask<IMemoryOwner<byte>?> ProcessAsync(
        RadiusPacket requestPacket,
        EndPoint remoteEndPoint,
        CancellationToken ct
    );
    
}
