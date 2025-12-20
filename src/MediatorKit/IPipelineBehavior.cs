namespace MediatorKit;

/// <summary>
/// Defines a request pipeline behavior that can wrap or short-circuit request handling.
/// Behaviors execute in the order they are returned by the configured resolver.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request within the pipeline.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="next">Delegate invoking the next pipeline step.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
