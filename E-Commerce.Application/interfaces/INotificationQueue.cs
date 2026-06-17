using E_Commerce.Application.DTOs.Notification;

namespace E_Commerce.Application.interfaces
{
    public interface INotificationQueue
    {
        // Enqueue a notification message (thread-safe)
        void Enqueue(NotificationMessage message);

        // Dequeue one message; blocks asynchronously until one is available
        Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
