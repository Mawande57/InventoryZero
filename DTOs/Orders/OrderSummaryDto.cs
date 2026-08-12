namespace InventoryZeroAPI.DTOs.Orders
{
    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal SellerPayout { get; set; }
        public DateTime CreatedAt { get; set; }

        // Product info
        public string ProductTitle { get; set; } = null!;
        public string ProductSlug { get; set; } = null!;
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Shop info
        public string ShopName { get; set; } = null!;
        public int ShopId { get; set; }

        // Shipping
        public string ShippingCity { get; set; } = null!;
        public string ShippingProvince { get; set; } = null!;
        public string? TrackingNumber { get; set; }
        public string? TrackingCarrier { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}