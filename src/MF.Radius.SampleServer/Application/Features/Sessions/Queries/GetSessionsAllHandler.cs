using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public class GetSessionsAllHandler(
    ISessionStore store
)
    : IQueryHandler<GetSessionsAllQuery, IReadOnlyList<SessionDto>>
{
    
    public async ValueTask<IReadOnlyList<SessionDto>> HandleAsync(GetSessionsAllQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var sessions = await store.GetAllAsync();
        var sessionDtos = new List<SessionDto>(capacity: sessions.Count);
        foreach (var session in sessions)
            sessionDtos.Add(SessionDto.FromEntity(session));
        return sessionDtos;
    }
    
}
