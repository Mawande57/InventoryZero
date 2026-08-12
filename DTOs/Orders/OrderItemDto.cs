namespace InventoryZeroAPI.DTOs.Orders
{
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}