namespace MediatorKit;

/// <summary>
/// Handles a notification.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles a notification.
    /// </summary>
    /// <param name="notification">The notification instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
