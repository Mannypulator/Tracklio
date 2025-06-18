using System.Diagnostics;
using MediatR;
using Tracklio.Shared.Metrics;

namespace Tracklio.Shared.Behaviours;

public sealed class HandlerPerformanceMetricBehaviour<TRequest, TResponse>(
    HandlerPerformanceMetric handlerPerformanceMetric)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly HandlerPerformanceMetric _handlerPerformanceMetric =
        handlerPerformanceMetric;
    private readonly Stopwatch _timer = new();

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();
        var response = await next();
        _timer.Stop();

        _handlerPerformanceMetric.MilliSecondsElapsed(
            _timer.ElapsedMilliseconds);

        return response;
    }
}

