using E_Commerce.Application.interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.BackgroundJobs
{
    public class DailySalesBatchJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySalesBatchJob> _logger;

        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3600 * 24);
        private const int ChunkSize = 10;

        public DailySalesBatchJob(IServiceScopeFactory scopeFactory, ILogger<DailySalesBatchJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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

                await Task.Delay(Interval, stoppingToken);
            }

            _logger.LogInformation("[DailySalesBatchJob] Stopped.");
        }

        private async Task RunAsync(CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBatchSalesProcessor>();


            var report = await processor.ProcessInParallelChunksAsync(DateTime.UtcNow, ChunkSize);

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