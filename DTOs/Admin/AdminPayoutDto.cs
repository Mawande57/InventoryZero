namespace InventoryZeroAPI.DTOs.Admin
{
    public class AdminPayoutDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public string ShopOwner { get; set; } = null!;
        public string OrderNumber { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? StripeTransferId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}