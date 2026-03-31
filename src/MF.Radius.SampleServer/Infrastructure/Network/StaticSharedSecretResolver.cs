using System.Net;
using MF.Radius.Core.Interfaces;

namespace MF.Radius.SampleServer.Infrastructure.Network;

/// <summary>
/// A static implementation of the <see cref="IRadiusSharedSecretResolver"/> interface.
/// Always returns a pre-configured secret regardless of the remote endpoint.
/// </summary>
/// <remarks>
/// This implementation is ideal for development, testing, or simple environments 
/// where all NAS (Network Access Servers) use the same shared secret.
/// In production environments, it is recommended to implement a resolver that 
/// fetches secrets from a database or a secure configuration provider.
/// </remarks>
public class StaticSharedSecretResolver
    : IRadiusSharedSecretResolver
{
    private readonly string _sharedSecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticSharedSecretResolver"/> class.
    /// </summary>
    /// <param name="sharedSecret">
    /// The shared secret to be used for all RADIUS clients. 
    /// The default value is 'testing123' (standard for many RADIUS tests).
    /// </param>
    /// <exception cref="ArgumentException">Thrown when the shared secret is null or empty.</exception>
    public StaticSharedSecretResolver(string sharedSecret)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret))
            throw new ArgumentException("Shared secret cannot be null or empty.", nameof(sharedSecret));
        _sharedSecret = sharedSecret;
    }

    /// <summary>
    /// Resolves the shared secret for the specified remote endpoint.
    /// </summary>
    /// <param name="remoteEndPoint">The network endpoint of the RADIUS client (NAS).</param>
    /// <returns>
    /// A <see cref="ValueTask{String}"/> containing the static shared secret.
    /// </returns>
    public ValueTask<string> GetSharedSecretAsync(EndPoint remoteEndPoint)
    {
        // For this implementation, we simply return the predefined secret.
        // ValueTask is used here for high performance as no actual I/O or async work is performed.
        return ValueTask.FromResult(_sharedSecret);
    }
    
}
