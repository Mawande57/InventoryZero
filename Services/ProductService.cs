using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using System.Threading.Tasks;
using InventoryZeroAPI.DTOs.Products;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InventoryZeroAPI.Services
{
    public class ProductService : IProductService
    {
        private readonly InventoryZeroDbContext _context;

        public ProductService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResultDto<ProductCardDto>> GetAllAsync(ProductFilterDto filter)
        {
            // Start with base query — only active, admin approved, not expired
            // This is called building a query — nothing hits the DB yet
            var query = _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now);

            // Apply filters one by one if they were provided
            // Each Where() adds to the query — still no DB hit yet

            // Search by title
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p =>
                    p.Title.Contains(filter.Search));

            // Filter by category
            if (!string.IsNullOrEmpty(filter.CategorySlug))
                query = query.Where(p =>
                    p.Category != null &&
                    p.Category.Slug == filter.CategorySlug);

            // Filter by city — matches the shop's city
            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(p =>
                    p.Shop.City != null &&
                    p.Shop.City.Contains(filter.City));

            // Filter by price range
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.SalePrice >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.SalePrice <= filter.MaxPrice.Value);

            // Filter by condition
            if (!string.IsNullOrEmpty(filter.Condition))
                query = query.Where(p => p.Condition == filter.Condition);

            // Urgent deals only
            if (filter.IsUrgent.HasValue && filter.IsUrgent.Value)
                query = query.Where(p => p.IsUrgent);

            // Sorting — switch changes order based on what user picked
            query = filter.SortBy switch
            {
                "price-asc" => query.OrderBy(p => p.SalePrice),
                "price-desc" => query.OrderByDescending(p => p.SalePrice),
                "discount" => query.OrderByDescending(p => p.DiscountPercentage),
                "ending-soon" => query.OrderBy(p => p.ListingEndDate),
                _ => query.OrderByDescending(p => p.CreatedAt) // default: newest
            };

            // Count total BEFORE pagination — needed for TotalPages
            var totalCount = await query.CountAsync();

            // NOW hit the database — Skip and Take handle pagination
            // Page 1: skip 0, take 12
            // Page 2: skip 12, take 12
            // Page 3: skip 24, take 12
            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Map to DTO — never return raw model
            var items = products.Select(p => new ProductCardDto
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
                ShopLogoUrl = p.Shop.LogoUrl,
                CategoryName = p.Category?.Name,
                // Get the main image, fall back to first image if no main set
                MainImageUrl = p.ProductImages
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl
                    ?? p.ProductImages.FirstOrDefault()?.ImageUrl
            }).ToList();

            return new PagedResultDto<ProductCardDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ProductDetailDto?> GetBySlugAsync(string slug)
        {
            // Increment views first — fire and forget
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages.OrderBy(i => i.SortOrder))
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p =>
                    p.Slug == slug &&
                    p.Status == "Active" &&
                    p.AdminApproved);

            if (product == null) return null;

            // Increment view count
            product.Views++;
            await _context.SaveChangesAsync();

            // Calculate average rating from reviews
            var avgRating = product.Reviews.Any()
                ? product.Reviews.Average(r => r.Rating)
                : 0;

            return new ProductDetailDto
            {
                Id = product.Id,
                Title = product.Title,
                Slug = product.Slug,
                Description = product.Description,
                ShortDescription = product.ShortDescription,
                OriginalPrice = product.OriginalPrice,
                SalePrice = product.SalePrice,
                DiscountPercentage = product.DiscountPercentage,
                Quantity = product.Quantity,
                SoldQuantity = product.SoldQuantity,
                RemainingQuantity = product.Quantity - product.SoldQuantity,
                Condition = product.Condition,
                ConditionNotes = product.ConditionNotes,
                Weight = product.Weight,
                IsUrgent = product.IsUrgent,
                ListingEndDate = product.ListingEndDate,
                ExpiryDate = product.ExpiryDate,
                Views = product.Views,
                Saves = product.Saves,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                ImageUrls = product.ProductImages
                    .Select(i => i.ImageUrl)
                    .ToList(),
                ShopId = product.Shop.Id,
                ShopName = product.Shop.ShopName,
                ShopCity = product.Shop.City,
                ShopLogoUrl = product.Shop.LogoUrl,
                ShopRating = product.Shop.TotalRevenue,
                ShopIsVerified = product.Shop.IsVerified,
                CategoryName = product.Category?.Name,
                CategorySlug = product.Category?.Slug,
                TotalReviews = product.Reviews.Count,
                AverageRating = (decimal)avgRating
            };
        }

        public async Task<List<ProductCardDto>> GetByCategoryAsync(string categorySlug)
        {
            var products = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.Category != null &&
                    p.Category.Slug == categorySlug &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now)
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
                ShopLogoUrl = p.Shop.LogoUrl,
                CategoryName = p.Category?.Name,
                MainImageUrl = p.ProductImages
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl
                    ?? p.ProductImages.FirstOrDefault()?.ImageUrl
            }).ToList();
        }

        public async Task<List<ProductCardDto>> GetByShopAsync(int shopId)
        {
            var products = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p =>
                    p.ShopId == shopId &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now)
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