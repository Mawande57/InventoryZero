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
            var saved = await _context.SavedProducts
                .Include(sp => sp.Product)
                    .ThenInclude(p => p.Shop)
                .Include(sp => sp.Product)
                    .ThenInclude(p => p.Category)
                .Include(sp => sp.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(sp => sp.UserId == userId)
                .OrderByDescending(sp => sp.CreatedAt)
                .ToListAsync();

            return saved.Select(sp => new SavedProductDto
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
                CategoryName = sp.Product.Category?.Name,
                SavedAt = sp.CreatedAt,
                MainImageUrl = sp.Product.ProductImages
                    .FirstOrDefault(i => i.IsMain)?.ImageUrl
                    ?? sp.Product.ProductImages.FirstOrDefault()?.ImageUrl
            }).ToList();
        }

        public async Task SaveAsync(int userId, int productId)
        {
            // Check if already saved
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