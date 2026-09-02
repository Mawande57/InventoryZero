// Services/AdminService.cs
using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Admin;
using InventoryZeroAPI.DTOs.Products;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly InventoryZeroDbContext _context;

        public AdminService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            // These used to load the full Users/Shops/Products/Orders/Payouts tables into memory
            // just to call Count()/Sum() on them in C#. That drags every column of every row over
            // the wire for a dashboard that only needs a handful of numbers. Doing the aggregation
            // in SQL instead means the DB does the counting/summing and we only get scalars back.

            var totalUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.Role != "Admin");

            // Seller = a non-admin user who owns at least one active & verified shop.
            // EXISTS-style subquery avoids materializing both sets and intersecting in memory.
            var totalSellers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.Role != "Admin" &&
                    _context.Shops.Any(s => s.UserId == u.Id && s.Status == "Active" && s.IsVerified == true));

            var totalShops = await _context.Shops
                .AsNoTracking()
                .CountAsync(s => s.Status == "Active" && s.IsVerified == true);

            var pendingShops = await _context.Shops
                .AsNoTracking()
                .CountAsync(s => s.Status == "Pending");

            var totalProducts = await _context.Products
                .AsNoTracking()
                .CountAsync(p => p.Status == "Active");

            var totalOrders = await _context.Orders
                .AsNoTracking()
                .CountAsync();

            var totalRevenue = await _context.Orders
                .AsNoTracking()
                .SumAsync(o => o.TotalAmount);

            var platformFees = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus != "Cancelled")
                .SumAsync(o => o.PlatformFee);

            var pendingPayouts = await _context.Payouts
                .AsNoTracking()
                .Where(p => p.Status == "Pending" && p.Order.OrderStatus != "Cancelled")
                .SumAsync(p => p.Amount);

            var disputesOpen = await _context.Disputes
                .AsNoTracking()
                .CountAsync(d => d.Status == "Open");

            return new AdminStatsDto
            {
                TotalUsers = totalUsers,
                TotalSellers = totalSellers,
                TotalShops = totalShops,
                PendingShops = pendingShops,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                PlatformFees = platformFees,
                PendingPayouts = pendingPayouts,
                DisputesOpen = disputesOpen
            };
        }

        public async Task<PagedResultDto<AdminShopDto>> GetShopsAsync(string? status, int page, int pageSize)
        {
            // Fail fast on bad paging input instead of letting Skip() blow up further down
            // with a less obvious exception once the query actually hits the DB.
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or greater.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

            var query = _context.Shops
                .AsNoTracking()
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var totalCount = await query.CountAsync();
            var shops = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Was previously one Products.Count() query per shop in the page (N+1).
            // Batch it into a single grouped query for the shop ids we actually loaded.
            var shopIds = shops.Select(s => s.Id).ToList();
            var productCounts = await _context.Products
                .AsNoTracking()
                .Where(p => shopIds.Contains(p.ShopId) && p.Status == "Active")
                .GroupBy(p => p.ShopId)
                .Select(g => new { ShopId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ShopId, x => x.Count);

            var items = shops.Select(s => new AdminShopDto
            {
                Id = s.Id,
                ShopName = s.ShopName,
                ShopDescription = s.ShopDescription,
                City = s.City,
                Province = s.Province,
                Status = s.Status,
                IsVerified = s.IsVerified,
                CreatedAt = s.CreatedAt,
                TotalProducts = productCounts.TryGetValue(s.Id, out var count) ? count : 0,
                TotalSales = s.TotalSales,
                TotalRevenue = s.TotalRevenue,
                OwnerName = s.User.FullName,
                OwnerEmail = s.User.Email,
                PhoneNumber = s.PhoneNumber,
                BusinessRegistrationNumber = s.BusinessRegistrationNumber,
                TaxNumber = s.TaxNumber,
                VerificationNotes = s.VerificationNotes,
                VerificationDate = s.VerificationDate
            }).ToList();

            return new PagedResultDto<AdminShopDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<AdminShopDto?> GetShopDetailAsync(int id)
        {
            // No point hitting the DB for a shop id that can't possibly exist.
            if (id <= 0) return null;

            var shop = await _context.Shops
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shop == null) return null;

            return new AdminShopDto
            {
                Id = shop.Id,
                ShopName = shop.ShopName,
                ShopDescription = shop.ShopDescription,
                City = shop.City,
                Province = shop.Province,
                Status = shop.Status,
                IsVerified = shop.IsVerified,
                CreatedAt = shop.CreatedAt,
                TotalProducts = await _context.Products.AsNoTracking().CountAsync(p => p.ShopId == shop.Id && p.Status == "Active"),
                TotalSales = shop.TotalSales,
                TotalRevenue = shop.TotalRevenue,
                OwnerName = shop.User.FullName,
                OwnerEmail = shop.User.Email,
                PhoneNumber = shop.PhoneNumber,
                BusinessRegistrationNumber = shop.BusinessRegistrationNumber,
                TaxNumber = shop.TaxNumber,
                VerificationNotes = shop.VerificationNotes,
                VerificationDate = shop.VerificationDate
            };
        }

        public async Task ApproveShopAsync(int shopId, int adminId, ShopApprovalDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null)
                throw new Exception("Shop not found.");

            shop.Status = "Active";
            shop.IsVerified = true;
            shop.VerificationDate = DateTime.UtcNow;  // ← Changed from DateTime.Now
            shop.VerificationNotes = dto.Notes;

            _context.ActivityLogs.Add(new ActivityLog
            {
                AdminUserId = adminId,
                Action = "Approved Shop",
                EntityType = "Shop",
                EntityId = shopId,
                NewValue = $"Shop '{shop.ShopName}' approved",
                CreatedAt = DateTime.UtcNow  // ← Changed from DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task RejectShopAsync(int shopId, int adminId, string reason)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null)
                throw new Exception("Shop not found.");

            shop.Status = "Rejected";
            shop.VerificationNotes = $"Rejected: {reason}";

            _context.ActivityLogs.Add(new ActivityLog
            {
                AdminUserId = adminId,
                Action = "Rejected Shop",
                EntityType = "Shop",
                EntityId = shopId,
                NewValue = $"Shop '{shop.ShopName}' rejected. Reason: {reason}",
                CreatedAt = DateTime.UtcNow  // ← Changed from DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? role, int page, int pageSize)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or greater.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Role != "Admin")
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            // Shop count per user, batched for the whole page in one query.
            var shopCounts = await _context.Shops
                .AsNoTracking()
                .Where(s => userIds.Contains(s.UserId))
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            // TotalOrders/TotalSpent used to be two separate queries PER USER in the page
            // (Orders.Count + Orders.Where(Paid).Sum), which is an N+1 on top of an N+1.
            // One grouped query gets both numbers for every user in the page at once.
            var orderStats = await _context.Orders
                .AsNoTracking()
                .Where(o => userIds.Contains(o.BuyerId))
                .GroupBy(o => o.BuyerId)
                .Select(g => new
                {
                    BuyerId = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.PaymentStatus == "Paid" ? o.TotalAmount : 0)
                })
                .ToDictionaryAsync(x => x.BuyerId, x => x);

            var items = users.Select(u =>
            {
                var stats = orderStats.GetValueOrDefault(u.Id);

                return new AdminUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    IsEmailVerified = u.IsEmailVerified,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    TotalOrders = stats?.TotalOrders ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0,
                    TotalShops = shopCounts.TryGetValue(u.Id, out var shopCount) ? shopCount : 0
                };
            }).ToList();

            return new PagedResultDto<AdminUserDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task ToggleUserStatusAsync(int userId)
        {
            // 1. Get the user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            // 2. Check if user has any pending or processing orders
            var hasActiveOrders = await _context.Orders
                .AnyAsync(o =>
                    o.BuyerId == userId &&
                    (o.OrderStatus == "Pending" || o.OrderStatus == "Processing")
                );

            // 3. If trying to DEACTIVATE (IsActive is true → becoming false)
            if (user.IsActive && hasActiveOrders)
                throw new Exception("Cannot deactivate a user with pending or processing orders.");

            // 4. Toggle status
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task ChangeUserRoleAsync(int userId, string role)
        {
            // Already fails before touching the DB if the role is invalid - left as-is.
            var validRoles = new[] { "Buyer", "Seller", "Admin" };
            if (!validRoles.Contains(role))
                throw new Exception("Invalid role.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            user.Role = role;
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<AdminProductDto>> GetProductsAsync(string? status, int page, int pageSize)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or greater.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

            // Shop and Category are many-to-one references, not collections, so there's no
            // cartesian-product row blowup here - a plain Include (single SQL join) is fine
            // and AsSplitQuery() would just add an extra round trip for no benefit.
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var totalCount = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = products.Select(p => new AdminProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                OriginalPrice = p.OriginalPrice,
                SalePrice = p.SalePrice,
                Quantity = p.Quantity,
                SoldQuantity = p.SoldQuantity,
                Status = p.Status,
                AdminApproved = p.AdminApproved,
                CreatedAt = p.CreatedAt,
                ShopName = p.Shop.ShopName,
                ShopId = p.ShopId,
                CategoryName = p.Category?.Name,
                Views = p.Views,
                Saves = p.Saves
            }).ToList();

            return new PagedResultDto<AdminProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task ToggleProductStatusAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new Exception("Product not found.");

            product.Status = product.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<AdminOrderDto>> GetOrdersAsync(string? status, int page, int pageSize)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or greater.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Buyer)
                .Include(o => o.Shop)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = orders.Select(o => new AdminOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                PlatformFee = o.PlatformFee,
                SellerPayout = o.SellerPayout,
                CreatedAt = o.CreatedAt,
                BuyerName = o.Buyer.FullName,
                BuyerEmail = o.Buyer.Email,
                ShopName = o.Shop.ShopName,
                ShippingCity = o.ShippingCity,
                ShippingProvince = o.ShippingProvince,
                TrackingNumber = o.TrackingNumber
            }).ToList();

            return new PagedResultDto<AdminOrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResultDto<AdminPayoutDto>> GetPayoutsAsync(string? status, int page, int pageSize)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or greater.");
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater.");

            var query = _context.Payouts
                .AsNoTracking()
                .Include(p => p.Shop)
                    .ThenInclude(s => s.User)  // ✅ ADD THIS - loads Shop.User
                .Include(p => p.Order)
                    .ThenInclude(o => o.Buyer)
                .Where(p => p.Order.OrderStatus != "Cancelled")
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var totalCount = await query.CountAsync();
            var payouts = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = payouts.Select(p => new AdminPayoutDto
            {
                Id = p.Id,
                Amount = p.Amount,
                Status = p.Status,
                ShopName = p.Shop?.ShopName ?? "Unknown Shop",
                ShopOwner = p.Shop?.User?.FullName ?? "Unknown",  // ✅ Now this works!
                OrderNumber = p.Order?.OrderNumber ?? "N/A",
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt,
                StripeTransferId = p.StripeTransferId,
                ErrorMessage = p.ErrorMessage
            }).ToList();

            return new PagedResultDto<AdminPayoutDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<object> ProcessPendingPayoutsAsync()
        {
            var pending = await _context.Payouts
                .Where(p => p.Status == "Pending" && p.Order.OrderStatus != "Cancelled")
                .ToListAsync();

            // Nothing to do - skip the SaveChangesAsync round trip entirely rather than
            // opening a transaction for zero updates.
            if (pending.Count == 0)
            {
                return new
                {
                    Processed = 0,
                    TotalAmount = 0m,
                    Message = $"{0} payouts processed totaling {0m:C}"
                };
            }

            var processed = 0;
            var totalAmount = 0m;

            foreach (var payout in pending)
            {
                // In a real app, this would call Stripe/PayFast API
                // For simulation, we'll mark as completed
                payout.Status = "Completed";
                payout.ProcessedAt = DateTime.Now;
                payout.StripeTransferId = $"transfer_{Guid.NewGuid():N}";
                processed++;
                totalAmount += payout.Amount;
            }

            await _context.SaveChangesAsync();

            return new
            {
                Processed = processed,
                TotalAmount = totalAmount,
                Message = $"{processed} payouts processed totaling {totalAmount:C}"
            };
        }
    }
}