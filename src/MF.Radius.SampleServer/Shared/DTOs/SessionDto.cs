using System.Net;
using MF.Radius.SampleServer.Application.Models;

namespace MF.Radius.SampleServer.Shared.DTOs;

public record SessionDto
{
    public required string NasIp { get; init; }
    public required int NasPort { get; init; }
    public required string SessionId { get; init; }
    public required string UserName { get; init; }

    public static SessionDto FromEntity(Session session)
    {
        var nasEp = session.NasEndPoint as IPEndPoint;
        return new SessionDto
        {
            NasIp = nasEp?.Address.ToString() ?? string.Empty,
            NasPort = nasEp?.Port ?? 0,
            SessionId = session.SessionId,
            UserName = session.UserName,
        };
    }
    
};