using E_Commerce.Application.DTOs.Inventory;
using E_Commerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace E_Commerce.Infrastructure.Decorators;

public class LoggingInventoryServiceDecorator : IInventoryService
{
    private readonly IInventoryService _inner;
    private readonly ILogger<LoggingInventoryServiceDecorator> _logger;

    public LoggingInventoryServiceDecorator(
        IInventoryService inner,
        ILogger<LoggingInventoryServiceDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<InventoryDto?> GetByProductIdAsync(int productId)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting GetByProductIdAsync for ProductId={ProductId}", productId);

        try
        {
            var result = await _inner.GetByProductIdAsync(productId);

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed GetByProductIdAsync for ProductId={ProductId} in {ElapsedMilliseconds} ms",
                productId,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error in GetByProductIdAsync for ProductId={ProductId} after {ElapsedMilliseconds} ms",
                productId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<InventoryDto> UpdateAsync(int productId, UpdateInventoryRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting UpdateAsync for ProductId={ProductId}, NewQuantity={Quantity}",
            productId,
            request.Quantity);

        try
        {
            var result = await _inner.UpdateAsync(productId, request);

            stopwatch.Stop();

            _logger.LogInformation(
                "Completed UpdateAsync for ProductId={ProductId} in {ElapsedMilliseconds} ms. FinalQuantity={Quantity}",
                productId,
                stopwatch.ElapsedMilliseconds,
                result.Quantity);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error in UpdateAsync for ProductId={ProductId} after {ElapsedMilliseconds} ms",
                productId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    public async Task<InventoryDto> DecreaseAsync(int productId, DecreaseInventoryRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "READ -> Starting DecreaseAsync for ProductId={ProductId}, Amount={Amount}",
            productId,
            request.Amount);

        try
        {
            var result = await _inner.DecreaseAsync(productId, request);

            stopwatch.Stop();

            _logger.LogInformation(
                "WRITE -> Completed DecreaseAsync for ProductId={ProductId} in {ElapsedMilliseconds} ms. FinalQuantity={Quantity}",
                productId,
                stopwatch.ElapsedMilliseconds,
                result.Quantity);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error in DecreaseAsync for ProductId={ProductId} after {ElapsedMilliseconds} ms",
                productId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}