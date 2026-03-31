using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Subscribers.Interfaces;
using MF.Radius.SampleServer.Shared.DTOs;

namespace MF.Radius.SampleServer.Application.Features.Subscribers.Queries;

/// <summary>
/// Represents a query handler responsible for handling the retrieval of subscriber data
/// based on a provided username query. This class interacts with the
/// <see cref="ISubscriberRepository"/> to fetch subscriber details and maps
/// the domain entity to a data transfer object (<see cref="SubscriberDto"/>).
/// </summary>
public class GetSubscriberQueryHandler(
    ISubscriberRepository repo
)
    : IQueryHandler<GetSubscriberQuery, SubscriberDto?>
{
    
    public async ValueTask<SubscriberDto?> HandleAsync(GetSubscriberQuery query, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query.UserName))
            return null;
        
        var subscriber = await repo.GetByUserNameAsync(query.UserName);
        if (subscriber == null)
            return null;
        
        var subscriberDto = SubscriberDto.FromEntity(subscriber);
        return subscriberDto;
    }
    
}
