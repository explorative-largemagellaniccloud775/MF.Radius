using System.Net;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public class GetSessionsByUserNameQueryHandler(
    ISessionStore store
)
    : IQueryHandler<GetSessionsByUserNameQuery, IReadOnlyList<SessionDto>>
{
    
    public async ValueTask<IReadOnlyList<SessionDto>> HandleAsync(GetSessionsByUserNameQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query.UserName)) return [];
        
        var sessions = await store.GetByUserNameAsync(query.UserName);
        var sessionDtos = new List<SessionDto>(capacity: sessions.Count);
        foreach (var session in sessions)
            sessionDtos.Add(SessionDto.FromEntity(session));

        return sessionDtos;
    }
    
}
