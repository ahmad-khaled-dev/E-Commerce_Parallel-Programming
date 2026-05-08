using E_Commerce.Application.DTOs.Batch;

namespace E_Commerce.Application.interfaces
{
    public interface IBatchSalesProcessor
    {
        // CASE 1 — PROBLEM: loads all records into memory at once
        Task<BatchSalesReportDto> ProcessWithoutBatchingAsync(DateTime date);

        // CASE 2 — SOLUTION: processes records in bounded chunks
        Task<BatchSalesReportDto> ProcessInChunksAsync(DateTime date, int chunkSize = 10);
    }
}
