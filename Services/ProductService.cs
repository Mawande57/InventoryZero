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
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (filter.Page < 1) throw new ArgumentOutOfRangeException(nameof(filter), "Page must be 1 or greater.");
            if (filter.PageSize < 1) throw new ArgumentOutOfRangeException(nameof(filter), "Page size must be 1 or greater.");

            // Start with base query — only active, admin approved, not expired
            // This is called building a query — nothing hits the DB yet.
            //
            // No Include() here: the final Select() further down projects straight to
            // ProductCardDto, which only ever needs ShopName/City/LogoUrl, CategoryName,
            // and one image URL. Pulling the full Shop/Category/ProductImages graph via
            // Include for every product on a paginated listing page - just to throw away
            // everything except a handful of scalar fields - was the biggest cost in this
            // method, especially the images: a product with a dozen photos would join and
            // materialize all of them just so we could pick one.
            var query = _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now &&
                    p.Shop != null &&
                    p.Shop.IsVerified == true);

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
            //
            // Projecting to the DTO here (instead of loading Product entities and
            // mapping afterward) means MainImageUrl is resolved as a small correlated
            // subquery per product instead of a full join against ProductImages.
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ShopOwnerId = p.Shop.UserId,
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
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    // Get the main image, fall back to first image if no main set
                    MainImageUrl = p.ProductImages
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages.Select(i => i.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();

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
            if (string.IsNullOrWhiteSpace(slug)) return null;

            // This one stays tracked (default, no AsNoTracking) because we increment
            // Views on the loaded entity and save it below.
            //
            // Two collection Includes here (ProductImages and Reviews) - loaded as a
            // single query, that shape duplicates every scalar Product/Shop/Category
            // column once per image-times-review combination (a product with 15 photos
            // and 40 reviews would come back as 600 rows). AsSplitQuery() runs it as
            // separate queries instead, which avoids that multiplication entirely.
            var product = await _context.Products
                .AsSplitQuery()
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages.OrderBy(i => i.SortOrder))
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p =>
                    p.Slug == slug &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.Shop != null &&
                    p.Shop.IsVerified == true);

            if (product == null) return null;

            product.Views++;
            await _context.SaveChangesAsync();

            var avgRating = product.Reviews.Any()
                ? product.Reviews.Average(r => r.Rating)
                : 0;

            var imageUrls = product.ProductImages
                .Select(i => i.ImageUrl)
                .ToList();

            return new ProductDetailDto
            {
                Id = product.Id,
                Title = product.Title,
                Slug = product.Slug,
                ShopOwnerId = product.Shop.UserId,
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
                ImageUrls = imageUrls,
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
            if (string.IsNullOrWhiteSpace(categorySlug)) return new List<ProductCardDto>();

            // Same reasoning as GetAllAsync: project straight to the DTO instead of
            // Include-ing the full Shop/Category/ProductImages graph for every product.
            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.Category != null &&
                    p.Category.Slug == categorySlug &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now &&
                    p.Shop != null &&
                    p.Shop.IsVerified == true)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ShopOwnerId = p.Shop.UserId,
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
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    MainImageUrl = p.ProductImages
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? p.ProductImages.Select(i => i.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<List<ProductCardDto>> GetByShopAsync(int shopId)
        {
            if (shopId <= 0) return new List<ProductCardDto>();

            // Same reasoning as GetAllAsync. Note: ShopLogoUrl isn't populated here,
            // same as the original - left that as-is since it changes what's returned,
            // not how it's fetched.
            return await _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.ShopId == shopId &&
                    p.Status == "Active" &&
                    p.AdminApproved &&
                    p.ListingEndDate > DateTime.Now &&
                    p.Shop != null &&
                    p.Shop.IsVerified == true)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    ShopOwnerId = p.Shop.UserId,
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