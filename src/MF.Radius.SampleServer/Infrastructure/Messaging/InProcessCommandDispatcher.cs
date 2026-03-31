using MF.Radius.SampleServer.Application.Abstractions.Messaging;

namespace MF.Radius.SampleServer.Infrastructure.Messaging;

/// <summary>
/// In-process command dispatcher that resolves exactly one command handler from DI.
/// </summary>
public class InProcessCommandDispatcher(
    IServiceProvider serviceProvider
)
    : ICommandDispatcher
{
    public ValueTask<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct)
        where TCommand : ICommand<TResult>
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.HandleAsync(command, ct);
    }
}
