namespace MediatorKit;

/// <summary>
/// Delegate representing the next step in a request pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
