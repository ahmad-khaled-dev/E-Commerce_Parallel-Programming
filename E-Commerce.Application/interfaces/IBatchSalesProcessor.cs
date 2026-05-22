using E_Commerce.Application.DTOs.Batch;

namespace E_Commerce.Application.interfaces
{
    public interface IBatchSalesProcessor
    {
        Task<BatchSalesReportDto> ProcessWithoutBatchingAsync(DateTime date);

        Task<BatchSalesReportDto> ProcessInParallelChunksAsync(DateTime date, int chunkSize = 10);
    }
}
