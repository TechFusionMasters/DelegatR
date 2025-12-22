namespace DelegatR;

/// <summary>
/// Represents a request that is dispatched via <see cref="IMediator.Send{TResponse}(IRequest{TResponse}, CancellationToken)"/>
/// and produces a single response.
/// </summary>
/// <typeparam name="TResponse">The response type produced by the request handler.</typeparam>
public interface IRequest<TResponse>
{
}
