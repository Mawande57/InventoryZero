namespace InventoryZeroAPI.DTOs.Admin
{
    public class AdminProductDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public int SoldQuantity { get; set; }
        public string Status { get; set; } = null!;
        public bool AdminApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ShopName { get; set; } = null!;
        public int ShopId { get; set; }
        public string? CategoryName { get; set; }
        public int Views { get; set; }
        public int Saves { get; set; }
    }
}
