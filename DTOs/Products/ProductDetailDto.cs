using System;
using System.Collections.Generic;

namespace InventoryZeroAPI.DTOs.Products
{
    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int Quantity { get; set; }
        public int SoldQuantity { get; set; }
        public int RemainingQuantity { get; set; }
        public string Condition { get; set; } = null!;
        public string? ConditionNotes { get; set; }
        public decimal? Weight { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime ListingEndDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int Views { get; set; }
        public int Saves { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // All images for the modal gallery
        public List<string> ImageUrls { get; set; } = new();

        // Full shop info
        public int ShopId { get; set; }
        public int ShopOwnerId { get; set; }  // ✅ Add this

        public string ShopName { get; set; } = null!;
        public string? ShopCity { get; set; }
        public string? ShopLogoUrl { get; set; }
        public decimal ShopRating { get; set; }
        public bool ShopIsVerified { get; set; }

        // Category
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        // Review summary
        public int TotalReviews { get; set; }
        public decimal AverageRating { get; set; }
    }
}