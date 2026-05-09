using Microsoft.AspNetCore.Mvc;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence; // لكي يتعرف على AppDbContext
using E_Commerce.Domain.Entities;      // لكي يتعرف على كلاس Order
using E_Commerce.Domain.Enums;
namespace E_Commerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BatchProcessingDemoController : ControllerBase
    {
        private readonly IBatchSalesProcessor _processor;
        private readonly AppDbContext _context; // أضفنا هذا السطر

        // قمنا بتعديل الـ Constructor لاستقبال الـ context
        public BatchProcessingDemoController(IBatchSalesProcessor processor, AppDbContext context)
        {
            _processor = processor;
            _context = context;
        }

        [HttpPost("seed-orders")]
        public async Task<IActionResult> SeedOrders([FromQuery] int count = 100)
        {
            // نتحقق من وجود مستخدم افتراضي أو نستخدم UserId = 1 كما في الـ Seeder
            var orders = new List<Order>();
            for (int i = 0; i < count; i++)
            {
                orders.Add(new Order
                {
                    UserId = 1,
                    CreatedAt = DateTime.Now,
                    TotalAmount = (i + 1) * 15.5m,
                    OrderStatus = (OrderStatus)1 // Completed
                });
            }

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{count} orders seeded successfully!" });
        }

        [HttpGet("without-batching")]
        public async Task<IActionResult> WithoutBatching([FromQuery] DateTime? date)
        {
            var targetDate = date ?? DateTime.UtcNow;
            var report = await _processor.ProcessWithoutBatchingAsync(targetDate);
            return Ok(report);
        }

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