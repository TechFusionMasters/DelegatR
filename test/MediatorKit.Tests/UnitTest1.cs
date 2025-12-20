using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MediatorKit.Tests;

public class MediatorSendTests
{
    [Fact]
    public async Task Send_returns_handler_response()
    {
        var request = new Ping();
        var handler = new PingHandler(123);

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IRequestHandler<Ping, int>))
            {
                return handler;
            }

            if (serviceType == typeof(IEnumerable<IPipelineBehavior<Ping, int>>))
            {
                return Array.Empty<IPipelineBehavior<Ping, int>>();
            }

            return null;
        };

        var mediator = new Mediator(factory);

        var result = await mediator.Send(request);

        Assert.Equal(123, result);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task Send_throws_when_no_handler_registered()
    {
        var request = new Ping();

        ServiceFactory factory = _ => null;

        var mediator = new Mediator(factory);

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(() => mediator.Send(request));
        Assert.Contains(typeof(Ping).FullName ?? nameof(Ping), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pipeline_behaviors_execute_in_resolver_order()
    {
        var callLog = new List<string>();
        var request = new Ping();
        var handler = new PingHandler(1, callLog);

        var b1 = new RecordingBehavior("b1", callLog);
        var b2 = new RecordingBehavior("b2", callLog);

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IRequestHandler<Ping, int>))
            {
                return handler;
            }

            if (serviceType == typeof(IEnumerable<IPipelineBehavior<Ping, int>>))
            {
                return new IPipelineBehavior<Ping, int>[] { b1, b2 };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        var result = await mediator.Send(request);

        Assert.Equal(1, result);
        Assert.Equal(
            new[] { "b1.before", "b2.before", "handler", "b2.after", "b1.after" },
            callLog);
    }

    [Fact]
    public async Task Pipeline_can_short_circuit_without_calling_next()
    {
        var request = new Ping();
        var handler = new PingHandler(999);
        var shortCircuit = new ShortCircuitBehavior(42);

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IRequestHandler<Ping, int>))
            {
                return handler;
            }

            if (serviceType == typeof(IEnumerable<IPipelineBehavior<Ping, int>>))
            {
                return new IPipelineBehavior<Ping, int>[] { shortCircuit };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        var result = await mediator.Send(request);

        Assert.Equal(42, result);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task CancellationToken_is_propagated_to_pipeline_and_handler()
    {
        var request = new Ping();
        var handler = new TokenCapturingHandler(10);
        var behavior = new TokenCapturingBehavior();

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IRequestHandler<Ping, int>))
            {
                return handler;
            }

            if (serviceType == typeof(IEnumerable<IPipelineBehavior<Ping, int>>))
            {
                return new IPipelineBehavior<Ping, int>[] { behavior };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var result = await mediator.Send(request, token);

        Assert.Equal(10, result);
        Assert.Equal(token, behavior.ObservedToken);
        Assert.Equal(token, handler.ObservedToken);
    }

    private sealed class Ping : IRequest<int>
    {
    }

    private sealed class PingHandler : IRequestHandler<Ping, int>
    {
        private readonly int _value;
        private readonly List<string>? _callLog;

        public PingHandler(int value, List<string>? callLog = null)
        {
            _value = value;
            _callLog = callLog;
        }

        public bool WasCalled { get; private set; }

        public Task<int> Handle(Ping request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            _callLog?.Add("handler");
            return Task.FromResult(_value);
        }
    }

    private sealed class RecordingBehavior : IPipelineBehavior<Ping, int>
    {
        private readonly string _name;
        private readonly List<string> _callLog;

        public RecordingBehavior(string name, List<string> callLog)
        {
            _name = name;
            _callLog = callLog;
        }

        public async Task<int> Handle(Ping request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            _callLog.Add(_name + ".before");
            var result = await next();
            _callLog.Add(_name + ".after");
            return result;
        }
    }

    private sealed class ShortCircuitBehavior : IPipelineBehavior<Ping, int>
    {
        private readonly int _value;

        public ShortCircuitBehavior(int value)
        {
            _value = value;
        }

        public Task<int> Handle(Ping request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            return Task.FromResult(_value);
        }
    }

    private sealed class TokenCapturingBehavior : IPipelineBehavior<Ping, int>
    {
        public CancellationToken ObservedToken { get; private set; }

        public Task<int> Handle(Ping request, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            return next();
        }
    }

    private sealed class TokenCapturingHandler : IRequestHandler<Ping, int>
    {
        private readonly int _value;

        public TokenCapturingHandler(int value)
        {
            _value = value;
        }

        public CancellationToken ObservedToken { get; private set; }

        public Task<int> Handle(Ping request, CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            return Task.FromResult(_value);
        }
    }
}

public class MediatorPublishTests
{
    [Fact]
    public async Task Publish_invokes_all_handlers_exactly_once()
    {
        var notification = new PingNotification();
        var h1 = new CountingNotificationHandler();
        var h2 = new CountingNotificationHandler();

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IEnumerable<INotificationHandler<PingNotification>>))
            {
                return new INotificationHandler<PingNotification>[] { h1, h2 };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        await mediator.Publish(notification);

        Assert.Equal(1, h1.Count);
        Assert.Equal(1, h2.Count);
    }

    [Fact]
    public async Task Publish_is_sequential_in_resolver_order()
    {
        var log = new List<string>();
        var notification = new PingNotification();

        var h1 = new RecordingNotificationHandler("H1", log);
        var h2 = new RecordingNotificationHandler("H2", log);

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IEnumerable<INotificationHandler<PingNotification>>))
            {
                return new INotificationHandler<PingNotification>[] { h1, h2 };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        await mediator.Publish(notification);

        Assert.Equal(new[] { "H1", "H2" }, log);
    }

    [Fact]
    public async Task Publish_stops_on_first_exception()
    {
        var log = new List<string>();
        var notification = new PingNotification();

        var h1 = new ThrowingNotificationHandler(new InvalidOperationException("boom"));
        var h2 = new RecordingNotificationHandler("H2", log);

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IEnumerable<INotificationHandler<PingNotification>>))
            {
                return new INotificationHandler<PingNotification>[] { h1, h2 };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Publish(notification));

        Assert.Empty(log);
    }

    [Fact]
    public async Task Publish_with_zero_handlers_is_no_op()
    {
        var notification = new PingNotification();

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IEnumerable<INotificationHandler<PingNotification>>))
            {
                return Array.Empty<INotificationHandler<PingNotification>>();
            }

            return null;
        };

        var mediator = new Mediator(factory);

        await mediator.Publish(notification);
    }

    [Fact]
    public async Task CancellationToken_is_propagated_to_notification_handlers()
    {
        var notification = new PingNotification();
        var handler = new TokenCapturingNotificationHandler();

        ServiceFactory factory = serviceType =>
        {
            if (serviceType == typeof(IEnumerable<INotificationHandler<PingNotification>>))
            {
                return new INotificationHandler<PingNotification>[] { handler };
            }

            return null;
        };

        var mediator = new Mediator(factory);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await mediator.Publish(notification, token);

        Assert.Equal(token, handler.ObservedToken);
    }

    private sealed class PingNotification : INotification
    {
    }

    private sealed class CountingNotificationHandler : INotificationHandler<PingNotification>
    {
        public int Count { get; private set; }

        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingNotificationHandler : INotificationHandler<PingNotification>
    {
        private readonly string _name;
        private readonly List<string> _log;

        public RecordingNotificationHandler(string name, List<string> log)
        {
            _name = name;
            _log = log;
        }

        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            _log.Add(_name);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingNotificationHandler : INotificationHandler<PingNotification>
    {
        private readonly Exception _exception;

        public ThrowingNotificationHandler(Exception exception)
        {
            _exception = exception;
        }

        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }

    private sealed class TokenCapturingNotificationHandler : INotificationHandler<PingNotification>
    {
        public CancellationToken ObservedToken { get; private set; }

        public Task Handle(PingNotification notification, CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
