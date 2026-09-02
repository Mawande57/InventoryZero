using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Shops;
using InventoryZeroAPI.DTOs.Products;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InventoryZeroAPI.Services
{
    public class ShopService : IShopService
    {
        private readonly InventoryZeroDbContext _context;

        public ShopService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<ShopProfileDto?> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Shops
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new ShopProfileDto
                {
                    Id = s.Id,
                    ShopName = s.ShopName,
                    ShopDescription = s.ShopDescription,
                    LogoUrl = s.LogoUrl,
                    CoverImageUrl = s.CoverImageUrl,
                    City = s.City,
                    Province = s.Province,
                    Country = s.Country,
                    IsVerified = s.IsVerified,
                    TotalSales = s.TotalSales,
                    TotalRevenue = s.TotalRevenue,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    OwnerName = s.User.FullName,
                    OwnerRating = s.User.Rating,
                    OwnerTotalReviews = s.User.TotalReviews
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<ProductCardDto>> GetShopProductsAsync(int shopId)
        {
            if (shopId <= 0) return new List<ProductCardDto>();

            // EARLY RETURN: If no products exist
            var anyProducts = await _context.Products.AsNoTracking().AnyAsync();
            if (!anyProducts) return new List<ProductCardDto>();

            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.ShopId == shopId &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now &&
                    p.Shop != null &&           // ← FIXED: Added null check
                    p.Shop.IsVerified == true)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ShortDescription = p.ShortDescription,
                    OriginalPrice = p.OriginalPrice,
                    SalePrice = p.SalePrice,
                    DiscountPercentage = p.DiscountPercentage,
                    RemainingQuantity = p.Quantity - p.SoldQuantity,
                    Condition = p.Condition,
                    IsUrgent = p.IsUrgent,
                    ListingEndDate = p.ListingEndDate,
                    Status = p.Status,
                    ShopName = p.Shop.ShopName,
                    ShopOwnerId = p.Shop.UserId,
                    ShopCity = p.Shop.City,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    MainImageUrl = p.ProductImages
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages.Select(i => i.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();
        }
    }
}