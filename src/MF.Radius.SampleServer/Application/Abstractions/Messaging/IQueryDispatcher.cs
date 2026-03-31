namespace MF.Radius.SampleServer.Application.Abstractions.Messaging;

/// <summary>
/// Dispatches the query to exactly one query handler.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Dispatches the specified query to a corresponding query handler and returns the result asynchronously.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result expected from the query.</typeparam>
    /// <param name="query">The query instance to dispatch.</param>
    /// <param name="ct">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the query result.</returns>
    ValueTask<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>;
    
}
