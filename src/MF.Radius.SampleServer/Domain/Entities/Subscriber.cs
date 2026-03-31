using System.Net;

namespace MF.Radius.SampleServer.Domain.Entities;

/// <summary>
/// Represents a broadband subscriber in the ISP system.
/// </summary>
public class Subscriber
{
    public required string UserName { get; init; }
    
    /// <summary>
    /// Current status of the subscriber (Active, Restricted, etc.)
    /// </summary>
    public required SubscriberStatus Status { get; init; }
    
    /// <summary>
    /// Plain-text password for all authentication methods.
    /// </summary>
    public required string StoredPassword { get; init; }
    
    /// <summary>
    /// Download rate limit in Kbps.
    /// </summary>
    public required uint BaseRateLimit { get; init; }
    
    /// <summary>
    /// Static IP assigned to the subscriber. 
    /// If null, the NAS should assign an IP from its local pool (Dynamic IP).
    /// </summary>
    public IPAddress? StaticIp { get; init; }
    
}
