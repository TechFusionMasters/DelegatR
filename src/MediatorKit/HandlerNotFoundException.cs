namespace MediatorKit;

public sealed class HandlerNotFoundException : InvalidOperationException
{
    public HandlerNotFoundException(Type requestType, Type responseType)
        : base($"Handler was not found for request type '{requestType.FullName}' with response type '{responseType.FullName}'.")
    {
    }
}
