namespace MF.Radius.SampleServer.Application.Features.Nas.Commands.CoA;

public record ApplyAclRestrictionCommand
    : NasCommandBase
{
    public required string AclName { get; init; }
}