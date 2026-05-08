using E_Commerce.Application.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers
{
    /// <summary>
    /// Requirement 4 — Batch Processing
    ///
    /// Two endpoints that compare processing all daily orders at once
    /// versus processing them in bounded chunks for better performance.
    ///
    /// A DailySalesBatchJob (BackgroundService) also runs the SOLUTION
    /// approach automatically every 60 seconds — check application logs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BatchProcessingDemoController : ControllerBase
    {
        private readonly IBatchSalesProcessor _processor;

        public BatchProcessingDemoController(IBatchSalesProcessor processor)
        {
            _processor = processor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 1 — PROBLEM
        // All orders for the requested date are loaded into RAM in one query.
        // Safe for small datasets; dangerous at scale (OutOfMemoryException,
        // long-held DB connection, no thread yield).
        //
        // Example: GET /api/batchprocessingdemo/without-batching?date=2026-05-08
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("without-batching")]
        public async Task<IActionResult> WithoutBatching([FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.UtcNow;
            var report = await _processor.ProcessWithoutBatchingAsync(targetDate);
            return Ok(report);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CASE 2 — SOLUTION
        // Orders are fetched page-by-page (Skip / Take).
        // Memory stays bounded to `chunkSize` rows; Task.Yield() between
        // chunks keeps the thread-pool responsive under concurrent load.
        //
        // Example: GET /api/batchprocessingdemo/with-batching?date=2026-05-08&chunkSize=10
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("with-batching")]
        public async Task<IActionResult> WithBatching(
            [FromQuery] DateTime? date,
            [FromQuery] int chunkSize = 10)
        {
            if (chunkSize <= 0)
                return BadRequest("chunkSize must be greater than 0.");

            var targetDate = date ?? DateTime.UtcNow;
            var report = await _processor.ProcessInChunksAsync(targetDate, chunkSize);
            return Ok(report);
        }
    }
}
