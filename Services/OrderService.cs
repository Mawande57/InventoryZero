using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly InventoryZeroDbContext _context;

        public OrderService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDetailDto> PlaceOrderAsync(int buyerId, PlaceOrderDto dto)
        {
            // 1. Get the product with shop info
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p =>
                    p.Id == dto.ProductId &&
                    p.Status == "Active" &&
                    p.AdminApproved);

            if (product == null)
                throw new Exception("Product not found or no longer available.");

            // 2. Check stock
            var remaining = product.Quantity - product.SoldQuantity;
            if (dto.Quantity > remaining)
                throw new Exception($"Only {remaining} units available.");

            // 3. If buyer provided a saved address ID use that
            string addressLine1 = dto.ShippingAddressLine1;
            string? addressLine2 = dto.ShippingAddressLine2;
            string city = dto.ShippingCity;
            string province = dto.ShippingProvince;
            string postalCode = dto.ShippingPostalCode;
            string phone = dto.ShippingPhoneNumber;

            if (dto.SavedAddressId.HasValue)
            {
                var saved = await _context.UserAddresses
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

            // 4. Calculate amounts
            var unitPrice = product.SalePrice;
            var subtotal = unitPrice * dto.Quantity;
            var shippingCost = 0m; // free shipping for now
            var taxAmount = 0m;   // no tax for now
            var totalAmount = subtotal + shippingCost + taxAmount;

            // 5. Calculate platform fee and seller payout
            var commissionRate = product.Shop.CommissionRate / 100;
            var platformFee = Math.Round(totalAmount * commissionRate, 2);
            var sellerPayout = totalAmount - platformFee;

            // 6. Generate unique order number
            var orderNumber = "IZ-" + DateTime.Now.ToString("yyyyMMdd") +
                              "-" + Guid.NewGuid().ToString("N")[..6].ToUpper();

            // 7. Create the order
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

            // 9. Update product sold quantity
            product.SoldQuantity += dto.Quantity;

            // 10. Update shop total sales
            product.Shop.TotalSales += dto.Quantity;
            product.Shop.TotalRevenue += sellerPayout;

            await _context.SaveChangesAsync();

            // 11. Create a pending payout record for the seller
            var payout = new Payout
            {
                ShopId = product.ShopId,
                OrderId = order.Id,
                Amount = sellerPayout,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Payouts.Add(payout);
            await _context.SaveChangesAsync();

            return await GetOrderDetailAsync(order.Id, buyerId)
                ?? throw new Exception("Order created but could not be retrieved.");
        }

        public async Task<List<OrderSummaryDto>> GetMyOrdersAsync(int buyerId)
        {
            var orders = await _context.Orders
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
            var order = await _context.Orders
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