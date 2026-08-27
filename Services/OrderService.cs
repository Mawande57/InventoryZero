using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly InventoryZeroDbContext _context;

        // NOTE: this only protects against races within a single process, and only if
        // OrderService is registered as a Singleton in DI - if it's Scoped (the usual
        // registration for anything holding a DbContext), a fresh semaphore is created
        // per request and this does nothing at all. Even as a Singleton, it serializes
        // every order for every product platform-wide through one lock, and it still
        // wouldn't protect against oversell once there's more than one app instance
        // behind a load balancer. Real oversell protection needs a DB-level conditional
        // update or row locking inside a transaction. Left as-is - swapping this out
        // changes the concurrency model, not just performance.
        private readonly SemaphoreSlim _stockLock = new SemaphoreSlim(1, 1);

        public OrderService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDetailDto> PlaceOrderAsync(int buyerId, PlaceOrderDto dto)
        {
            // Validate before ever taking the lock - no point blocking other orders
            // on a request that was going to fail anyway.
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Quantity <= 0) throw new Exception("Quantity must be at least 1.");

            await _stockLock.WaitAsync();

            try
            {
                // 1. Get the product (tracked - we update SoldQuantity/Status on it below).
                // ProductImages isn't used anywhere in this method, so it's not included -
                // the final response is built by GetOrderDetailAsync at the end anyway,
                // which does its own query with the includes it actually needs.
                var product = await _context.Products
                    .Include(p => p.Shop)
                    .FirstOrDefaultAsync(p =>
                        p.Id == dto.ProductId &&
                        p.Status == "Active" &&
                        p.AdminApproved);

                if (product == null)
                    throw new Exception("Product not found or no longer available.");

                // Shop was already loaded above via Include(p => p.Shop) - no need to
                // query it again separately, it's the same row.
                var shop = product.Shop;

                if (shop == null)
                    throw new Exception("Shop not found.");

                // 2. Check if buyer owns the shop
                if (shop.UserId == buyerId)
                    throw new Exception("You cannot purchase your own products.");

                // 3. Check stock
                var remaining = product.Quantity - product.SoldQuantity;
                if (dto.Quantity > remaining)
                    throw new Exception($"Only {remaining} units available.");

                // 4. Get shipping address
                string addressLine1 = dto.ShippingAddressLine1;
                string? addressLine2 = dto.ShippingAddressLine2;
                string city = dto.ShippingCity;
                string province = dto.ShippingProvince;
                string postalCode = dto.ShippingPostalCode;
                string phone = dto.ShippingPhoneNumber;

                if (dto.SavedAddressId.HasValue)
                {
                    var saved = await _context.UserAddresses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a =>
                            a.Id == dto.SavedAddressId.Value &&
                            a.UserId == buyerId);

                    if (saved != null)
                    {
                        addressLine1 = saved.AddressLine1;
                        addressLine2 = saved.AddressLine2;
                        city = saved.City;
                        province = saved.Province;
                        postalCode = saved.PostalCode;
                        phone = saved.PhoneNumber;
                    }
                }

                // 5. Calculate amounts
                var unitPrice = product.SalePrice;
                var subtotal = unitPrice * dto.Quantity;
                var shippingCost = 0m;
                var taxAmount = 0m;
                var totalAmount = subtotal + shippingCost + taxAmount;

                var commissionRate = shop.CommissionRate / 100;
                var platformFee = Math.Round(totalAmount * commissionRate, 2);
                var sellerPayout = totalAmount - platformFee;

                // 6. Generate order number
                var orderNumber = "IZ-" + DateTime.Now.ToString("yyyyMMdd") +
                                  "-" + Guid.NewGuid().ToString("N")[..6].ToUpper();

                // 7. Create order
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    BuyerId = buyerId,
                    ShopId = product.ShopId,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = subtotal,
                    ShippingCost = shippingCost,
                    TaxAmount = taxAmount,
                    TotalAmount = totalAmount,
                    PlatformFee = platformFee,
                    SellerPayout = sellerPayout,
                    PaymentStatus = "Pending",
                    OrderStatus = "Pending",
                    ShippingAddressLine1 = addressLine1,
                    ShippingAddressLine2 = addressLine2,
                    ShippingCity = city,
                    ShippingProvince = province,
                    ShippingPostalCode = postalCode,
                    ShippingCountry = "South Africa",
                    ShippingPhoneNumber = phone,
                    BuyerNotes = dto.BuyerNotes,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);

                // 8. Create order item
                var orderItem = new OrderItem
                {
                    Order = order,
                    ProductId = product.Id,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice,
                    Subtotal = subtotal,
                    CreatedAt = DateTime.Now
                };

                _context.OrderItems.Add(orderItem);

                // Payout links to the order via the navigation property (Order = order)
                // rather than OrderId = order.Id. The order hasn't been saved yet at this
                // point, so order.Id is still the temporary/default value - EF's change
                // tracker resolves the real FK automatically at SaveChanges time as long
                // as the relationship is set via navigation. That's what lets order,
                // order item, payout, and the product/shop updates below all go out in
                // a single SaveChangesAsync instead of two.
                var payout = new Payout
                {
                    ShopId = product.ShopId,
                    Order = order,
                    Amount = sellerPayout,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Payouts.Add(payout);

                // 9. Update product sold quantity
                product.SoldQuantity += dto.Quantity;
                product.UpdatedAt = DateTime.Now;

                if (product.Quantity - product.SoldQuantity <= 0)
                {
                    product.Status = "Out of Stock";
                }

                shop.TotalSales += dto.Quantity;
                shop.TotalRevenue += sellerPayout;

                // Order, order item, payout, and the product/shop counter updates are
                // one unit of work - committing them together means the DB never ends
                // up in a state where the order exists but the payout doesn't (or vice
                // versa), which the previous two-save version could leave behind if the
                // second SaveChangesAsync failed.
                await _context.SaveChangesAsync();

                return await GetOrderDetailAsync(order.Id, buyerId)
                    ?? throw new Exception("Order created but could not be retrieved.");
            }
            finally
            {
                _stockLock.Release();
            }
        }

        public async Task<List<OrderSummaryDto>> GetMyOrdersAsync(int buyerId)
        {
            // Nothing to look up for an invalid buyer id.
            if (buyerId <= 0) return new List<OrderSummaryDto>();

            // OrderItems and ProductImages are both collections, nested two levels deep
            // (Order -> OrderItems -> Product -> ProductImages). Loaded as one query,
            // that shape duplicates every scalar Order/OrderItem column once per image
            // row. AsSplitQuery() runs it as separate queries instead, avoiding that
            // row multiplication - worth it here since an order can have several items
            // and each product can have several images.
            var orders = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.Shop)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => MapToSummary(o)).ToList();
        }

        public async Task<OrderDetailDto?> GetOrderDetailAsync(int orderId, int buyerId)
        {
            if (orderId <= 0 || buyerId <= 0) return null;

            var order = await _context.Orders
                .AsNoTracking()
                .AsSplitQuery()
                .Include(o => o.Shop)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.BuyerId == buyerId);

            if (order == null) return null;

            var summary = MapToSummary(order);

            return new OrderDetailDto
            {
                Id = summary.Id,
                OrderNumber = summary.OrderNumber,
                OrderStatus = summary.OrderStatus,
                PaymentStatus = summary.PaymentStatus,
                TotalAmount = summary.TotalAmount,
                PlatformFee = summary.PlatformFee,
                SellerPayout = summary.SellerPayout,
                CreatedAt = summary.CreatedAt,
                ProductTitle = summary.ProductTitle,
                ProductSlug = summary.ProductSlug,
                ProductImage = summary.ProductImage,
                Quantity = summary.Quantity,
                UnitPrice = summary.UnitPrice,
                ShopName = summary.ShopName,
                ShopId = summary.ShopId,
                ShippingCity = summary.ShippingCity,
                ShippingProvince = summary.ShippingProvince,
                TrackingNumber = summary.TrackingNumber,
                TrackingCarrier = summary.TrackingCarrier,
                ShippedAt = summary.ShippedAt,
                DeliveredAt = summary.DeliveredAt,
                ShippingAddressLine1 = order.ShippingAddressLine1,
                ShippingAddressLine2 = order.ShippingAddressLine2,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingPhoneNumber = order.ShippingPhoneNumber,
                ShippingCountry = order.ShippingCountry,
                ShippingCost = order.ShippingCost,
                TaxAmount = order.TaxAmount,
                Subtotal = order.Subtotal,
                BuyerNotes = order.BuyerNotes,
                SellerNotes = order.SellerNotes,
                CancellationReason = order.CancellationReason,
                CancelledAt = order.CancelledAt,
                PaidAt = order.PaidAt,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    Title = oi.Product.Title,
                    Slug = oi.Product.Slug,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal
                }).ToList()
            };
        }

        private OrderSummaryDto MapToSummary(Order o)
        {
            // All of this operates on data already loaded via Include above -
            // no additional DB queries happen here.
            var firstItem = o.OrderItems.FirstOrDefault();
            var product = firstItem?.Product;

            return new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                PlatformFee = o.PlatformFee,
                SellerPayout = o.SellerPayout,
                CreatedAt = o.CreatedAt,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                ShopName = o.Shop.ShopName,
                ShopId = o.Shop.Id,
                ShippingCity = o.ShippingCity,
                ShippingProvince = o.ShippingProvince,
                TrackingNumber = o.TrackingNumber,
                TrackingCarrier = o.TrackingCarrier,
                ShippedAt = o.ShippedAt,
                DeliveredAt = o.DeliveredAt,
                ProductTitle = product?.Title ?? "Unknown",
                ProductSlug = product?.Slug ?? "",
                ProductImage = product?.ProductImages
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl
                    ?? product?.ProductImages.FirstOrDefault()?.ImageUrl
            };
        }
    }
}