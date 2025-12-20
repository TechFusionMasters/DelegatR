using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatorKit;

var services = new Dictionary<Type, object?>();

services[typeof(IRequestHandler<HelloRequest, string>)] = new HelloRequestHandler();
services[typeof(IEnumerable<IPipelineBehavior<HelloRequest, string>>)] = new IPipelineBehavior<HelloRequest, string>[]
{
    new TimingBehavior()
};
services[typeof(IEnumerable<INotificationHandler<GreetingNotification>>)] = new INotificationHandler<GreetingNotification>[]
{
    new GreetingNotificationHandler1(),
    new GreetingNotificationHandler2()
};

ServiceFactory factory = serviceType =>
{
    services.TryGetValue(serviceType, out var instance);
    return instance;
};

var mediator = new Mediator(factory);

Console.WriteLine("--- Send ---");
var response = await mediator.Send(new HelloRequest("World"));
Console.WriteLine($"Response: {response}");

Console.WriteLine();
Console.WriteLine("--- Publish (sequential) ---");
await mediator.Publish(new GreetingNotification("Hello from Publish"));

internal sealed record HelloRequest(string Name) : IRequest<string>;

internal sealed class HelloRequestHandler : IRequestHandler<HelloRequest, string>
{
    public Task<string> Handle(HelloRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}

internal sealed class TimingBehavior : IPipelineBehavior<HelloRequest, string>
{
    public async Task<string> Handle(HelloRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Console.WriteLine("[TimingBehavior] before");
            return await next();
        }
        finally
        {
            sw.Stop();
            Console.WriteLine($"[TimingBehavior] after ({sw.ElapsedMilliseconds} ms)");
        }
    }
}

internal sealed record GreetingNotification(string Message) : INotification;

internal sealed class GreetingNotificationHandler1 : INotificationHandler<GreetingNotification>
{
    public Task Handle(GreetingNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[H1] {notification.Message}");
        return Task.CompletedTask;
    }
}

internal sealed class GreetingNotificationHandler2 : INotificationHandler<GreetingNotification>
{
    public Task Handle(GreetingNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[H2] {notification.Message}");
        return Task.CompletedTask;
    }
}
