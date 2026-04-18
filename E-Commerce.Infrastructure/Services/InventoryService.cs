using E_Commerce.Application.DTOs.Inventory;
using E_Commerce.Application.Interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Infrastructure.Services
{
     
     
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InventoryDto?> GetByProductIdAsync(int productId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.ProductId == productId)
                .Select(x => new InventoryDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    Quantity = x.Quantity
                })
                .FirstOrDefaultAsync();
        }

        public async Task<InventoryDto> UpdateAsync(int productId, UpdateInventoryRequest request)
        {
            if (request.Quantity < 0)
                throw new ArgumentException("Quantity cannot be negative.");

            var inventory = await _context.Inventories
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (inventory is null)
                throw new InvalidOperationException("Inventory not found.");

            inventory.Quantity = request.Quantity;

            await _context.SaveChangesAsync();

            return new InventoryDto
            {
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                Quantity = inventory.Quantity
            };
        }
    }
}
