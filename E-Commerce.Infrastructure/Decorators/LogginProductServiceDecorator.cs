using E_Commerce.Application.DTOs.Products;
using E_Commerce.Application.DTOs.Products.Products;
using E_Commerce.Application.interfaces;
using E_Commerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace E_Commerce.Infrastructure.Decorators;

public class LoggingProductServiceDecorator : IProductService
{
    private readonly IProductService _inner;
    private readonly ILogger<LoggingProductServiceDecorator> _logger;

    public LoggingProductServiceDecorator(
        IProductService inner,
        ILogger<LoggingProductServiceDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("GetAllAsync started at {Time}", DateTime.Now);

        try
        {
            var result = await _inner.GetAllAsync();

            stopwatch.Stop();

            _logger.LogInformation(
                "GetAllAsync completed at {Time} in {ElapsedMilliseconds} ms. Returned {Count} products.",
                DateTime.Now,
                stopwatch.ElapsedMilliseconds,
                result.Count);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "GetAllAsync failed at {Time} after {ElapsedMilliseconds} ms.",
                DateTime.Now,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("GetByIdAsync started for ProductId={ProductId} at {Time}", id, DateTime.Now);

        try
        {
            var result = await _inner.GetByIdAsync(id);

            stopwatch.Stop();

            _logger.LogInformation(
                "GetByIdAsync completed for ProductId={ProductId} at {Time} in {ElapsedMilliseconds} ms",
                id,
                DateTime.Now,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "GetByIdAsync failed for ProductId={ProductId} at {Time} after {ElapsedMilliseconds} ms",
                id,
                DateTime.Now,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}