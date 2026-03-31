namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Represents a contract for an application event within the system.
/// </summary>
/// <remarks>
/// Application events implementing this interface are used to signal that
/// particular occurrences or operations have taken place within the application.
/// These events are primarily used in a messaging or event-driven architecture
/// to notify handlers or subscribers of state changes or business logic executions.
/// </remarks>
public interface IApplicationEvent
{
    DateTimeOffset OccurredAt { get; }
}
