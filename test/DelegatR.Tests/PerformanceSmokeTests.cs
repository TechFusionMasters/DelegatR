using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DelegatR;
using Xunit;

namespace DelegatR.Tests;

public sealed class PerformanceSmokeTests
{
    [Fact]
    public async Task Send_performance_smoke_test()
    {
        // This is a stability-focused smoke test, not a benchmark.
        // Threshold is intentionally generous to avoid flakiness.

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IRequestHandler<Ping, int>))
            {
                return new PingHandler();
            }

            if (serviceType == typeof(IEnumerable<IPipelineBehavior<Ping, int>>))
            {
                return Array.Empty<IPipelineBehavior<Ping, int>>();
            }

            return null;
        };

        var mediator = new Mediator(factory);
        var request = new Ping();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var beforeBytes = GC.GetTotalMemory(forceFullCollection: false);

        const int iterations = 10_000;
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
        {
            _ = await mediator.Send(request);
        }

        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"Send loop took too long: {sw.Elapsed}.");

        var afterBytes = GC.GetTotalMemory(forceFullCollection: false);
        var deltaBytes = afterBytes - beforeBytes;
        Assert.True(deltaBytes < 20 * 1024 * 1024, $"Unexpected memory growth: {deltaBytes} bytes.");
    }

    private sealed class Ping : IRequest<int>
    {
    }

    private sealed class PingHandler : IRequestHandler<Ping, int>
    {
        public Task<int> Handle(Ping request, CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
