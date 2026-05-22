using E_Commerce.Application.DTOs.Batch;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class BatchSalesProcessor : IBatchSalesProcessor
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<BatchSalesProcessor> _logger;

        public BatchSalesProcessor(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<BatchSalesProcessor> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<BatchSalesReportDto> ProcessWithoutBatchingAsync(DateTime date)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await using var context = _dbContextFactory.CreateDbContext();

            var allOrders = await context.Orders
                .Where(o => o.CreatedAt.Date == date.Date)
                .ToListAsync();

            decimal totalRevenue = allOrders.Sum(o => o.TotalAmount);


            await Task.Delay(5000);
            sw.Stop();

            _logger.LogWarning(
                "[BatchSalesProcessor] WITHOUT BATCHING: loaded {Count} orders in one shot.",
                allOrders.Count);

            return new BatchSalesReportDto
            {
                Approach = "PROBLEM — No Batching (all records loaded at once)",
                ReportDate = date,
                TotalOrdersProcessed = allOrders.Count,
                TotalRevenue = totalRevenue,
                ChunkSize = allOrders.Count == 0 ? 0 : allOrders.Count,
                TotalChunks = 1,
                ElapsedMs = sw.ElapsedMilliseconds,
                Verdict = $"All {allOrders.Count} records pulled into RAM in one query. " +
                          "Risk: OutOfMemoryException on large tables; no yield for other threads."
            };
        }

        public async Task<BatchSalesReportDto> ProcessInParallelChunksAsync(DateTime date, int chunkSize = 10)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();


            await using var countContext = _dbContextFactory.CreateDbContext();
            int totalOrders = await countContext.Orders
                .Where(o => o.CreatedAt.Date == date.Date)
                .CountAsync();

            if (totalOrders == 0)
            {
                return new BatchSalesReportDto
                {
                    Approach = $"SOLUTION — Parallel Chunked Processing (chunkSize = {chunkSize})",
                    ReportDate = date,
                    TotalOrdersProcessed = 0,
                    TotalRevenue = 0,
                    ChunkSize = chunkSize,
                    TotalChunks = 0,
                    ElapsedMs = 0,
                    Verdict = "No orders found for this date."
                };
            }

            int totalChunks = (int)Math.Ceiling((double)totalOrders / chunkSize);


            var chunkRevenues = new decimal[totalChunks];
            var chunkCounts = new int[totalChunks];


            var tasks = Enumerable.Range(0, totalChunks).Select(index => Task.Run(async () =>
            {
                int skip = index * chunkSize;

                await using var context = _dbContextFactory.CreateDbContext();

                var chunk = await context.Orders
                    .Where(o => o.CreatedAt.Date == date.Date)
                    .OrderBy(o => o.Id)
                    .Skip(skip)
                    .Take(chunkSize)
                    .ToListAsync();

                chunkRevenues[index] = chunk.Sum(o => o.TotalAmount);
                chunkCounts[index] = chunk.Count;

                _logger.LogInformation(
                    "[BatchSalesProcessor] | Thread: {Thread}| Parallel Chunk {Chunk}/{Total}: {Count} orders (skip={Skip}). ",
                    Thread.CurrentThread.ManagedThreadId, index + 1, totalChunks, chunk.Count, skip);
            }));

            await Task.WhenAll(tasks);

            sw.Stop();

            int totalProcessed = chunkCounts.Sum();
            decimal totalRevenue = chunkRevenues.Sum();

            return new BatchSalesReportDto
            {
                Approach = $"SOLUTION — Parallel Chunked Processing (chunkSize = {chunkSize})",
                ReportDate = date,
                TotalOrdersProcessed = totalProcessed,
                TotalRevenue = totalRevenue,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks,
                ElapsedMs = sw.ElapsedMilliseconds,
                Verdict = $"Processed {totalProcessed} orders across {totalChunks} parallel chunks of ≤{chunkSize}. " +
                          $"All chunks ran concurrently via Task.WhenAll — each with its own DbContext."
            };
        }
    }
}