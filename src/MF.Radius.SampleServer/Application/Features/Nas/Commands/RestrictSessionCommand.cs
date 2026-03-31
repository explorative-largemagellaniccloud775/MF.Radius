using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Application.Features.Nas.Commands;

public record RestrictSessionCommand
    : NasCommandBase, ICommand<NasCommandResult>
{
    public required string AclName { get; init; }
}