using System.Net;
using MF.Radius.Core.Enums;

namespace MF.Radius.Core.Models.Acct;

/// <summary>
/// Represents a specialized RADIUS Accounting-Request.
/// </summary>
public record RadiusAcctRequest
{
    public required string UserName { get; init; }
    public required string SessionId { get; init; }
    public required RadiusAcctStatusType StatusType { get; init; }
    
    /// <summary>
    /// The original RADIUS packet for access to supplementary attributes
    /// (e.g. usage stats).
    /// </summary>
    public required RadiusPacket RawPacket { get; init; }
    
    public required EndPoint RemoteEndPoint { get; init; }
    
}
