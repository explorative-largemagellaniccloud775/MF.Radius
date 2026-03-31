namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Defines a contract for handling application events of a specific type.
/// </summary>
/// <typeparam name="TEvent">
/// The type of the application event to be handled. Must implement the <see cref="IApplicationEvent"/> interface.
/// </typeparam>
/// <remarks>
/// Implementations of this interface are responsible for processing application events
/// and executing the required business logic or side effects associated with the event.
/// Typically, these handlers are used in event-driven architectures.
/// This interface supports asynchronous operations and allows for cancellation.
/// </remarks>
public interface IApplicationEventHandler<in TEvent>
    where TEvent : IApplicationEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken ct);
}
