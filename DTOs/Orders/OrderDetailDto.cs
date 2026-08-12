namespace InventoryZeroAPI.DTOs.Orders
{
    public class OrderDetailDto : OrderSummaryDto
    {
        public string ShippingAddressLine1 { get; set; } = null!;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingPostalCode { get; set; } = null!;
        public string ShippingPhoneNumber { get; set; } = null!;
        public string ShippingCountry { get; set; } = null!;
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
        public string? BuyerNotes { get; set; }
        public string? SellerNotes { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}