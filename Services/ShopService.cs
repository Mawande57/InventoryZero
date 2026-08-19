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
            var shop = await _context.Shops
                .Include(s => s.User)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.Status == "Active" &&
                    s.IsVerified == true);

            if (shop == null) return null;

            return new ShopProfileDto
            {
                Id = shop.Id,
                ShopName = shop.ShopName,
                ShopDescription = shop.ShopDescription,
                LogoUrl = shop.LogoUrl,
                CoverImageUrl = shop.CoverImageUrl,
                City = shop.City,
                Province = shop.Province,
                Country = shop.Country,
                IsVerified = shop.IsVerified,
                TotalSales = shop.TotalSales,
                TotalRevenue = shop.TotalRevenue,
                Status = shop.Status,
                CreatedAt = shop.CreatedAt,
                OwnerName = shop.User.FullName,
                OwnerRating = shop.User.Rating,
                OwnerTotalReviews = shop.User.TotalReviews
            };
        }

        public async Task<List<ProductCardDto>> GetShopProductsAsync(int shopId)
        {
            var products = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.ShopId == shopId &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now &&
                    p.Shop.IsVerified == true)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return products.Select(p => new ProductCardDto
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
                ShopCity = p.Shop.City,
                CategoryName = p.Category?.Name,
                MainImageUrl = p.ProductImages
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl
                    ?? p.ProductImages.FirstOrDefault()?.ImageUrl
            }).ToList();
        }
    }
}