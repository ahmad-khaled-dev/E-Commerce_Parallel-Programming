using E_Commerce.Application.DTOs.Cart;
using E_Commerce.Application.DTOs.Order;
using E_Commerce.Application.interfaces;
using E_Commerce.Domain.Entities;
using E_Commerce.Domain.Enums;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace E_Commerce.Infrastructure.Services
{

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDto> CheckoutAsync(CheckoutRequest request)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId);

            if (cart is null)
                throw new InvalidOperationException("Cart not found.");

            if (!cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            var productIds = cart.CartItems.Select(i => i.ProductId).ToList();

            var inventories = await _context.Inventories
                .Where(i => productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId);

            foreach (var item in cart.CartItems)
            {
                if (!inventories.TryGetValue(item.ProductId, out var inventory))
                    throw new InvalidOperationException($"Inventory not found for product {item.ProductId}.");

                if (inventory.Quantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product '{item.Product.Name}'.");
            }

            Console.WriteLine($"BEFORE SAVE: User {request.UserId}, Time = {DateTime.Now}");

            await Task.Delay(3000);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Paid,
                    TotalAmount = 0m
                };

                foreach (var cartItem in cart.CartItems)
                {
                    var inventory = inventories[cartItem.ProductId];
                    inventory.Quantity -= cartItem.Quantity;

                    order.Items.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.Price
                    });
                }

                order.TotalAmount = order.Items.Sum(x => x.UnitPrice * x.Quantity);

                await _context.Orders.AddAsync(order);

                _context.CartItems.RemoveRange(cart.CartItems);

                Console.WriteLine($"ORDER CREATED: User {request.UserId}, OrderId = {order.Id}, Time = {DateTime.Now}");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                return await GetByIdInternalAsync(order.Id)
                       ?? throw new InvalidOperationException("Order created but could not be loaded.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDto?> GetByIdAsync(int orderId)
        {
            return await GetByIdInternalAsync(orderId);
        }

        private async Task<OrderDto?> GetByIdInternalAsync(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == orderId)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Status = o.OrderStatus,
                    Items = o.Items.Select(i => new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
} 