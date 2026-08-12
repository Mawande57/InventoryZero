using InventoryZeroAPI.DTOs.Orders;

namespace InventoryZeroAPI.Services
{
    public interface IOrderService
    {
        Task<OrderDetailDto> PlaceOrderAsync(int buyerId, PlaceOrderDto dto);
        Task<List<OrderSummaryDto>> GetMyOrdersAsync(int buyerId);
        Task<OrderDetailDto?> GetOrderDetailAsync(int orderId, int buyerId);
    }
}