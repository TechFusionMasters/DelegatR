namespace DelegatR;

/// <summary>
/// Default mediator implementation that resolves handlers and pipeline behaviors via <see cref="ServiceFactory"/>.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly ServiceFactory _factory;

    /// <summary>
    /// Initializes a new instance of the mediator.
    /// </summary>
    /// <param name="factory">Service resolver used to obtain handlers and pipeline behaviors.</param>
    public Mediator(ServiceFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Publishes a notification to all handlers for the concrete notification type.
    /// Handlers are awaited sequentially in resolver order and execution stops on the first exception.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The token propagated to each handler.</param>
    public Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        return PublishCore(notification, cancellationToken);
    }

    /// <summary>
    /// Sends a request to exactly one handler for the concrete request type.
    /// Pipeline behaviors execute in resolver order and may wrap or short-circuit the handler.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The token propagated through pipeline and handler.</param>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _factory(handlerType);
        if (handler is null)
        {
            throw new HandlerNotFoundException(requestType, responseType);
        }

        if (!handlerType.IsInstanceOfType(handler))
        {
            throw new InvalidOperationException(
                $"Resolved handler instance is not assignable to '{handlerType.FullName}'. " +
                $"Actual type: '{handler.GetType().FullName}'.");
        }

        var pipelineBehaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var pipelineEnumerableType = typeof(IEnumerable<>).MakeGenericType(pipelineBehaviorType);

        var pipelineBehaviorsObj = _factory(pipelineEnumerableType);
        var pipelineBehaviors = pipelineBehaviorsObj as System.Collections.IEnumerable;

        RequestHandlerDelegate<TResponse> next = () => InvokeHandler<TResponse>(handlerType, handler, request, cancellationToken);

        if (pipelineBehaviors is not null)
        {
            var behaviors = new List<object>();
            foreach (var behavior in pipelineBehaviors)
            {
                if (behavior is not null)
                {
                    behaviors.Add(behavior);
                }
            }

            for (var i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var currentNext = next;
                next = () => InvokeBehavior<TResponse>(pipelineBehaviorType, behavior, request, currentNext, cancellationToken);
            }
        }

        return next();
    }

    private Task PublishCore(INotification notification, CancellationToken cancellationToken)
    {
        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlerEnumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);

        var handlersObj = _factory(handlerEnumerableType);
        if (handlersObj is not System.Collections.IEnumerable handlers)
        {
            return Task.CompletedTask;
        }

        return InvokeNotificationHandlers(handlerType, handlers, notification, cancellationToken);
    }

    private static async Task InvokeNotificationHandlers(
        Type handlerType,
        System.Collections.IEnumerable handlers,
        INotification notification,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            if (!handlerType.IsInstanceOfType(handler))
            {
                throw new InvalidOperationException(
                    $"Resolved notification handler instance is not assignable to '{handlerType.FullName}'. " +
                    $"Actual type: '{handler.GetType().FullName}'.");
            }

            var handleMethod = handlerType.GetMethod("Handle");
            if (handleMethod is null)
            {
                throw new InvalidOperationException($"Notification handler type '{handlerType.FullName}' does not contain a Handle method.");
            }

            object? result;
            try
            {
                result = handleMethod.Invoke(handler, new object[] { notification, cancellationToken });
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
            if (result is not Task task)
            {
                throw new InvalidOperationException($"Notification handler type '{handlerType.FullName}' returned an invalid result from Handle.");
            }

            await task.ConfigureAwait(false);
        }
    }

    private static Task<TResponse> InvokeHandler<TResponse>(Type handlerType, object handler, object request, CancellationToken cancellationToken)
    {
        var handleMethod = handlerType.GetMethod("Handle");
        if (handleMethod is null)
        {
            throw new InvalidOperationException($"Handler type '{handlerType.FullName}' does not contain a Handle method.");
        }

        var result = handleMethod.Invoke(handler, new[] { request, cancellationToken });
        return result as Task<TResponse>
            ?? throw new InvalidOperationException($"Handler type '{handlerType.FullName}' returned an invalid result from Handle.");
    }

    private static Task<TResponse> InvokeBehavior<TResponse>(Type behaviorType, object behavior, object request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var handleMethod = behaviorType.GetMethod("Handle");
        if (handleMethod is null)
        {
            throw new InvalidOperationException($"Pipeline behavior type '{behaviorType.FullName}' does not contain a Handle method.");
        }

        var result = handleMethod.Invoke(behavior, new object[] { request, next, cancellationToken });
        return result as Task<TResponse>
            ?? throw new InvalidOperationException($"Pipeline behavior type '{behaviorType.FullName}' returned an invalid result from Handle.");
    }
}
