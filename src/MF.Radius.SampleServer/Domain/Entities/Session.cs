using System.Net;

namespace MF.Radius.SampleServer.Application.Models;

/// <summary>
/// Represents a unique active network session on a specific NAS.
/// </summary>
public record Session
{
    /// <summary>
    /// The unique session identifier provided by the NAS (Acct-Session-Id).
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// The subscriber's identity (User-Name).
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// The IP address and port of the NAS where the session is hosted.
    /// Used as part of the unique key and for sending CoA/Disconnect packets.
    /// </summary>
    public required EndPoint NasEndPoint { get; init; }

    /// <summary>
    /// Timestamp when the session was first registered in the system.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    
    /// <summary>
    /// Optional Cisco-specific profile name currently applied to the session.
    /// </summary>
    public string? CurrentCiscoProfile { get; init; }
    
}
