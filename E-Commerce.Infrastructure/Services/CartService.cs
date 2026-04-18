using E_Commerce.Application.DTOs.Cart;
using E_Commerce.Application.interfaces;
using E_Commerce.Domain.Entities;
using E_Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Infrastructure.Services
{ 
    public class CartService : ICartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddItemAsync(AddCartItemRequest request)
        {
            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");

            var userExists = await _context.Users.AnyAsync(x => x.Id == request.UserId);
            if (!userExists)
                throw new InvalidOperationException("User not found.");

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ProductId && x.IsActive);

            if (product is null)
                throw new InvalidOperationException("Product not found.");

            var cart = await _context.Carts
                .Include(x => x.CartItems)
                .FirstOrDefaultAsync(x => x.UserId == request.UserId);






            if (cart is null)
            {
                cart = new Cart
                {
                    UserId = request.UserId
                };

                await _context.Carts.AddAsync(cart);
            }

            var existingItem = cart.CartItems.FirstOrDefault(x => x.ProductId == request.ProductId);

            if (existingItem is null)
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }
            else
            {
                existingItem.Quantity += request.Quantity;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<CartDto?> GetCartAsync(int userId)
        {
            var cart = await _context.Carts
                .AsNoTracking()
                .Include(x => x.CartItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);




            if (cart is null)
                return null;

            return new CartDto
            {
                CartId = cart.Id,
                UserId = cart.UserId,
                Items = cart.CartItems.Select(x => new CartItemDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    UnitPrice = x.Product.Price,
                    Quantity = x.Quantity
                }).ToList()
            };
        }
    }
}
