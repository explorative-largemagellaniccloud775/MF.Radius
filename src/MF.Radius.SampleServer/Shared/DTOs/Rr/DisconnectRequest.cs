namespace MF.Radius.SampleServer.Shared.DTOs.Rr;

public record DisconnectRequest
{
    public required string NasIp { get; init; }
    public required int NasPort { get; init;}
    public required string SessionId { get; init; }
    public string? UserName { get; init; }
}