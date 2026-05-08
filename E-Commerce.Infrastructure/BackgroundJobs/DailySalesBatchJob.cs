using E_Commerce.Application.interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.BackgroundJobs
{
    // Scheduled background job that runs periodically and processes
    // daily sales using the SOLUTION approach (chunked, memory-bounded).
    //
    // Uses IServiceScopeFactory because AppDbContext is Scoped, while
    // BackgroundService is effectively a Singleton — we must create our
    // own scope per execution to resolve Scoped dependencies safely.
    public class DailySalesBatchJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySalesBatchJob> _logger;

        // Short interval for demo/testing; change to TimeSpan.FromHours(24) in production
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
        private const int ChunkSize = 10;

        public DailySalesBatchJob(IServiceScopeFactory scopeFactory, ILogger<DailySalesBatchJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DailySalesBatchJob] Started — will run every {Interval}.", Interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[DailySalesBatchJob] Error during batch execution.");
                }

                // Wait for next run
                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("[DailySalesBatchJob] Stopped.");
        }

        private async Task RunAsync(CancellationToken ct)
        {
            // Create a fresh scope so we get a fresh AppDbContext (Scoped lifetime)
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBatchSalesProcessor>();

            var report = await processor.ProcessInChunksAsync(DateTime.UtcNow, ChunkSize);

            _logger.LogInformation(
                "[DailySalesBatchJob] Completed | Date={Date} | Orders={Orders} | Revenue={Revenue:C} | Chunks={Chunks} | Elapsed={Elapsed}ms",
                report.ReportDate.ToShortDateString(),
                report.TotalOrdersProcessed,
                report.TotalRevenue,
                report.TotalChunks,
                report.ElapsedMs);
        }
    }
}
