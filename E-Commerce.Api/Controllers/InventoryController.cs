using E_Commerce.Application.DTOs.Inventory;
using E_Commerce.Application.Interfaces; 
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{productId:int}")]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var inventory = await _inventoryService.GetByProductIdAsync(productId);

        if (inventory is null)
            return NotFound();

        return Ok(inventory);
    }

    [HttpPut("{productId:int}")]
    public async Task<IActionResult> Update(int productId, UpdateInventoryRequest request)
    {
        var inventory = await _inventoryService.UpdateAsync(productId, request);
        return Ok(inventory);
    }



    [HttpPost("{productId:int}/decrease-unsafe")]
    public async Task<IActionResult> DecreaseUnsafe(int productId, DecreaseInventoryRequest request)
    {
        var inventory = await _inventoryService.DecreaseUnsafeAsync(productId, request);
        return Ok(inventory);
    }

    [HttpPost("{productId:int}/decrease-safe")]
    public async Task<IActionResult> DecreaseSafe(int productId, DecreaseInventoryRequest request)
    {
        try
        {
            var inventory = await _inventoryService.DecreaseSafeAsync(productId, request);
            return Ok(inventory);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

 