namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for read-only requests.
/// </summary>
public interface IQuery<out TResult>;