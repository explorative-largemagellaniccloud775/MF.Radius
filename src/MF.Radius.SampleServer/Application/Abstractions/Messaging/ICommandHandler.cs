namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Represents a handler for processing a specific type of command and producing a result.
/// </summary>
/// <typeparam name="TCommand">
/// The type of command to be handled. Must implement <see cref="ICommand{TResult}"/>.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of result produced by the command handler.
/// </typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    ValueTask<TResult> HandleAsync(TCommand command, CancellationToken ct);
}
