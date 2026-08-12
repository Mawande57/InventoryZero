using System;

namespace InventoryZeroAPI.DTOs.Shops
{
    public class ShopProfileDto
    {
        public int Id { get; set; }
        public string ShopName { get; set; } = null!;
        public string? ShopDescription { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string Country { get; set; } = null!;
        public bool IsVerified { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Owner info — just enough, never expose email/password
        public string OwnerName { get; set; } = null!;
        public decimal OwnerRating { get; set; }
        public int OwnerTotalReviews { get; set; }
    }
}