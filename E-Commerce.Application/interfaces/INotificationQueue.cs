using E_Commerce.Application.DTOs.Notification;

namespace E_Commerce.Application.interfaces
{
    public interface INotificationQueue
    {
        void Enqueue(NotificationMessage message);
        Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken);
    }
}
