namespace MediatorKit;

/// <summary>
/// Resolves services by type.
/// This is intended to be backed by a DI container or a custom resolver.
/// </summary>
/// <param name="serviceType">The service type to resolve.</param>
/// <returns>The resolved service instance, or <c>null</c> if not available.</returns>
public delegate object? ServiceFactory(Type serviceType);
