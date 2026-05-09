using E_Commerce.Application.DTOs.Notification;
using E_Commerce.Application.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers
{
    /// <summary>
    /// Requirement 3 — Asynchronous Queues
    ///
    /// Two endpoints that demonstrate the difference between processing
    /// side-tasks (invoice + email) synchronously versus via an async queue.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AsyncQueueDemoController : ControllerBase
    {
        private readonly INotificationQueue _queue;
        private readonly ILogger<AsyncQueueDemoController> _logger;

        public AsyncQueueDemoController(INotificationQueue queue, ILogger<AsyncQueueDemoController> logger)
        {
            _queue  = queue;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 1 — PROBLEM
        // Invoice generation and email notification run synchronously inside
        // the HTTP request pipeline. The client must wait ~2 seconds for
        // tasks it does not need the result of.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("without-queue")]
        public async Task<IActionResult> WithoutQueue([FromBody] int orderId)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // --- Core order logic (fast, ~50 ms) ---
            await Task.Delay(50);

            // --- Side tasks that block the response ---
            await SimulateInvoiceGenerationAsync();   // ~1 000 ms
            await SimulateEmailNotificationAsync();   // ~1 000 ms

            sw.Stop();

            _logger.LogWarning(
                "[AsyncQueueDemo] WITHOUT QUEUE: OrderId={OrderId} took {Elapsed}ms (user waited for invoice + email).",
                orderId, sw.ElapsedMilliseconds);

            return Ok(new
            {
                Case          = "PROBLEM — Synchronous side-tasks",
                OrderId       = orderId,
                ElapsedMs     = sw.ElapsedMilliseconds,
                UserWaitedFor = "Core order logic + invoice generation + email notification",
                Verdict       = $"User blocked for {sw.ElapsedMilliseconds} ms. " +
                                "Only ~50 ms were necessary; the rest was avoidable waiting."
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 2 — SOLUTION
        // Heavy side-tasks are enqueued (fire-and-forget) into a
        // Channel<NotificationMessage>. The HTTP response is returned after
        // ~50 ms of real work. NotificationWorker processes the queue entries
        // in the background without ever touching the HTTP thread.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("with-queue")]
        public async Task<IActionResult> WithQueue([FromBody] int orderId)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // --- Core order logic (fast, ~50 ms) ---
            await Task.Delay(50);

            // --- Enqueue side-tasks; do NOT await them here ---
            _queue.Enqueue(new NotificationMessage
            {
                OrderId     = orderId,
                UserId      = 1,
                TotalAmount = 149.99m,
                Type        = "InvoiceGeneration"
            });

            _queue.Enqueue(new NotificationMessage
            {
                OrderId     = orderId,
                UserId      = 1,
                TotalAmount = 149.99m,
                Type        = "EmailConfirmation"
            });

            sw.Stop();

            _logger.LogInformation(
                "[AsyncQueueDemo] WITH QUEUE: OrderId={OrderId} responded in {Elapsed}ms. " +
                "Invoice + email dispatched to NotificationWorker.",
                orderId, sw.ElapsedMilliseconds);

            return Ok(new
            {
                Case          = "SOLUTION — Async Queue (Channel<T> + BackgroundService)",
                OrderId       = orderId,
                ElapsedMs     = sw.ElapsedMilliseconds,
                UserWaitedFor = "Core order logic only",
                Verdict       = $"User received response in {sw.ElapsedMilliseconds} ms. " +
                                "Invoice + email are being processed by NotificationWorker in the background. " +
                                "Check application logs to see when they complete (~2 s later)."
            });
        }

        // Simulates heavy synchronous side-work (PDF rendering, SMTP, etc.)
        private static Task SimulateInvoiceGenerationAsync()  => Task.Delay(1000);
        private static Task SimulateEmailNotificationAsync()  => Task.Delay(1000);
    }
}
