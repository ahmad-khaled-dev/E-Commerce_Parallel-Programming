using E_Commerce.Application.DTOs.Products.Products;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
 
namespace E_Commerce.Infrastructure.Services
{
    public class ProductService : IProductService
    {

        private readonly AppDbContext _context;
        private static readonly SemaphoreSlim _getAllProductsSemaphore = new SemaphoreSlim(3, 3);
        public ProductService(AppDbContext context)
        {
            _context = context; 
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {
            await _getAllProductsSemaphore.WaitAsync();

            try
            {
                await Task.Delay(6000);

                return await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Inventory)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        IsActive = p.IsActive,
                        Quantity = p.Inventory != null ? p.Inventory.Quantity : 0
                    })
                    .ToListAsync();
            }
            finally
            {
                _getAllProductsSemaphore.Release();
            }

        }
        public async Task<ProductDto?> GetByIdAsync(int id)
        {

         return  await    _context.Products
                .AsNoTracking()
                .Where(p => p.Id == id)

                 .Select(p => new ProductDto
                 {
                     Id = p.Id,
                     Name = p.Name,
                     Description = p.Description,
                     IsActive = p.IsActive,
                     Price = p.Price,
                     Quantity = p.Inventory != null ? p.Inventory.Quantity : 0,
                 })
                 .FirstOrDefaultAsync();
           
        }
    }
}
