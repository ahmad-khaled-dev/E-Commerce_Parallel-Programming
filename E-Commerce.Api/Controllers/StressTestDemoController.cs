using E_Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/stress-test")]
public class StressTestDemoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<StressTestDemoController> _logger;

    public StressTestDemoController(AppDbContext context, ILogger<StressTestDemoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("inventory-no-lock")]
    public async Task<IActionResult> InventoryNoLock(int productId, int users = 100)
    {
        var initial = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId);
        if (initial is null)
            return NotFound(new { message = "Inventory not found" });

        var startQuantity = initial.Quantity;
        var successCount = 0;
        var failCount = 0;

        var tasks = Enumerable.Range(0, users).Select(async _ =>
        {
            try
            {
                var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
                if (inv is null) return;

                inv.Quantity += 1;
                await Task.Delay(10);
                await _context.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
            }
            catch
            {
                Interlocked.Increment(ref failCount);
            }
        });

        await Task.WhenAll(tasks);

        var final = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId);

        _logger.LogInformation(
            "No-lock stress test: productId={Id}, start={Start}, final={Final}, success={Success}, fail={Fail}",
            productId, startQuantity, final?.Quantity, successCount, failCount);

        return Ok(new
        {
            productId,
            startQuantity,
            finalQuantity = final?.Quantity,
            expectedQuantity = startQuantity + users,
            successCount,
            failCount,
            totalUsers = users,
            message = "No lock used — data corruption is likely!"
        });
    }

    [HttpGet("inventory-with-lock")]
    public async Task<IActionResult> InventoryWithLock(int productId, int users = 100)
    {
        var initial = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId);
        if (initial is null)
            return NotFound(new { message = "Inventory not found" });

        var startQuantity = initial.Quantity;
        var successCount = 0;
        var failCount = 0;
        var semaphore = new SemaphoreSlim(1, 1);

        var tasks = Enumerable.Range(0, users).Select(async _ =>
        {
            await semaphore.WaitAsync();
            try
            {
                var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
                if (inv is null) return;

                inv.Quantity += 1;
                await Task.Delay(10);
                await _context.SaveChangesAsync();
                Interlocked.Increment(ref successCount);
            }
            catch
            {
                Interlocked.Increment(ref failCount);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var final = await _context.Inventories.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId);

        _logger.LogInformation(
            "With-lock stress test: productId={Id}, start={Start}, final={Final}, success={Success}, fail={Fail}",
            productId, startQuantity, final?.Quantity, successCount, failCount);

        return Ok(new
        {
            productId,
            startQuantity,
            finalQuantity = final?.Quantity,
            expectedQuantity = startQuantity + users,
            successCount,
            failCount,
            totalUsers = users,
            message = "SemaphoreSlim serialized access — data integrity preserved!"
        });
    }
}
