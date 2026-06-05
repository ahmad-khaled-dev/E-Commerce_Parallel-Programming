using E_Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/distributed-lock")]
public class DistributedLockDemoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDatabase _redisDb;
    private readonly ILogger<DistributedLockDemoController> _logger;

    public DistributedLockDemoController(AppDbContext context, IConnectionMultiplexer redis, ILogger<DistributedLockDemoController> logger)
    {
        _context = context;
        _redisDb = redis.GetDatabase();
        _logger = logger;
    }

    [HttpPost("update-inventory-no-lock")]
    public async Task<IActionResult> UpdateInventoryNoLock(int productId, int quantity)
    {
        var inventory = await _context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (inventory is null)
            return NotFound(new { message = "Inventory not found" });

        var originalQuantity = inventory.Quantity;
        inventory.Quantity = quantity;

        Thread.Sleep(100);

        await _context.SaveChangesAsync();

        _logger.LogInformation("No-lock: Product {ProductId} quantity changed from {Old} to {New}", productId, originalQuantity, quantity);

        return Ok(new { productId, oldQuantity = originalQuantity, newQuantity = quantity, message = "Updated without lock — race condition possible!" });
    }

    [HttpPost("update-inventory-with-lock")]
    public async Task<IActionResult> UpdateInventoryWithLock(int productId, int quantity)
    {
        var lockKey = $"lock:inventory:{productId}";
        var lockValue = Guid.NewGuid().ToString();
        var expiry = TimeSpan.FromMilliseconds(5000);

        bool lockAcquired = await _redisDb.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);

        if (!lockAcquired)
            return Conflict(new { message = "Resource is locked by another process" });

        try
        {
            await Task.Delay(3000);

            var inventory = await _context.Inventories.FirstOrDefaultAsync(x => x.ProductId == productId);
            if (inventory is null)
                return NotFound(new { message = "Inventory not found" });

            var originalQuantity = inventory.Quantity;
            inventory.Quantity = quantity;

            Thread.Sleep(100);

            await _context.SaveChangesAsync();

            _logger.LogInformation("With-lock: Product {ProductId} quantity changed from {Old} to {New}", productId, originalQuantity, quantity);

            return Ok(new { productId, oldQuantity = originalQuantity, newQuantity = quantity, message = "Updated with distributed lock — safe!" });
        }
        finally
        {
            var script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    redis.call('DEL', KEYS[1])
                    return 1
                end
                return 0";
            await _redisDb.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
        }
    }
}
