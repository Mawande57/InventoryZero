// Services/SellerService.cs
using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.DTOs.Products;
using InventoryZeroAPI.DTOs.Shops;
using InventoryZeroAPI.DTOs.Seller;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class SellerService : ISellerService
    {
        private readonly InventoryZeroDbContext _context;

        public SellerService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetStatsAsync(int userId)
        {
            if (userId <= 0) return new { Shops = 0, Products = 0, Orders = 0, Revenue = 0m };

            // Only the ids are needed for the downstream Contains() filters below,
            // and only counts/sums are needed for the response - loading full
            // Shop/Product/Order entity graphs (the OrderItems Include wasn't even
            // used anywhere in this method) just to call .Count()/.Sum() on them
            // in memory was pulling far more data than this dashboard needs.
            var shopIds = await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            var productCount = await _context.Products
                .CountAsync(p => shopIds.Contains(p.ShopId));

            var orderCount = await _context.Orders
                .CountAsync(o => shopIds.Contains(o.ShopId));

            var revenue = await _context.Orders
                .Where(o => shopIds.Contains(o.ShopId) &&
                    (o.OrderStatus == "Delivered" || o.OrderStatus == "Shipped"))
                .SumAsync(o => o.SellerPayout);

            return new
            {
                Shops = shopIds.Count,
                Products = productCount,
                Orders = orderCount,
                Revenue = revenue
            };
        }

        public async Task<List<OrderSummaryDto>> GetOrdersAsync(int userId, string? status)
        {
            if (userId <= 0) return new List<OrderSummaryDto>();

            var shopIds = await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            if (shopIds.Count == 0) return new List<OrderSummaryDto>();

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Shop)
                .Where(o => shopIds.Contains(o.ShopId));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                PlatformFee = o.PlatformFee,
                SellerPayout = o.SellerPayout,
                CreatedAt = o.CreatedAt,
                ProductTitle = o.OrderItems.FirstOrDefault()?.Product?.Title ?? "Product",
                ProductSlug = o.OrderItems.FirstOrDefault()?.Product?.Slug ?? "",
                Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                UnitPrice = o.OrderItems.FirstOrDefault()?.UnitPrice ?? 0,
                ShopName = o.Shop.ShopName,
                ShopId = o.ShopId,
                ShippingCity = o.ShippingCity,
                ShippingProvince = o.ShippingProvince,
                TrackingNumber = o.TrackingNumber,
                TrackingCarrier = o.TrackingCarrier,
                ShippedAt = o.ShippedAt,
                DeliveredAt = o.DeliveredAt
            }).ToList();
        }

        public async Task UpdateOrderStatusAsync(int orderId, int userId, string status, string? trackingNumber)
        {
            if (orderId <= 0) throw new ArgumentOutOfRangeException(nameof(orderId));
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            // Tracked (not AsNoTracking) - we mutate this entity below.
            var order = await _context.Orders
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new Exception("Order not found.");

            if (order.Shop.UserId != userId)
                throw new Exception("You don't have permission to update this order.");

            if (order.OrderStatus == "Delivered" || order.OrderStatus == "Cancelled")
                throw new Exception("Cannot change status of delivered or cancelled orders.");

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == "Shipped")
            {
                order.ShippedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(trackingNumber))
                    order.TrackingNumber = trackingNumber;
            }
            else if (status == "Delivered")
            {
                order.DeliveredAt = DateTime.UtcNow;
            }
            else if (status == "Cancelled")
            {
                order.CancelledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, int userId)
        {
            await UpdateOrderStatusAsync(orderId, userId, "Cancelled", null);
        }

        public async Task<object> GetProductByIdAsync(int productId, int userId)
        {
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            // EARLY RETURN: If no products exist
            var anyProducts = await _context.Products.AsNoTracking().AnyAsync();
            if (!anyProducts) throw new Exception("Product not found.");

            // Read-only - nothing here gets modified or saved.
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            // Check if user owns this product
            if (product.Shop.UserId != userId)
                throw new Exception("You don't have permission to view this product.");

            return new
            {
                product.Id,
                product.Title,
                product.Description,
                product.OriginalPrice,
                product.SalePrice,
                product.Quantity,
                product.Condition,
                product.IsUrgent,
                product.ShopId,
                CategoryId = product.CategoryId,
                product.Slug,
                product.Status,
                product.ListingEndDate,
                product.CreatedAt,
                product.UpdatedAt,
                product.SoldQuantity,
                product.DiscountPercentage,
                product.Saves,
                product.Views,
                Images = product.ProductImages.Select(i => new
                {
                    i.Id,
                    i.ImageUrl,
                    i.IsMain,
                    i.SortOrder
                }),
                ShopName = product.Shop.ShopName,
                CategoryName = product.Category?.Name
            };
        }

        public async Task<List<ProductCardDto>> GetProductsAsync(int userId)
        {
            if (userId <= 0) return new List<ProductCardDto>();

            // EARLY RETURN: If no products exist
            var anyProducts = await _context.Products.AsNoTracking().AnyAsync();
            if (!anyProducts) return new List<ProductCardDto>();

            var shopIds = await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            if (shopIds.Count == 0) return new List<ProductCardDto>();

            // Projected straight to the DTO instead of Include + map afterward -
            // same reasoning as ProductService: avoids materializing full Shop/
            // Category/ProductImages entities just to read a few scalar fields.
            //
            // NOTE: unlike ProductService's DTO mapping, this doesn't fall back to
            // the first image when no main image is set - that's the original
            // behavior, preserved exactly (a "fix" here would change what's
            // returned, which wasn't asked for).
            return await _context.Products
                .AsNoTracking()
                .Where(p => shopIds.Contains(p.ShopId))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductCardDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
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
                })
                .ToListAsync();
        }

        public async Task<object> CreateProductAsync(int userId, CreateProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // Read-only lookup, just for the ownership check - not modified here.
            var shop = await _context.Shops
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.ShopId && s.UserId == userId);

            if (shop == null)
                throw new Exception("Shop not found or you don't have permission.");

            var slug = await GenerateSlugAsync(dto.Title);

            var product = new Product
            {
                Title = dto.Title,
                Slug = slug,
                Description = dto.Description,
                OriginalPrice = dto.OriginalPrice,
                SalePrice = dto.SalePrice,
                DiscountPercentage = CalculateDiscount(dto.OriginalPrice, dto.SalePrice),
                Quantity = dto.Quantity,
                Condition = dto.Condition,
                IsUrgent = dto.IsUrgent,
                ShopId = dto.ShopId,
                CategoryId = dto.CategoryId,
                AdminApproved = true,
                Status = "Active",
                ListingEndDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            // Handle image upload - check for null
            if (dto.Images != null && dto.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                for (int i = 0; i < dto.Images.Count; i++)
                {
                    var file = dto.Images[i];
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var imageUrl = $"/uploads/products/{uniqueFileName}";

                    // Linked via the Product navigation rather than ProductId -
                    // product hasn't been saved yet, so its Id is still the
                    // temporary value. EF resolves the real FK automatically at
                    // SaveChanges time, which is what lets the product and all
                    // of its images go out in one SaveChangesAsync instead of two.
                    _context.ProductImages.Add(new ProductImage
                    {
                        Product = product,
                        ImageUrl = imageUrl,
                        IsMain = i == 0,
                        SortOrder = i,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new { Id = product.Id, Slug = product.Slug };
        }

        public async Task<object> UpdateProductAsync(int productId, int userId, CreateProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            // Tracked (not AsNoTracking) - fields are set directly on this entity below.
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            if (product.Shop == null || product.Shop.UserId != userId)
                throw new Exception("You don't have permission to edit this product.");

            var oldTitle = product.Title;
            var oldOriginalPrice = product.OriginalPrice;
            var oldSalePrice = product.SalePrice;
            var oldQuantity = product.Quantity;
            var oldCondition = product.Condition;
            var oldIsUrgent = product.IsUrgent;
            var oldCategoryId = product.CategoryId;

            product.Title = dto.Title;
            product.Description = dto.Description;
            product.OriginalPrice = dto.OriginalPrice;
            product.SalePrice = dto.SalePrice;
            product.DiscountPercentage = CalculateDiscount(dto.OriginalPrice, dto.SalePrice);
            product.Quantity = dto.Quantity;
            product.Condition = dto.Condition;
            product.IsUrgent = dto.IsUrgent;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            if (dto.Images != null && dto.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Delete existing images
                var existingImages = product.ProductImages.ToList();
                foreach (var img in existingImages)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    _context.ProductImages.Remove(img);
                }

                // Add new images
                for (int i = 0; i < dto.Images.Count; i++)
                {
                    var file = dto.Images[i];
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var imageUrl = $"/uploads/products/{uniqueFileName}";

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = imageUrl,
                        IsMain = i == 0,
                        SortOrder = i,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // product is already tracked (it came from the query above) and its
            // properties were changed directly, so EF's default change tracking
            // already knows exactly which columns to update - no need to force
            // EntityState.Modified, which would make it write every column
            // instead of just the ones that actually changed.
            var saveResult = await _context.SaveChangesAsync();

            if (saveResult == 0)
                throw new Exception("Failed to save product changes.");

            // product already holds the new values in memory (they were set
            // directly above) - no need to re-fetch it from the DB to compare
            // old vs new, that was a redundant round trip returning data we
            // already had.
            var changesDetected =
                oldTitle != product.Title ||
                oldOriginalPrice != product.OriginalPrice ||
                oldSalePrice != product.SalePrice ||
                oldQuantity != product.Quantity ||
                oldCondition != product.Condition ||
                oldIsUrgent != product.IsUrgent ||
                oldCategoryId != product.CategoryId;

            return new
            {
                product.Id,
                product.Title,
                product.Description,
                product.OriginalPrice,
                product.SalePrice,
                product.DiscountPercentage,
                product.Quantity,
                product.Condition,
                product.IsUrgent,
                product.ShopId,
                CategoryId = product.CategoryId,
                product.Slug,
                product.Status,
                product.ListingEndDate,
                product.CreatedAt,
                product.UpdatedAt,
                product.SoldQuantity,
                product.Saves,
                product.Views,
                Images = product.ProductImages.Select(i => new
                {
                    i.Id,
                    i.ImageUrl,
                    i.IsMain,
                    i.SortOrder
                }),
                ShopName = product.Shop?.ShopName,
                CategoryName = product.Category?.Name,
                ChangesApplied = changesDetected
            };
        }

        public async Task DeleteProductAsync(int productId, int userId)
        {
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            if (product.Shop.UserId != userId)
                throw new Exception("You don't have permission to delete this product.");

            if (product.ProductImages != null && product.ProductImages.Any())
            {
                foreach (var img in product.ProductImages)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ShopProfileDto>> GetShopsAsync(int userId)
        {
            if (userId <= 0) return new List<ShopProfileDto>();

            return await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new ShopProfileDto
                {
                    Id = s.Id,
                    ShopName = s.ShopName,
                    ShopDescription = s.ShopDescription,
                    City = s.City,
                    Province = s.Province,
                    Country = s.Country,
                    IsVerified = s.IsVerified,
                    TotalSales = s.TotalSales,
                    TotalRevenue = s.TotalRevenue,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    OwnerName = s.User.FullName
                })
                .ToListAsync();
        }

        public async Task<List<ShopProfileDto>> GetVerifiedShopsAsync(int userId)
        {
            if (userId <= 0) return new List<ShopProfileDto>();

            return await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId && s.IsVerified == true && s.Status == "Active")
                .Select(s => new ShopProfileDto
                {
                    Id = s.Id,
                    ShopName = s.ShopName,
                    ShopDescription = s.ShopDescription,
                    City = s.City,
                    Province = s.Province,
                    Country = s.Country,
                    IsVerified = s.IsVerified,
                    TotalSales = s.TotalSales,
                    TotalRevenue = s.TotalRevenue,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    OwnerName = s.User.FullName
                })
                .ToListAsync();
        }

        public async Task<object> CreateShopAsync(int userId, CreateShopDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));

            var shop = new Shop
            {
                ShopName = dto.ShopName,
                ShopDescription = dto.ShopDescription,
                City = dto.City,
                Province = dto.Province,
                PhoneNumber = dto.PhoneNumber,
                UserId = userId,
                Status = "Pending",
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                CommissionRate = 15.00m
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();

            return new { Id = shop.Id, Status = shop.Status };
        }

        public async Task<List<PayoutDto>> GetPayoutsAsync(int userId)
        {
            if (userId <= 0) return new List<PayoutDto>();

            var shopIds = await _context.Shops
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            if (shopIds.Count == 0) return new List<PayoutDto>();

            return await _context.Payouts
                .AsNoTracking()
                .Where(p => shopIds.Contains(p.ShopId))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PayoutDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    Status = p.Status,
                    ShopName = p.Shop.ShopName,
                    OrderNumber = p.Order.OrderNumber,
                    CreatedAt = p.CreatedAt,
                    ProcessedAt = p.ProcessedAt
                })
                .ToListAsync();
        }

        private async Task<string> GenerateSlugAsync(string title)
        {
            var slug = title.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("&", "and")
                .Replace("`", "")
                .Replace("\"", "")
                .Replace("'", "");

            // Remove any other special characters
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");

            // Ensure unique - was previously a synchronous .Count() call inside this
            // async method, which blocks a thread pool thread on every product
            // creation. CountAsync() lets the thread go back to the pool while the
            // query runs instead of blocking it.
            var existing = await _context.Products.CountAsync(p => p.Slug.StartsWith(slug));
            return existing > 0 ? $"{slug}-{existing + 1}" : slug;
        }

        private decimal CalculateDiscount(decimal original, decimal sale)
        {
            if (original <= 0 || sale <= 0) return 0;
            return Math.Round(((original - sale) / original) * 100, 2);
        }
    }
}