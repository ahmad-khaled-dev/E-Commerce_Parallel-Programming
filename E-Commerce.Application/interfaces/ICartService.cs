using E_Commerce.Application.DTOs.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Application.interfaces
{
 
    public interface ICartService
    {
        Task AddItemAsync(AddCartItemRequest request);
        Task<CartDto?> GetCartAsync(int userId);
    }
}
