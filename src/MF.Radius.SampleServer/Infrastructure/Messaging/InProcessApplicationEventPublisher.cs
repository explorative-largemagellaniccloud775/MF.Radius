using MF.Radius.SampleServer.Application.Abstractions.Messaging;

namespace MF.Radius.SampleServer.Infrastructure.Messaging;

/// <summary>
/// In-process publisher that broadcasts application events to all registered handlers.
/// </summary>
public sealed class InProcessApplicationEventPublisher(
    IServiceProvider serviceProvider
)
    : IApplicationEventPublisher
{
    
    public async ValueTask PublishAsync<TEvent>(TEvent evt, CancellationToken ct)
        where TEvent : IApplicationEvent
    {
        var handlers = serviceProvider.GetServices<IApplicationEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(evt, ct);
    }
    
}
