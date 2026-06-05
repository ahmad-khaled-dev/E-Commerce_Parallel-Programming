using E_Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/caching")]
public class CachingDemoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingDemoController> _logger;

    public CachingDemoController(AppDbContext context, IDistributedCache cache, ILogger<CachingDemoController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("products-no-cache")]
    public async Task<IActionResult> GetProductsNoCache()
    {
        var sw = Stopwatch.StartNew();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Inventory)
            .OrderBy(p => p.Id)
            .Take(10)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.IsActive,
                Quantity = p.Inventory != null ? p.Inventory.Quantity : 0
            })
            .ToListAsync();

        sw.Stop();

        _logger.LogInformation("No-cache fetched {Count} products in {Elapsed}ms", products.Count, sw.ElapsedMilliseconds);

        return Ok(new { products, elapsedMs = sw.ElapsedMilliseconds, source = "Database" });
    }

    [HttpGet("products-with-cache")]
    public async Task<IActionResult> GetProductsWithCache()
    {
        var sw = Stopwatch.StartNew();
        const string cacheKey = "top-products";

        var cached = await _cache.GetStringAsync(cacheKey);

        if (cached is not null)
        {
            sw.Stop();
            var products = JsonSerializer.Deserialize<object>(cached);
            _logger.LogInformation("Cache HIT — fetched from Redis in {Elapsed}ms", sw.ElapsedMilliseconds);
            return Ok(new { products, elapsedMs = sw.ElapsedMilliseconds, source = "Cache (HIT)" });
        }

        var productsFromDb = await _context.Products
            .AsNoTracking()
            .Include(p => p.Inventory)
            .OrderBy(p => p.Id)
            .Take(10)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.IsActive,
                Quantity = p.Inventory != null ? p.Inventory.Quantity : 0
            })
            .ToListAsync();

        var json = JsonSerializer.Serialize(productsFromDb);

        await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        });

        sw.Stop();

        _logger.LogInformation("Cache MISS — fetched from DB in {Elapsed}ms, stored in Redis", sw.ElapsedMilliseconds);

        return Ok(new { products = productsFromDb, elapsedMs = sw.ElapsedMilliseconds, source = "Database (MISS)" });
    }
}
