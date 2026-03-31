using MF.Radius.SampleServer.Application.Abstractions.Messaging;

namespace MF.Radius.SampleServer.Infrastructure.Messaging;

/// <summary>
/// In-process query dispatcher that resolves one query handler from DI.
/// </summary>
public class InProcessQueryDispatcher(
    IServiceProvider serviceProvider
)
    : IQueryDispatcher
{
    
    public ValueTask<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return handler.HandleAsync(query, ct);
    }
    
}
