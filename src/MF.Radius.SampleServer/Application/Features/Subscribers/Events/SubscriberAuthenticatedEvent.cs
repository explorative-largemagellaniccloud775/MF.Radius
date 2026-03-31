using MF.Radius.Core.Enums;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Subscribers.Events;

/// <summary>
/// Published when Access-Accept is produced for a subscriber.
/// </summary>
public record SubscriberAuthenticatedEvent
    : IApplicationEvent
{
    public required string? UserName { get; init; }
    public required RadiusAuthProtocol AuthProtocol { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
