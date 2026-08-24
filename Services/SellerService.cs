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
            var shops = await _context.Shops.Where(s => s.UserId == userId).ToListAsync();
            var shopIds = shops.Select(s => s.Id).ToList();

            var products = await _context.Products
                .Where(p => shopIds.Contains(p.ShopId))
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => shopIds.Contains(o.ShopId))
                .ToListAsync();

            var revenue = orders
                .Where(o => o.OrderStatus == "Delivered" || o.OrderStatus == "Shipped")
                .Sum(o => o.SellerPayout);

            return new
            {
                Shops = shops.Count,
                Products = products.Count,
                Orders = orders.Count,
                Revenue = revenue
            };
        }

        public async Task<List<OrderSummaryDto>> GetOrdersAsync(int userId, string? status)
        {
            var shopIds = await _context.Shops
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            var query = _context.Orders
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
            order.UpdatedAt = DateTime.Now;

            if (status == "Shipped")
            {
                order.ShippedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(trackingNumber))
                    order.TrackingNumber = trackingNumber;
            }
            else if (status == "Delivered")
            {
                order.DeliveredAt = DateTime.Now;
            }
            else if (status == "Cancelled")
            {
                order.CancelledAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, int userId)
        {
            await UpdateOrderStatusAsync(orderId, userId, "Cancelled", null);
        }

        public async Task<List<ProductCardDto>> GetProductsAsync(int userId)
        {
            var shopIds = await _context.Shops
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => shopIds.Contains(p.ShopId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return products.Select(p => new ProductCardDto
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
                CategoryName = p.Category?.Name,
                MainImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain)?.ImageUrl
            }).ToList();
        }

        // Services/SellerService.cs - Update CreateProductAsync

        public async Task<object> CreateProductAsync(int userId, CreateProductDto dto)
        {
            var shop = await _context.Shops
                .FirstOrDefaultAsync(s => s.Id == dto.ShopId && s.UserId == userId);

            if (shop == null)
                throw new Exception("Shop not found or you don't have permission.");

            var slug = GenerateSlug(dto.Title);

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
                ListingEndDate = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // ✅ Handle image upload
            if (dto.Images != null && dto.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var imageList = new List<ProductImage>();

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

                    imageList.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = imageUrl,
                        IsMain = i == 0,  // First image is main
                        SortOrder = i,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.ProductImages.AddRangeAsync(imageList);
                await _context.SaveChangesAsync();
            }

            return new { Id = product.Id, Slug = product.Slug };
        }

        // Also update UpdateProductAsync for editing images
        public async Task UpdateProductAsync(int productId, int userId, CreateProductDto dto)
        {
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            if (product.Shop.UserId != userId)
                throw new Exception("You don't have permission to edit this product.");

            product.Title = dto.Title;
            product.Description = dto.Description;
            product.OriginalPrice = dto.OriginalPrice;
            product.SalePrice = dto.SalePrice;
            product.DiscountPercentage = CalculateDiscount(dto.OriginalPrice, dto.SalePrice);
            product.Quantity = dto.Quantity;
            product.Condition = dto.Condition;
            product.IsUrgent = dto.IsUrgent;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.Now;

            // ✅ Handle new images
            if (dto.Images != null && dto.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Keep existing images count for sort order
                var existingCount = product.ProductImages.Count;

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
                        IsMain = existingCount == 0 && i == 0,
                        SortOrder = existingCount + i,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }
        }


        public async Task DeleteProductAsync(int productId, int userId)
        {
            var product = await _context.Products
                .Include(p => p.Shop)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                throw new Exception("Product not found.");

            if (product.Shop.UserId != userId)
                throw new Exception("You don't have permission to delete this product.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ShopProfileDto>> GetShopsAsync(int userId)
        {
            var shops = await _context.Shops
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            return shops.Select(s => new ShopProfileDto
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
            }).ToList();
        }
        public async Task<List<ShopProfileDto>> GetVerifiedShopsAsync(int userId)
        {
            var shops = await _context.Shops
                .Include(s => s.User)
                .Where(s => s.UserId == userId && s.IsVerified == true && s.Status == "Active")  // ✅ ONLY verified + active
                .ToListAsync();

            return shops.Select(s => new ShopProfileDto
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
            }).ToList();
        }

        public async Task<object> CreateShopAsync(int userId, CreateShopDto dto)
        {
            var shop = new Shop
            {
                ShopName = dto.ShopName,
                ShopDescription = dto.ShopDescription,
                City = dto.City,
                Province = dto.Province,
                PhoneNumber = dto.PhoneNumber,
                UserId = userId,
                Status = "Pending",
                IsVerified= false,
                CreatedAt = DateTime.Now,
                CommissionRate = 15.00m
            };

            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();

            return new { Id = shop.Id, Status = shop.Status };
        }

        public async Task<List<PayoutDto>> GetPayoutsAsync(int userId)
        {
            var shopIds = await _context.Shops
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .ToListAsync();

            var payouts = await _context.Payouts
                .Include(p => p.Shop)
                .Include(p => p.Order)
                .Where(p => shopIds.Contains(p.ShopId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payouts.Select(p => new PayoutDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                ShopName = p.Shop.ShopName,
                OrderNumber = p.Order.OrderNumber,
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt
            }).ToList();
        }

        private string GenerateSlug(string title)
        {
            var slug = title.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("&", "and");

            // Ensure unique
            var existing = _context.Products.Count(p => p.Slug.StartsWith(slug));
            return existing > 0 ? $"{slug}-{existing + 1}" : slug;
        }

        private decimal CalculateDiscount(decimal original, decimal sale)
        {
            if (original <= 0 || sale <= 0) return 0;
            return Math.Round(((original - sale) / original) * 100, 2);
        }
    }
}

