using InventoryZeroAPI.DTOs.SavedProducts;

namespace InventoryZeroAPI.Services
{
    public interface ISavedProductService
    {
        Task<List<SavedProductDto>> GetSavedAsync(int userId);
        Task SaveAsync(int userId, int productId);
        Task UnsaveAsync(int userId, int productId);
    }
}