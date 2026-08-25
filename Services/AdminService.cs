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
            // ✅ Exclude Admin from users
            var users = await _context.Users
                .Where(u => u.Role != "Admin")
                .ToListAsync();

            // ✅ Sellers = Users who have at least one shop
            var userIdsWithShops = await _context.Shops
                .Where(s => s.Status == "Active" && s.IsVerified == true)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var sellers = users.Where(u => userIdsWithShops.Contains(u.Id)).ToList();

            var shops = await _context.Shops
                .Where(s => s.Status == "Active" && s.IsVerified == true)
                .ToListAsync();

            var pendingShops = await _context.Shops
                .Where(s => s.Status == "Pending")
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Status == "Active")
                .ToListAsync();

            var orders = await _context.Orders.ToListAsync();
            var payouts = await _context.Payouts.ToListAsync();
            var disputes = await _context.Disputes.ToListAsync();

            return new AdminStatsDto
            {
                TotalUsers = users.Count,
                TotalSellers = sellers.Count,  // ✅ Users with shops
                TotalShops = shops.Count,
                PendingShops = pendingShops.Count,
                TotalProducts = products.Count,
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                PlatformFees = orders.Sum(o => o.PlatformFee),
                PendingPayouts = payouts.Where(p => p.Status == "Pending").Sum(p => p.Amount),
                DisputesOpen = disputes.Count(d => d.Status == "Open")
            };
        }

        public async Task<PagedResultDto<AdminShopDto>> GetShopsAsync(string? status, int page, int pageSize)
        {
            var query = _context.Shops
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
                TotalProducts = _context.Products.Count(p => p.ShopId == s.Id && p.Status == "Active"),
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
            var shop = await _context.Shops
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
                TotalProducts = await _context.Products.CountAsync(p => p.ShopId == shop.Id && p.Status == "Active"),
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
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null)
                throw new Exception("Shop not found.");

            shop.Status = "Active";
            shop.IsVerified = true;
            shop.VerificationDate = DateTime.Now;
            shop.VerificationNotes = dto.Notes;

            // Log activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                AdminUserId = adminId,
                Action = "Approved Shop",
                EntityType = "Shop",
                EntityId = shopId,
                NewValue = $"Shop '{shop.ShopName}' approved",
                CreatedAt = DateTime.Now
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
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? role, int page, int pageSize)
        {
            var query = _context.Users
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
            var shopCounts = await _context.Shops
                .Where(s => userIds.Contains(s.UserId))
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var items = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsEmailVerified = u.IsEmailVerified,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                TotalOrders = _context.Orders.Count(o => o.BuyerId == u.Id),
                TotalSpent = _context.Orders.Where(o => o.BuyerId == u.Id && o.PaymentStatus == "Paid").Sum(o => o.TotalAmount),
                TotalShops = shopCounts.ContainsKey(u.Id) ? shopCounts[u.Id] : 0
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
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
        }

        public async Task ChangeUserRoleAsync(int userId, string role)
        {
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
            var query = _context.Products
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
            var query = _context.Orders
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
            var query = _context.Payouts
                .Include(p => p.Shop)
                .Include(p => p.Order)
                .ThenInclude(o => o.Buyer)
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
                ShopOwner = p.Shop?.User?.FullName ?? "Unknown",
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
                .Where(p => p.Status == "Pending")
                .ToListAsync();

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