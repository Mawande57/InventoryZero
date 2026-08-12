using InventoryZeroAPI.DTOs.Products;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductCardDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductDetailDto?> GetBySlugAsync(string slug);
        Task<List<ProductCardDto>> GetByCategoryAsync(string categorySlug);
        Task<List<ProductCardDto>> GetByShopAsync(int shopId);
    }
}