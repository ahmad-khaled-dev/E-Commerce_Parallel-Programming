using E_Commerce.Application.DTOs.Notification;
using E_Commerce.Application.interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.BackgroundJobs
{
    // Long-running singleton that drains the INotificationQueue.
    // Runs on its own thread pool thread; never blocks HTTP request threads.
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
                    // Blocks asynchronously until a message arrives
                    var msg = await _queue.DequeueAsync(stoppingToken);
                    await ProcessMessageAsync(msg);
                }
                catch (OperationCanceledException)
                {
                    break; // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[NotificationWorker] Unhandled error while processing a notification.");
                }
            }

            _logger.LogInformation("[NotificationWorker] Stopped.");
        }

        // Simulates heavy side-work (PDF invoice generation / SMTP send).
        // Runs in background — the HTTP response has already been returned to the user.
        private async Task ProcessMessageAsync(NotificationMessage msg)
        {
            await Task.Delay(2000); // simulate ~2s of real work

            _logger.LogInformation(
                "[NotificationWorker] Processed | Type={Type} | OrderId={OrderId} | UserId={UserId} | Amount={Amount:C}",
                msg.Type, msg.OrderId, msg.UserId, msg.TotalAmount);
        }
    }
}
