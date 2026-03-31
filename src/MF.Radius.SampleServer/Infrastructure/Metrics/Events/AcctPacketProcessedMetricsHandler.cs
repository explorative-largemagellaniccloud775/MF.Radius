using MF.Radius.Core.Enums;
using MF.Radius.SampleServer.Application.Abstractions.Messaging;
using MF.Radius.SampleServer.Application.Features.Sessions.Events;

namespace MF.Radius.SampleServer.Infrastructure.Metrics.Events;

public class AcctPacketProcessedMetricsHandler(RadiusMetrics metrics)
    : IApplicationEventHandler<AcctPacketProcessedEvent>
{
    
    public ValueTask HandleAsync(AcctPacketProcessedEvent evt, CancellationToken ct)
    {
        switch (evt.StatusType)
        {
            case RadiusAcctStatusType.Start:
                metrics.IncAcctStart();
                break;
            case RadiusAcctStatusType.InterimUpdate:
                metrics.IncAcctUpdate();
                break;
            case RadiusAcctStatusType.Stop:
                metrics.IncAcctStop();
                break;
        }
        return ValueTask.CompletedTask;
    }
    
}
