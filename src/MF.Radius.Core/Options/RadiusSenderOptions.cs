namespace MF.Radius.Core.Options;

/// <summary>
/// Configuration options for the server-initiated RADIUS requests (CoA/DM).
/// </summary>
public sealed record RadiusSenderOptions
{
    /// <summary>
    /// The IP address to bind the outgoing socket to. 
    /// Useful for multi-homed servers. Default is any.
    /// </summary>
    public string BindAddress { get; init; } = "0.0.0.0";
    
    /// <summary>
    /// The UDP port to send the requests to. 
    /// Standard is 3799.
    /// </summary>
    public int Port { get; init; } = 3799;

    /// <summary>
    /// Default timeout for waiting ACK/NAK from NAS if not specified in the call.
    /// </summary>
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(3);
    
}