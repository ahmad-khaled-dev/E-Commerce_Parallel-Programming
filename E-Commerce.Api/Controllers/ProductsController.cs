using E_Commerce.Application.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce.Api.Controllers
{
     
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;


        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }





        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);

            if (product is null)
                return NotFound();

            return Ok(product);
        }
    }
}
