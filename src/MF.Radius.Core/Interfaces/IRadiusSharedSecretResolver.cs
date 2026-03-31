using System.Net;

namespace MF.Radius.Core.Interfaces;

/// <summary>
/// Provides a mechanism to resolve the RADIUS Shared Secret based on the client's network endpoint.
/// </summary>
public interface IRadiusSharedSecretResolver
{
    /// <summary>
    /// Resolves the shared secret for a given NAS (Network Access Server) endpoint.
    /// </summary>
    /// <param name="remoteEndPoint">The source address of the RADIUS client.</param>
    /// <returns>The shared secret string associated with the client.</returns>
    ValueTask<string> GetSharedSecretAsync(EndPoint remoteEndPoint);
    
}
