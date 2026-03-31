using MF.Radius.Core.Enums;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Events;

/// <summary>
/// Published when the accounting packet is processed.
/// </summary>
public class AcctPacketProcessedEvent
    : IApplicationEvent
{
    public required string UserName { get; init; }
    public required string SessionId { get; init; }
    public required RadiusAcctStatusType StatusType { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}