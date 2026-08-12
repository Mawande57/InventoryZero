using System;

namespace InventoryZeroAPI.DTOs.Products
{
    public class ProductCardDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int RemainingQuantity { get; set; }
        public string Condition { get; set; } = null!;
        public string? MainImageUrl { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime ListingEndDate { get; set; }
        public string Status { get; set; } = null!;

        // Shop info — buyer needs to know who's selling
        public string ShopName { get; set; } = null!;
        public string? ShopCity { get; set; }
        public string? ShopLogoUrl { get; set; }

        // Category
        public string? CategoryName { get; set; }
    }
}