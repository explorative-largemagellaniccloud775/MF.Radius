using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Subscribers.Queries;

public record GetSubscriberQuery
    : IQuery<SubscriberDto?>
{
    public required string UserName { get; init; }
};
