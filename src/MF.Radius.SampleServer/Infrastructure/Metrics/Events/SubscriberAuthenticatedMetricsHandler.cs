using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Subscribers.Events;

namespace MF.Radius.SampleServer.Infrastructure.Metrics.Events;

public class SubscriberAuthenticatedMetricsHandler(RadiusMetrics metrics)
    : IApplicationEventHandler<SubscriberAuthenticatedEvent>
{
    
    public ValueTask HandleAsync(SubscriberAuthenticatedEvent evt, CancellationToken ct)
    {
        metrics.IncAuthOk();
        return ValueTask.CompletedTask;
    }
    
}
