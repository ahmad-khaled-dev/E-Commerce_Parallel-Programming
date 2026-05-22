using E_Commerce.Application.DTOs.Notification;
using E_Commerce.Application.interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.BackgroundJobs
{
     
    public class NotificationWorker : BackgroundService
    {
        private readonly INotificationQueue _queue;
        private readonly ILogger<NotificationWorker> _logger;

        public NotificationWorker(INotificationQueue queue, ILogger<NotificationWorker> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[NotificationWorker] Started — waiting for messages on the queue.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    var msg = await _queue.DequeueAsync(stoppingToken);
                    await ProcessMessageAsync(msg);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[NotificationWorker] Unhandled error while processing a notification.");
                }
            }

            _logger.LogInformation("[NotificationWorker] Stopped.");
        }

        private async Task ProcessMessageAsync(NotificationMessage msg)
        {
            await Task.Delay(2000);

            _logger.LogInformation(
    "[NotificationWorker] Thread: {ThreadId} | Processed | Type={Type} | OrderId={OrderId} | Amount={Amount}",
    Environment.CurrentManagedThreadId,
    msg.Type,
    msg.OrderId,
    msg.TotalAmount);
        }
    }
}
