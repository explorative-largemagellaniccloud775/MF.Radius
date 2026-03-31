namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Handles a single query type.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    ValueTask<TResult> HandleAsync(TQuery query, CancellationToken ct);
}