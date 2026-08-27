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
            // Projecting straight to CategoryDto (instead of Include + manual mapping
            // afterward) lets EF generate SQL that only pulls the columns the DTO
            // actually uses - subcategories never touch Description/IsActive/etc,
            // so those columns are never fetched for them. Nothing is tracked either,
            // since none of this gets modified.
            //
            // Note: subcategories aren't filtered by IsActive here, same as the
            // original - if a child category gets deactivated it'll still show up
            // under its parent. Flagging it, not changing it.
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ParentCategoryId == null)
                .OrderBy(c => c.SortOrder)
                .Select(c => new CategoryDto
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
                })
                .ToListAsync();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            // No point round-tripping to the DB for an id that can't exist.
            if (id <= 0) return null;

            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new CategoryDto
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
                })
                .FirstOrDefaultAsync();
        }
    }
}