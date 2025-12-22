namespace DelegatR;

/// <summary>
/// Dispatches requests to a single handler and publishes notifications to all handlers.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request and returns the response produced by exactly one handler.
    /// Pipeline behaviors, if registered, execute in resolver order and may wrap or short-circuit the invocation.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The token propagated through pipeline and handler.</param>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers for the concrete notification type.
    /// Handlers are executed sequentially in resolver order and execution stops on the first exception.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The token propagated to each handler.</param>
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
