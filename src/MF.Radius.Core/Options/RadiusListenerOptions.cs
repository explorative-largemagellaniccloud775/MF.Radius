namespace MF.Radius.Core.Options;

/// <summary>
/// Configuration options for the RADIUS server listener.
/// </summary>
public sealed record RadiusListenerOptions
{
    /// <summary>
    /// The IP address to bind the incoming socket to. 
    /// </summary>
    public string BindAddress { get; init; } = "0.0.0.0";
    
    /// <summary>
    /// The UDP port to listen on.
    /// Default is 1812 for Authentication and 1813 for Accounting.
    /// </summary>
    public required int[] Ports { get; set; } = [1812, 1813];

    /// <summary>
    /// The capacity of the inbound packet queue.
    /// </summary>
    public int InboundQueueSize { get; set; } = 5000;

    /// <summary>
    /// The number of concurrent worker tasks processing the requests.
    /// </summary>
    public int ConcurrentWorkers { get; set; } = 4;
    
}
