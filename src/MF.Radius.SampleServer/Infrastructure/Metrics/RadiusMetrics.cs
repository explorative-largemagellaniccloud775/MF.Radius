using System.Diagnostics.Metrics;

namespace MF.Radius.SampleServer.Infrastructure.Metrics;

public class RadiusMetrics
{
    private readonly Counter<long> _authSuccess;
    private readonly Counter<long> _authReject;
    private readonly Counter<long> _acctStart;
    private readonly Counter<long> _acctUpdate;
    private readonly Counter<long> _acctStop;

    public RadiusMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricsConstants.RadiusMeterName);

        _authSuccess = meter.CreateCounter<long>("radius.auth.success", "packets", "Total Access-Accept sent");
        _authReject = meter.CreateCounter<long>("radius.auth.reject", "packets", "Total Access-Reject sent");
        _acctStart = meter.CreateCounter<long>("radius.acct.start", "packets", "Total Accounting-Start processed");
        _acctUpdate = meter.CreateCounter<long>("radius.acct.update", "packets", "Total Accounting-Update processed");
        _acctStop = meter.CreateCounter<long>("radius.acct.stop", "packets", "Total Accounting-Stop processed");
    }

    public void IncAuthOk() => _authSuccess.Add(1);
    public void IncAuthReject() => _authReject.Add(1);
    public void IncAcctStart() => _acctStart.Add(1);
    public void IncAcctUpdate() => _acctUpdate.Add(1);
    public void IncAcctStop() => _acctStop.Add(1);
    
}