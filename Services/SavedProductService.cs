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
            if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
            if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));

            var exists = await _context.SavedProducts
                .AnyAsync(sp => sp.UserId == userId && sp.ProductId == productId);
            if (exists) return;

            var saved = new SavedProduct
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow  // ← FIXED
            };
            _context.SavedProducts.Add(saved);

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

            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.Saves > 0) product.Saves--;

            await _context.SaveChangesAsync();
        }
    }
}