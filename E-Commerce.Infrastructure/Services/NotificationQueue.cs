using E_Commerce.Application.DTOs.Notification;
using E_Commerce.Application.interfaces;
using System.Threading.Channels;

namespace E_Commerce.Infrastructure.Services
{
    // Thread-safe in-memory queue backed by System.Threading.Channels.
    // Registered as Singleton so the same Channel is shared between the
    // controller (producer) and NotificationWorker (consumer).
    public class NotificationQueue : INotificationQueue
    {
        // BoundedChannel prevents unbounded memory growth under load
        private readonly Channel<NotificationMessage> _channel =
            Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.Wait  // back-pressure instead of drop/throw
            });

        public void Enqueue(NotificationMessage message)
        {
            // TryWrite succeeds immediately when there is room in the bounded buffer
            if (!_channel.Writer.TryWrite(message))
                throw new InvalidOperationException("Notification queue is full — apply back-pressure at the caller.");
        }

        public async Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken)
            => await _channel.Reader.ReadAsync(cancellationToken);
    }
}
