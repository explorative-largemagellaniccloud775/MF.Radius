using System.Net;

namespace MF.Radius.SampleServer.Application.Features.Nas.Commands;

/// <summary>
/// Base command for NAS-targeted operations.
/// </summary>
public abstract record NasCommandBase
{
    public required IPEndPoint NasEndPoint { get; init; }

    /// <summary>
    /// Acct-Session-Id (or equivalent NAS session identifier).
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Optional User-Name for better NAS matching and logging compatibility.
    /// </summary>
    public string? UserName { get; init; }
    
}
