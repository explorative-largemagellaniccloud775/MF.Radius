using System.Net;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Interfaces;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Sessions.Queries;

public class GetSessionByNasAndIdQueryHandler(
    ISessionStore store
)
    : IQueryHandler<GetSessionByNasAndIdQuery, SessionDto?>
{
    
    public async ValueTask<SessionDto?> HandleAsync(GetSessionByNasAndIdQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var session = await store.GetAsync(query.NasIpEndPoint, query.SessionId);
        if (session == null)
            return null;
        return SessionDto.FromEntity(session);
    }
    
}
