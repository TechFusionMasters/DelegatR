namespace DelegatR;

/// <summary>
/// Thrown when a request handler cannot be resolved for a request sent via <see cref="IMediator.Send{TResponse}(IRequest{TResponse}, CancellationToken)"/>.
/// </summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    /// <summary>
    /// Creates an exception describing the missing handler.
    /// </summary>
    /// <param name="requestType">The concrete request type.</param>
    /// <param name="responseType">The expected response type.</param>
    public HandlerNotFoundException(Type requestType, Type responseType)
        : base($"Handler was not found for request type '{requestType.FullName}' with response type '{responseType.FullName}'.")
    {
    }
}
