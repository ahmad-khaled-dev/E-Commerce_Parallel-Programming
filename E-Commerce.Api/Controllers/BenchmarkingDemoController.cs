using E_Commerce.Domain.Enums;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/benchmarking")]
public class BenchmarkingDemoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public BenchmarkingDemoController(AppDbContext context, IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _context = context;
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet("sequential-orders")]
    public async Task<IActionResult> SequentialOrders(int count = 50)
    {
        var sw = Stopwatch.StartNew();

        var orders = new List<object>();
        for (int i = 1; i <= count; i++)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == i)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.CreatedAt,
                    o.TotalAmount,
                    Status = o.OrderStatus.ToString(),
                    Items = o.Items.Select(item => new
                    {
                        item.ProductId,
                        ProductName = item.Product.Name,
                        item.Quantity,
                        item.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            orders.Add(order ?? (object)new { id = i, notFound = true });
        }

        sw.Stop();

        return Ok(new
        {
            totalTimeMs = sw.ElapsedMilliseconds,
            ordersFetched = count,
            method = "Sequential (bottleneck)",
            data = orders
        });
    }

    [HttpGet("parallel-orders")]
    public async Task<IActionResult> ParallelOrders(int count = 50)
    {
        var swSequential = Stopwatch.StartNew();

        for (int i = 1; i <= count; i++)
        {
            await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == i)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.CreatedAt,
                    o.TotalAmount,
                    Status = o.OrderStatus.ToString(),
                    Items = o.Items.Select(item => new
                    {
                        item.ProductId,
                        ProductName = item.Product.Name,
                        item.Quantity,
                        item.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        swSequential.Stop();
        var sequentialTime = swSequential.ElapsedMilliseconds;

        var swParallel = Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, count).Select(async i =>
        {
            await using var ctx = _dbContextFactory.CreateDbContext();
            return await ctx.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == i)
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.CreatedAt,
                    o.TotalAmount,
                    Status = o.OrderStatus.ToString(),
                    Items = o.Items.Select(item => new
                    {
                        item.ProductId,
                        ProductName = item.Product.Name,
                        item.Quantity,
                        item.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        });

        var results = await Task.WhenAll(tasks);

        swParallel.Stop();
        var parallelTime = swParallel.ElapsedMilliseconds;
        var speedup = parallelTime > 0 ? (double)sequentialTime / parallelTime : 0;

        return Ok(new
        {
            sequentialTimeMs = sequentialTime,
            parallelTimeMs = parallelTime,
            speedupFactor = Math.Round(speedup, 2),
            ordersFetched = count,
            method = "Parallel (fix)",
            data = results.Select((r, idx) => (object?)r ?? new { id = idx + 1, notFound = true }).ToList()
        });
    }
}
