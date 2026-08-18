// DTOs/Seller/PayoutDto.cs
namespace InventoryZeroAPI.DTOs.Seller
{
    public class PayoutDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public string OrderNumber { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}