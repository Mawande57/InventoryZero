using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.SavedProducts;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class SavedProductService : ISavedProductService
    {
        private readonly InventoryZeroDbContext _context;

        public SavedProductService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<List<SavedProductDto>> GetSavedAsync(int userId)
        {
            if (userId <= 0) return new List<SavedProductDto>();

            // Projecting straight to SavedProductDto instead of Include + map
            // afterward - avoids materializing full Product/Shop/Category/
            // ProductImages entities just to read a handful of scalar fields off
            // each. MainImageUrl becomes a small correlated subquery instead of a
            // join against the whole ProductImages table for every saved item.
            return await _context.SavedProducts
                .AsNoTracking()
                .Where(sp => sp.UserId == userId)
                .OrderByDescending(sp => sp.CreatedAt)
                .Select(sp => new SavedProductDto
                {
                    Id = sp.Id,
                    ProductId = sp.Product.Id,
                    Title = sp.Product.Title,
                    Slug = sp.Product.Slug,
                    SalePrice = sp.Product.SalePrice,
                    OriginalPrice = sp.Product.OriginalPrice,
                    DiscountPercentage = sp.Product.DiscountPercentage,
                    RemainingQuantity = sp.Product.Quantity - sp.Product.SoldQuantity,
                    ListingEndDate = sp.Product.ListingEndDate,
                    Status = sp.Product.Status,
                    ShopName = sp.Product.Shop.ShopName,
                    ShopCity = sp.Product.Shop.City,
                    CategoryName = sp.Product.Category != null ? sp.Product.Category.Name : null,
                    SavedAt = sp.CreatedAt,
                    MainImageUrl = sp.Product.ProductImages
                        .Where(i => i.IsMain)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                        ?? sp.Product.ProductImages.Select(i => i.ImageUrl).FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task SaveAsync(int userId, int productId)
        {
            // Fail fast with a clear message instead of letting a bad id surface
            // later as a foreign-key constraint violation from SaveChangesAsync.
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));

            // Check if already saved
            // NOTE: this check-then-insert isn't atomic - two concurrent saves for
            // the same user/product could both pass this check before either one
            // commits. A unique constraint on (UserId, ProductId) at the DB level
            // is the real guard against a duplicate row; this is just a fast path
            // to avoid the round trip in the common case.
            var exists = await _context.SavedProducts
                .AnyAsync(sp => sp.UserId == userId && sp.ProductId == productId);
            if (exists) return; // already saved, just ignore

            var saved = new SavedProduct
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.Now
            };
            _context.SavedProducts.Add(saved);

            // Increment product saves count
            var product = await _context.Products.FindAsync(productId);
            if (product != null) product.Saves++;

            await _context.SaveChangesAsync();
        }

        public async Task UnsaveAsync(int userId, int productId)
        {
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));

            var saved = await _context.SavedProducts
                .FirstOrDefaultAsync(sp =>
                    sp.UserId == userId && sp.ProductId == productId);
            if (saved == null) return;

            _context.SavedProducts.Remove(saved);

            // Decrement saves count
            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.Saves > 0) product.Saves--;

            await _context.SaveChangesAsync();
        }
    }
}