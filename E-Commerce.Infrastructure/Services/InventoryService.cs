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


            await Task.Delay(3000);

            await _context.SaveChangesAsync();




            return new InventoryDto
            {
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                Quantity = inventory.Quantity
            };
        }



        public async Task<InventoryDto> DecreaseUnsafeAsync(int productId, DecreaseInventoryRequest request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            var inventory = await _context.Inventories
                .AsNoTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (inventory is null)
                throw new InvalidOperationException("Inventory not found.");

            if (inventory.Quantity < request.Amount)
                throw new InvalidOperationException("Insufficient stock.");

            var newQuantity = inventory.Quantity - request.Amount;

            await Task.Delay(3000);

            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE Inventories
        SET Quantity = {newQuantity}
        WHERE ProductId = {productId}");

            if (affectedRows == 0)
                throw new InvalidOperationException("Inventory update failed.");

            return new InventoryDto
            {
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                Quantity = newQuantity
            };
        }

        public async Task<InventoryDto> DecreaseSafeAsync(int productId, DecreaseInventoryRequest request)
        {
            if (request.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");

            var inventory = await _context.Inventories
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.ProductId == productId);

            if (inventory is null)
                throw new InvalidOperationException("Inventory not found.");

            if (inventory.Quantity < request.Amount)
                throw new InvalidOperationException("Insufficient stock.");

            inventory.Quantity -= request.Amount;

            await Task.Delay(3000);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("Concurrency conflict occurred. Please retry.");
            }

            return new InventoryDto
            {
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                Quantity = inventory.Quantity
            };
        }

    }
}


