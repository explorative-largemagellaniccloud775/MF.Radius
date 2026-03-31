using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Application.Features.Nas.Events;

/// <summary>
/// Published after a NAS command has completed (success/reject/failure/timeout).
/// </summary>
public sealed record NasCommandCompletedEvent
    : IApplicationEvent
{
    public required string CommandName { get; init; }
    public required string SessionId { get; init; }
    public string? UserName { get; init; }

    public required NasCommandResult Result { get; init; }

    public DateTimeOffset OccurredAt { get; init; } = DateTime.Now;
    
}
