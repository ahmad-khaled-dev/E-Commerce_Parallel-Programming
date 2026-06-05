using E_Commerce.Domain.Entities;
using E_Commerce.Domain.Enums;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Api.Controllers;

[ApiController]
[Route("api/transaction")]
public class TransactionDemoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TransactionDemoController> _logger;

    public TransactionDemoController(AppDbContext context, ILogger<TransactionDemoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("place-order-no-transaction")]
    public async Task<IActionResult> PlaceOrderNoTransaction(int userId, int productId, int quantity)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return NotFound(new { message = "Product not found" });

        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (inventory is null)
            return NotFound(new { message = "Inventory not found" });

        if (inventory.Quantity < quantity)
            return BadRequest(new { message = "Insufficient stock" });

        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            OrderStatus = OrderStatus.Pending,
            TotalAmount = product.Price * quantity
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Step 1: Order created (Id={OrderId})", order.Id);

        _context.OrderItems.Add(new OrderItem
        {
            OrderId = order.Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = product.Price
        });

        inventory.Quantity -= quantity;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Step 2: Inventory decreased, OrderItem added");

        if (productId % 2 == 0)
            throw new InvalidOperationException($"Step 3: Payment failed — order #{order.Id} and inventory already changed!");

        return Ok(new { orderId = order.Id, message = "Order placed successfully (no transaction)" });
    }

    [HttpPost("place-order-with-transaction")]
    public async Task<IActionResult> PlaceOrderWithTransaction(int userId, int productId, int quantity)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return NotFound(new { message = "Product not found" });

        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        if (inventory is null)
            return NotFound(new { message = "Inventory not found" });

        if (inventory.Quantity < quantity)
            return BadRequest(new { message = "Insufficient stock" });

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                TotalAmount = product.Price * quantity,
                Items = new HashSet<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    }
                }
            };

            _context.Orders.Add(order);

            inventory.Quantity -= quantity;

            await _context.SaveChangesAsync();

            if (productId % 2 == 0)
                throw new InvalidOperationException($"Payment failed — transaction rolled back for order #{order.Id}");

            await transaction.CommitAsync();

            return Ok(new { orderId = order.Id, message = "Order placed successfully (transaction committed)" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("rolled back"))
        {
            await transaction.RollbackAsync();
            return Conflict(new { message = "Payment failed — Transaction rolled back. Order and inventory were NOT changed. Data integrity preserved." });
        }
    }
}
