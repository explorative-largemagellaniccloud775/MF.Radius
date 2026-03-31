using System.Net;
using MF.Radius.SampleServer.Domain.Entities;

namespace MF.Radius.SampleServer.Shared.DTOs;

public class SubscriberDto
{
    public required string UserName { get; init; }
    public SubscriberStatus Status { get; init; }
    public uint RateLimitKbps { get; init; }
    public string? StaticIp { get; init; }

    public static SubscriberDto FromEntity(Subscriber subscriber)
    {
        return new SubscriberDto
        {
            UserName = subscriber.UserName,
            Status = subscriber.Status,
            RateLimitKbps = subscriber.BaseRateLimit,
            StaticIp = subscriber.StaticIp?.ToString(),
        };
    }
    
}