using E_Commerce.Application.DTOs.Cart;
using E_Commerce.Application.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers
{
     
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem(AddCartItemRequest request)
        {
            await _cartService.AddItemAsync(request);
            return Ok(new { message = "Item added to cart successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] int userId)
        {
            var cart = await _cartService.GetCartAsync(userId);

            if (cart is null)
                return NotFound(new { message = "Cart not found." });

            return Ok(cart);
        }
    }
}
