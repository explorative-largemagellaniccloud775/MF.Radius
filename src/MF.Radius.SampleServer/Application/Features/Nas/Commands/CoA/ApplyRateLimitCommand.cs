namespace MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;

public record ApplyRateLimitCommand
    : NasCommandBase
{
    /// <summary>Downstream rate in Kbps.</summary>
    public required uint DownstreamKbps { get; init; }

    /// <summary>Optional upstream rate in Kbps (NAS-specific support).</summary>
    public uint? UpstreamKbps { get; init; }
    
}
