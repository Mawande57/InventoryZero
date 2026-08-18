namespace InventoryZeroAPI.DTOs.Admin
{
    public class AdminOrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SellerPayout { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BuyerName { get; set; } = null!;
        public string BuyerEmail { get; set; } = null!;
        public string ShopName { get; set; } = null!;
        public string ShippingCity { get; set; } = null!;
        public string ShippingProvince { get; set; } = null!;
        public string? TrackingNumber { get; set; }
    }
}