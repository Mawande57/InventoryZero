// Services/ISellerService.cs
using InventoryZeroAPI.DTOs.Seller;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.DTOs.Products;
using InventoryZeroAPI.DTOs.Shops;

namespace InventoryZeroAPI.Services
{
    public interface ISellerService
    {
        Task<object> GetStatsAsync(int userId);
        Task<List<OrderSummaryDto>> GetOrdersAsync(int userId, string? status);
        Task UpdateOrderStatusAsync(int orderId, int userId, string status, string? trackingNumber);
        Task CancelOrderAsync(int orderId, int userId);
        Task<List<ProductCardDto>> GetProductsAsync(int userId);
        Task<object> CreateProductAsync(int userId, CreateProductDto dto);
        Task UpdateProductAsync(int productId, int userId, CreateProductDto dto);
        Task DeleteProductAsync(int productId, int userId);
        Task<List<ShopProfileDto>> GetShopsAsync(int userId);
        Task<object> CreateShopAsync(int userId, CreateShopDto dto);
        Task<List<PayoutDto>> GetPayoutsAsync(int userId);
    }
}