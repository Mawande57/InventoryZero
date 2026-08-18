// Services/IAdminService.cs
using InventoryZeroAPI.DTOs.Admin;
using InventoryZeroAPI.DTOs.Products;

namespace InventoryZeroAPI.Services
{
    public interface IAdminService
    {
        Task<AdminStatsDto> GetStatsAsync();

        // Shops
        Task<PagedResultDto<AdminShopDto>> GetShopsAsync(string? status, int page, int pageSize);
        Task<AdminShopDto?> GetShopDetailAsync(int id);
        Task ApproveShopAsync(int shopId, int adminId, ShopApprovalDto dto);
        Task RejectShopAsync(int shopId, int adminId, string reason);

        // Users
        Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? role, int page, int pageSize);
        Task ToggleUserStatusAsync(int userId);
        Task ChangeUserRoleAsync(int userId, string role);

        // Products
        Task<PagedResultDto<AdminProductDto>> GetProductsAsync(string? status, int page, int pageSize);
        Task ToggleProductStatusAsync(int productId);

        // Orders
        Task<PagedResultDto<AdminOrderDto>> GetOrdersAsync(string? status, int page, int pageSize);

        // Payouts
        Task<PagedResultDto<AdminPayoutDto>> GetPayoutsAsync(string? status, int page, int pageSize);
        Task<object> ProcessPendingPayoutsAsync();
    }
}