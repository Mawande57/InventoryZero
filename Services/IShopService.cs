using InventoryZeroAPI.DTOs.Products;
using InventoryZeroAPI.DTOs.Shops;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public interface IShopService
    {
        Task<ShopProfileDto?> GetByIdAsync(int id);
        Task<List<ProductCardDto>> GetShopProductsAsync(int shopId);
    }
}