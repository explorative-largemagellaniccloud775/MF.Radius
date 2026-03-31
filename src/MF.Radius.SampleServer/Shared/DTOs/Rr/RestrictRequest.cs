namespace MF.Radius.SampleServer.Shared.DTOs.Rr;

public record RestrictRequest
{
    public required string NasIp { get; init; }
    public required int NasPort { get; init;}
    public required string SessionId { get; init; }
    public string? UserName { get; init; }
    public required string AclName { get; init; }
}