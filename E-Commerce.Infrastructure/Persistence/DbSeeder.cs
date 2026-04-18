using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.Persistence;
 
namespace E_Commerce.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User
                {
                    UserName = "ahmad",
                    Email = "ahmad@example.com",
                    PasswordHash = "123456"
                },
                new User
                {
                    UserName = "sara",
                    Email = "sara@example.com",
                    PasswordHash = "123456"
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Laptop",
                    Description = "High performance laptop",
                    Price = 1200,
                    IsActive = true,
                    Inventory = new Inventory { Quantity = 10 }
                },
                new Product
                {
                    Name = "Mouse",
                    Description = "Wireless mouse",
                    Price = 25,
                    IsActive = true,
                    Inventory = new Inventory { Quantity = 50 }
                },
                new Product
                {
                    Name = "Keyboard",
                    Description = "Mechanical keyboard",
                    Price = 80,
                    IsActive = true,
                    Inventory = new Inventory { Quantity = 20 }
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}