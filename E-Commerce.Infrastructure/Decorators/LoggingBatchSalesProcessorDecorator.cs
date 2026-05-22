using E_Commerce.Application.DTOs.Batch;
using E_Commerce.Application.interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace E_Commerce.Infrastructure.Decorators;

public class LoggingBatchSalesProcessorDecorator : IBatchSalesProcessor
{
    private readonly IBatchSalesProcessor _inner;
    private readonly ILogger<LoggingBatchSalesProcessorDecorator> _logger;

    public LoggingBatchSalesProcessorDecorator(
        IBatchSalesProcessor inner,
        ILogger<LoggingBatchSalesProcessorDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<BatchSalesReportDto> ProcessWithoutBatchingAsync(DateTime date)
    {
        var stopwatch = Stopwatch.StartNew();
        var methodName = nameof(ProcessWithoutBatchingAsync);

        _logger.LogInformation("{Method} started for Date={Date}", methodName, date);

        try
        {
            var result = await _inner.ProcessWithoutBatchingAsync(date);

            stopwatch.Stop();
            _logger.LogInformation(
                "{Method} completed in {ElapsedMilliseconds} ms. Orders={Orders}, Revenue={Revenue}",
                methodName, stopwatch.ElapsedMilliseconds, result.TotalOrdersProcessed, result.TotalRevenue);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{Method} failed after {ElapsedMilliseconds} ms", methodName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<BatchSalesReportDto> ProcessInParallelChunksAsync(DateTime date, int chunkSize = 10)
    {
        var stopwatch = Stopwatch.StartNew();
        var methodName = nameof(ProcessInParallelChunksAsync);

        _logger.LogInformation("{Method} started for Date={Date}, ChunkSize={ChunkSize}", methodName, date, chunkSize);

        try
        {
            var result = await _inner.ProcessInParallelChunksAsync(date, chunkSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "{Method} completed in {ElapsedMilliseconds} ms. Orders={Orders}, Chunks={Chunks}, Revenue={Revenue}",
                methodName, stopwatch.ElapsedMilliseconds, result.TotalOrdersProcessed, result.TotalChunks, result.TotalRevenue);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{Method} failed after {ElapsedMilliseconds} ms", methodName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
