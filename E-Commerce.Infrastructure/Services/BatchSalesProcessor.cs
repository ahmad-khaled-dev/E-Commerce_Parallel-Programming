using E_Commerce.Application.DTOs.Batch;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Infrastructure.Services
{
    public class BatchSalesProcessor : IBatchSalesProcessor
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BatchSalesProcessor> _logger;

        public BatchSalesProcessor(AppDbContext context, ILogger<BatchSalesProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 1 — PROBLEM
        // Loads every order for the day into memory in a single query.
        // On a table with millions of rows this causes:
        //   • A massive memory spike (all rows materialised at once)
        //   • A single long-running DB query that holds a connection open
        //   • No opportunity for other tasks to run during processing
        // ─────────────────────────────────────────────────────────────────────
        public async Task<BatchSalesReportDto> ProcessWithoutBatchingAsync(DateTime date)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Single query — entire result-set lands in RAM at once
            var allOrders = await _context.Orders
                .Where(o => o.CreatedAt.Date == date.Date)
                .ToListAsync();

            decimal totalRevenue = allOrders.Sum(o => o.TotalAmount);

            sw.Stop();

            _logger.LogWarning(
                "[BatchSalesProcessor] WITHOUT BATCHING: loaded {Count} orders in one shot.",
                allOrders.Count);

            return new BatchSalesReportDto
            {
                Approach      = "PROBLEM — No Batching (all records loaded at once)",
                ReportDate    = date,
                TotalOrdersProcessed = allOrders.Count,
                TotalRevenue  = totalRevenue,
                ChunkSize     = allOrders.Count == 0 ? 0 : allOrders.Count,
                TotalChunks   = 1,
                ElapsedMs     = sw.ElapsedMilliseconds,
                Verdict       = $"All {allOrders.Count} records pulled into RAM in one query. " +
                                "Risk: OutOfMemoryException on large tables; no yield for other threads."
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 2 — SOLUTION
        // Processes orders page-by-page (Skip/Take).
        // At most `chunkSize` rows live in memory at any moment.
        // Task.Yield() between chunks lets other continuations run on the
        // thread pool — the event loop stays responsive under high concurrency.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<BatchSalesReportDto> ProcessInChunksAsync(DateTime date, int chunkSize = 10)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int skip           = 0;
            int totalProcessed = 0;
            int chunkCount     = 0;
            decimal totalRevenue = 0m;

            while (true)
            {
                var chunk = await _context.Orders
                    .Where(o => o.CreatedAt.Date == date.Date)
                    .OrderBy(o => o.Id)          // stable ordering required for Skip/Take pagination
                    .Skip(skip)
                    .Take(chunkSize)
                    .ToListAsync();

                if (chunk.Count == 0) break;     // no more records

                totalRevenue   += chunk.Sum(o => o.TotalAmount);
                totalProcessed += chunk.Count;
                chunkCount++;
                skip           += chunkSize;

                _logger.LogInformation(
                    "[BatchSalesProcessor] Chunk {Chunk}: {Count} orders processed (cumulative: {Total}).",
                    chunkCount, chunk.Count, totalProcessed);

                // Yield the thread-pool thread so other tasks can be scheduled
                // between chunks — keeps the system responsive under load
                await Task.Yield();
            }

            sw.Stop();

            return new BatchSalesReportDto
            {
                Approach      = $"SOLUTION — Chunked Processing (chunkSize = {chunkSize})",
                ReportDate    = date,
                TotalOrdersProcessed = totalProcessed,
                TotalRevenue  = totalRevenue,
                ChunkSize     = chunkSize,
                TotalChunks   = chunkCount,
                ElapsedMs     = sw.ElapsedMilliseconds,
                Verdict       = $"Processed {totalProcessed} orders in {chunkCount} chunk(s) of ≤{chunkSize}. " +
                                $"Memory bounded to ~{chunkSize} rows at a time; thread yielded between chunks."
            };
        }
    }
}
