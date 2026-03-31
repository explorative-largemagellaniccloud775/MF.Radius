using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public record GetSessionsAllQuery
    : IQuery<IReadOnlyList<SessionDto>>
;