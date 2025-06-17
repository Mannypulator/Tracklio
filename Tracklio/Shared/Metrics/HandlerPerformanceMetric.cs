using System;
using System.Diagnostics.Metrics;

namespace Tracklio.Shared.Metrics;

public sealed class HandlerPerformanceMetric
{
    private readonly Counter<long> _milliSecondsElapsed;
    public HandlerPerformanceMetric(IMeterFactory meterFactory)
    {
        // a meter
        var meter = meterFactory.Create("TravelInspiration.API");
        _milliSecondsElapsed = meter.CreateCounter<long>(
            "travelinspiration.api.requesthandler.millisecondselapsed");
    }

    public void MilliSecondsElapsed(long milliSecondsElapsed)
           => _milliSecondsElapsed.Add(milliSecondsElapsed);
}

