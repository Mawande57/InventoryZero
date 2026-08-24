namespace InventoryZeroAPI.DTOs.Seller
{
    public class CreateProductDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; } = "New";
        public int ShopId { get; set; }
        public int? CategoryId { get; set; }
        public bool IsUrgent { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
