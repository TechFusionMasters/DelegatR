namespace DelegatR;

/// <summary>
/// Represents a notification published via <see cref="IMediator.Publish(INotification, CancellationToken)"/>.
/// Notifications are delivered to all registered handlers for the concrete notification type.
/// </summary>
public interface INotification
{
}
