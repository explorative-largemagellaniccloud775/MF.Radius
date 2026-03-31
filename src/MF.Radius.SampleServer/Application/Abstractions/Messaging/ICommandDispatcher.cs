namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Represents a mechanism for dispatching commands and retrieving results asynchronously within an application.
/// </summary>
public interface ICommandDispatcher
{
    ValueTask<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct)
        where TCommand : ICommand<TResult>;
}