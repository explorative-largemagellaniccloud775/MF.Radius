namespace MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;

public record ApplyServiceProfileCommand
    : NasCommandBase
{
    /// <summary>
    /// Vendor/NAS profile name (e.g., "internet-premium", "walled-garden").
    /// </summary>
    public required string ProfileName { get; init; }
    
}
