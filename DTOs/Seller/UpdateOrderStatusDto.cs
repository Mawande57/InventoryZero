// DTOs/Seller/UpdateOrderStatusDto.cs
namespace InventoryZeroAPI.DTOs.Seller
{
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = null!;
        public string? TrackingNumber { get; set; }
    }
}

