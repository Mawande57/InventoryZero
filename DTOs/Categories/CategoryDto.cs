using System.Collections.Generic;

namespace InventoryZeroAPI.DTOs.Categories
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? ParentCategoryId { get; set; }
        public int SortOrder { get; set; }
        public List<CategoryDto> SubCategories { get; set; } = new();
    }
}