using E_Commerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User { UserName = "ahmad", Email = "ahmad@example.com", PasswordHash = "123456" },
                new User { UserName = "Ali", Email = "ali@example.com", PasswordHash = "123456" },
                new User { UserName = "Issa", Email = "issa@example.com", PasswordHash = "123456" },
                new User { UserName = "Mohammed", Email = "mohammed@example.com", PasswordHash = "123456" }
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
                    Inventory = new Inventory { Quantity = 2 }
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

        await ReseedCartsAsync(context);
    }

    private static async Task ReseedCartsAsync(AppDbContext context)
    {
        var carts = await context.Carts
            .Include(c => c.CartItems)
            .ToListAsync();

        if (carts.Any())
        {
            var allCartItems = carts.SelectMany(c => c.CartItems).ToList();

            if (allCartItems.Any())
                context.CartItems.RemoveRange(allCartItems);

            context.Carts.RemoveRange(carts);
            await context.SaveChangesAsync();
        }

        var users = await context.Users
            .OrderBy(u => u.Id)
            .Take(4)
            .ToListAsync();

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == 1);
        if (product is null)
            return;

        var newCarts = users.Select(user => new Cart
        {
            UserId = user.Id,
            CartItems = new List<CartItem>
            {
                new CartItem
                {
                    ProductId = product.Id,
                    Quantity = 1
                }
            }
        }).ToList();

        await context.Carts.AddRangeAsync(newCarts);
        await context.SaveChangesAsync();
    }
}