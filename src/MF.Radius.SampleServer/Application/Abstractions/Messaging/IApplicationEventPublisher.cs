namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Defines a contract for publishing application events to facilitate a decoupled,
/// message-driven architecture within the application.
/// Implementations of this interface are responsible for invoking the appropriate
/// event handlers or subscribers upon receiving the event to be published.
/// </summary>
public interface IApplicationEventPublisher
{
    ValueTask PublishAsync<TEvent>(TEvent evt, CancellationToken ct)
        where TEvent : IApplicationEvent;
}
