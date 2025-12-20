namespace MediatorKit;

/// <summary>
/// Handles a request and produces a response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
