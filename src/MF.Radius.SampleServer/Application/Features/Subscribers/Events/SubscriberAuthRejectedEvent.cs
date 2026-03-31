using MF.Radius.Core.Enums;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;

namespace MF.Radius.SampleServer.Application.Features.Subscribers.Events;

/// <summary>
/// Published when Access-Reject is produced.
/// </summary>
public record SubscriberAuthRejectedEvent
    : IApplicationEvent
{
    public required string? UserName { get; init; }
    public required RadiusAuthProtocol AuthProtocol { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
