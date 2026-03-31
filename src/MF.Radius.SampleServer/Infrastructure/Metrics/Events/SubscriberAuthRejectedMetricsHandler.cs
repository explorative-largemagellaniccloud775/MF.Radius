using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Subscribers.Events;

namespace MF.Radius.SampleServer.Infrastructure.Metrics.Events;

public class SubscriberAuthRejectedMetricsHandler(RadiusMetrics metrics)
    : IApplicationEventHandler<SubscriberAuthRejectedEvent>
{
    
    public ValueTask HandleAsync(SubscriberAuthRejectedEvent evt, CancellationToken ct)
    {
        metrics.IncAuthReject();
        return ValueTask.CompletedTask;
    }
    
}