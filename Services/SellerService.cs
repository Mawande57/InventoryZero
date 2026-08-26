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
        // Services/SellerService.cs - Add this method

        public async Task<object> GetProductByIdAsync(int productId, int userId)
        {
            var product = await _context.Products
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
                // Include images
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

        // Services/SellerService.cs

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

            // ✅ Handle image upload - check for null
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

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = imageUrl,
                        IsMain = i == 0,
                        SortOrder = i,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
            }

            return new { Id = product.Id, Slug = product.Slug };
        }

        // Also update UpdateProductAsync for editing images
        public async Task<object> UpdateProductAsync(int productId, int userId, CreateProductDto dto)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"🔍 UPDATE PRODUCT STARTED");
            Console.WriteLine($"📌 ProductId: {productId}");
            Console.WriteLine($"👤 UserId: {userId}");
            Console.WriteLine($"📦 New Values from DTO:");
            Console.WriteLine($"   - Title: {dto.Title}");
            Console.WriteLine($"   - Description: {dto.Description}");
            Console.WriteLine($"   - OriginalPrice: {dto.OriginalPrice}");
            Console.WriteLine($"   - SalePrice: {dto.SalePrice}");
            Console.WriteLine($"   - Quantity: {dto.Quantity}");
            Console.WriteLine($"   - Condition: {dto.Condition}");
            Console.WriteLine($"   - IsUrgent: {dto.IsUrgent}");
            Console.WriteLine($"   - ShopId: {dto.ShopId}");
            Console.WriteLine($"   - CategoryId: {dto.CategoryId}");
            Console.WriteLine($"   - Images Count: {dto.Images?.Count ?? 0}");
            Console.WriteLine("========================================");

            // ─── GET PRODUCT ─────────────────────────────────────
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                Console.WriteLine($"❌ ERROR: Product {productId} not found in database!");
                throw new Exception("Product not found.");
            }

            Console.WriteLine($"✅ Found product in database:");
            Console.WriteLine($"   - Current Title: {product.Title}");
            Console.WriteLine($"   - Current OriginalPrice: {product.OriginalPrice}");
            Console.WriteLine($"   - Current SalePrice: {product.SalePrice}");
            Console.WriteLine($"   - Current Quantity: {product.Quantity}");
            Console.WriteLine($"   - Current Condition: {product.Condition}");
            Console.WriteLine($"   - Current IsUrgent: {product.IsUrgent}");
            Console.WriteLine($"   - Current ShopId: {product.ShopId}");
            Console.WriteLine($"   - Current CategoryId: {product.CategoryId}");
            Console.WriteLine($"   - Current Images: {product.ProductImages.Count}");
            Console.WriteLine($"   - Shop UserId: {product.Shop?.UserId}");

            // ─── CHECK PERMISSION ──────────────────────────────
            if (product.Shop == null || product.Shop.UserId != userId)
            {
                Console.WriteLine($"❌ ERROR: User {userId} doesn't own this product! Shop.UserId: {product.Shop?.UserId}");
                throw new Exception("You don't have permission to edit this product.");
            }

            Console.WriteLine($"✅ Permission check passed - User owns this product");

            // ─── UPDATE PRODUCT ──────────────────────────────────
            Console.WriteLine("📝 Updating product fields...");

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
            product.UpdatedAt = DateTime.Now;

            Console.WriteLine("📊 Field changes:");
            Console.WriteLine($"   - Title: '{oldTitle}' → '{product.Title}'");
            Console.WriteLine($"   - OriginalPrice: {oldOriginalPrice} → {product.OriginalPrice}");
            Console.WriteLine($"   - SalePrice: {oldSalePrice} → {product.SalePrice}");
            Console.WriteLine($"   - Quantity: {oldQuantity} → {product.Quantity}");
            Console.WriteLine($"   - Condition: '{oldCondition}' → '{product.Condition}'");
            Console.WriteLine($"   - IsUrgent: {oldIsUrgent} → {product.IsUrgent}");
            Console.WriteLine($"   - CategoryId: {oldCategoryId} → {product.CategoryId}");
            Console.WriteLine($"   - DiscountPercentage: {product.DiscountPercentage}%");

            // ─── HANDLE IMAGES ──────────────────────────────────
            if (dto.Images != null && dto.Images.Any())
            {
                Console.WriteLine($"📷 Processing {dto.Images.Count} new images...");

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Console.WriteLine($"📁 Creating uploads folder: {uploadsFolder}");
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete existing images
                var existingImages = product.ProductImages.ToList();
                Console.WriteLine($"🗑️ Deleting {existingImages.Count} existing images...");

                foreach (var img in existingImages)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"   - Deleting file: {filePath}");
                        File.Delete(filePath);
                    }
                    _context.ProductImages.Remove(img);
                }

                // Add new images
                Console.WriteLine($"📤 Adding {dto.Images.Count} new images...");
                for (int i = 0; i < dto.Images.Count; i++)
                {
                    var file = dto.Images[i];
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    Console.WriteLine($"   - Saving image {i + 1}: {file.FileName} → {uniqueFileName}");

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
                        CreatedAt = DateTime.Now
                    });
                }
            }
            else
            {
                Console.WriteLine("📷 No new images to process - keeping existing images");
            }

            // ─── SAVE CHANGES ────────────────────────────────────
            Console.WriteLine("💾 Saving changes to database...");

            // Force EF to track changes
            _context.Entry(product).State = EntityState.Modified;

            var saveResult = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ SaveChangesAsync returned: {saveResult} entities saved");

            if (saveResult == 0)
            {
                Console.WriteLine("⚠️ WARNING: No entities were saved! Changes may not have been applied.");
                throw new Exception("Failed to save product changes.");
            }

            // ─── VERIFY UPDATE ───────────────────────────────────
            Console.WriteLine("🔍 Verifying update by fetching product again...");
            var verifiedProduct = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            Console.WriteLine("✅ Verification result:");
            Console.WriteLine($"   - Title: {verifiedProduct?.Title}");
            Console.WriteLine($"   - OriginalPrice: {verifiedProduct?.OriginalPrice}");
            Console.WriteLine($"   - SalePrice: {verifiedProduct?.SalePrice}");
            Console.WriteLine($"   - Quantity: {verifiedProduct?.Quantity}");
            Console.WriteLine($"   - Condition: {verifiedProduct?.Condition}");
            Console.WriteLine($"   - Images: {verifiedProduct?.ProductImages.Count}");

            // ─── COMPARE BEFORE/AFTER ──────────────────────────
            var changesDetected =
                oldTitle != verifiedProduct?.Title ||
                oldOriginalPrice != verifiedProduct.OriginalPrice ||
                oldSalePrice != verifiedProduct.SalePrice ||
                oldQuantity != verifiedProduct.Quantity ||
                oldCondition != verifiedProduct.Condition ||
                oldIsUrgent != verifiedProduct.IsUrgent ||
                oldCategoryId != verifiedProduct.CategoryId;

            Console.WriteLine($"✅ Changes successfully applied: {changesDetected}");
            Console.WriteLine("========================================");

            // ─── RETURN UPDATED PRODUCT ──────────────────────────
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
            Console.WriteLine($"🗑️ DeleteProductAsync: ProductId={productId}, UserId={userId}");

            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                Console.WriteLine($"❌ Product {productId} not found!");
                throw new Exception("Product not found.");
            }

            if (product.Shop.UserId != userId)
            {
                Console.WriteLine($"❌ User {userId} doesn't own this product!");
                throw new Exception("You don't have permission to delete this product.");
            }

            // ─── DELETE IMAGE FILES ──────────────────────────────
            if (product.ProductImages != null && product.ProductImages.Any())
            {
                Console.WriteLine($"🗑️ Deleting {product.ProductImages.Count} image files...");
                foreach (var img in product.ProductImages)
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.ImageUrl.TrimStart('/'));
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"   - Deleting file: {filePath}");
                        File.Delete(filePath);
                    }
                }
            }

            // ─── REMOVE FROM DATABASE ────────────────────────────
            _context.Products.Remove(product);
            var saveResult = await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Product deleted. Save result: {saveResult}");


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
                .Replace("&", "and")
                .Replace("`", "")  // ✅ Remove backticks
                .Replace("\"", "") // ✅ Remove quotes
                .Replace("'", ""); // ✅ Remove single quotes

            // Remove any other special characters
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9-]", "");

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

