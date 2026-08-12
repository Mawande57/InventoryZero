namespace InventoryZeroAPI.DTOs.SavedProducts
{
    public class SavedProductDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public string? MainImageUrl { get; set; }
        public string ShopName { get; set; } = null!;
        public string? ShopCity { get; set; }
        public string? CategoryName { get; set; }
        public int RemainingQuantity { get; set; }
        public DateTime ListingEndDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime SavedAt { get; set; }
    }
}