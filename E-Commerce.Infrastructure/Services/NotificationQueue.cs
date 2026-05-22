using E_Commerce.Application.DTOs.Notification;
using E_Commerce.Application.interfaces;
using System.Threading.Channels;

namespace E_Commerce.Infrastructure.Services
{

    public class NotificationQueue : INotificationQueue
    {

        private readonly Channel<NotificationMessage> _channel =
            Channel.CreateBounded<NotificationMessage>(new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        public void Enqueue(NotificationMessage message)
        {

            if (!_channel.Writer.TryWrite(message))
                throw new InvalidOperationException("Notification queue is full — apply back-pressure at the caller.");
        }

        public async Task<NotificationMessage> DequeueAsync(CancellationToken cancellationToken)
            => await _channel.Reader.ReadAsync(cancellationToken);
    }
}
