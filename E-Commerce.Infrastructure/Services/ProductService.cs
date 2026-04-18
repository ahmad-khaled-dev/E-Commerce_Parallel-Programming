using E_Commerce.Application.DTOs.Products.Products;
using E_Commerce.Application.interfaces;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
 
namespace E_Commerce.Infrastructure.Services
{
    public class ProductService : IProductService
    {

        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductDto>> GetAllAsync()
        {

            return await _context.Products
                 .AsNoTracking()
                 .Include(p => p.Inventory)
                 .Select(p => new ProductDto
                 {
                     Id = p.Id,
                     Name = p.Name,
                     Description= p.Description,
                     IsActive= p.IsActive,
                     Price = p.Price,
                     Quantity=p.Inventory !=null ?p.Inventory.Quantity : 0,
                 }).ToListAsync();
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
