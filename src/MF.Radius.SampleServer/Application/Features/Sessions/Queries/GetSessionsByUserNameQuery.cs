using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public record GetSessionsByUserNameQuery
    : IQuery<IReadOnlyList<SessionDto>>
{
    public required string UserName { get; init; } 
};