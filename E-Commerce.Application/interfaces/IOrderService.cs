using E_Commerce.Application.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Application.interfaces
{
 
    public interface IOrderService
    {
        Task<OrderDto> CheckoutAsync(CheckoutRequest request);
        Task<OrderDto?> GetByIdAsync(int orderId);
    }
}
