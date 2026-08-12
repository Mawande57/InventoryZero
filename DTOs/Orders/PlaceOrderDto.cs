namespace InventoryZeroAPI.DTOs.Orders
{
    public class PlaceOrderDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        // Shipping address
        public string ShippingAddressLine1 { get; set; } = null!;
        public string? ShippingAddressLine2 { get; set; }
        public string ShippingCity { get; set; } = null!;
        public string ShippingProvince { get; set; } = null!;
        public string ShippingPostalCode { get; set; } = null!;
        public string ShippingPhoneNumber { get; set; } = null!;
        public string? BuyerNotes { get; set; }

        // Optional — use saved address
        public int? SavedAddressId { get; set; }
    }
}