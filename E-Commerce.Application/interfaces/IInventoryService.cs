using E_Commerce.Application.DTOs.Inventory;
 
namespace E_Commerce.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryDto?> GetByProductIdAsync(int productId);
    Task<InventoryDto> UpdateAsync(int productId, UpdateInventoryRequest request);
}