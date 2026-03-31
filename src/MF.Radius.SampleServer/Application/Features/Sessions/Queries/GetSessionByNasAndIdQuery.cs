using System.Net;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public record GetSessionByNasAndIdQuery
    : IQuery<SessionDto?>
{
    public required IPEndPoint NasIpEndPoint { get; init; }
    public required string SessionId { get; init; }
};