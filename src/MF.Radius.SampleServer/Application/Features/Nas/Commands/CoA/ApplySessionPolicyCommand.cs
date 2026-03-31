namespace MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;

public record ApplySessionPolicyCommand
    : NasCommandBase
{
    public uint? SessionTimeoutSeconds { get; init; }
    public uint? IdleTimeoutSeconds { get; init; }
    public uint? AcctInterimIntervalSeconds { get; init; }
}
