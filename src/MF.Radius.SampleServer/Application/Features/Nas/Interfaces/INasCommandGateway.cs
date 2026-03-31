using MF.Radius.SampleServer.Application.Features.Nas.Commands;
using MF.Radius.SampleServer.Application.Features.Nas.Models;

namespace MF.Radius.SampleServer.Application.Features.Nas.Interfaces;

/// <summary>
/// Provides an abstraction for executing commands on a Network Access Server (NAS).
/// </summary>
/// <remarks>
/// The <c>INasCommandGateway</c> interface defines operations for managing network sessions on a NAS,
/// such as restricting or disconnecting user sessions. Implementations of this interface are expected
/// to handle communication with the NAS and return results reflecting the success or failure of the operation.
/// </remarks>
public interface INasCommandGateway
{
    ValueTask<NasCommandResult> RestrictAsync(RestrictSessionCommand command, CancellationToken ct);
    ValueTask<NasCommandResult> DisconnectAsync(DisconnectSessionCommand command, CancellationToken ct);
}
