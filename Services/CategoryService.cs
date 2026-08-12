using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Categories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly InventoryZeroDbContext _context;

        public CategoryService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            // Get only parent categories (no parent themselves)
            // and include their children
            var categories = await _context.Categories
                .Where(c => c.IsActive && c.ParentCategoryId == null)
                .Include(c => c.InverseParentCategory) // this is the children
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            return categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconUrl = c.IconUrl,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
                SubCategories = c.InverseParentCategory.Select(sub => new CategoryDto
                {
                    Id = sub.Id,
                    Name = sub.Name,
                    Slug = sub.Slug,
                    IconUrl = sub.IconUrl,
                    SortOrder = sub.SortOrder
                }).ToList()
            }).ToList();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var c = await _context.Categories
                .Include(c => c.InverseParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (c == null) return null;

            return new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                IconUrl = c.IconUrl,
                ParentCategoryId = c.ParentCategoryId,
                SortOrder = c.SortOrder,
                SubCategories = c.InverseParentCategory.Select(sub => new CategoryDto
                {
                    Id = sub.Id,
                    Name = sub.Name,
                    Slug = sub.Slug,
                    IconUrl = sub.IconUrl,
                    SortOrder = sub.SortOrder
                }).ToList()
            };
        }
    }
}