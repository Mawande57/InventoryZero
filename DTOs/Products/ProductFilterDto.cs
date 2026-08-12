namespace InventoryZeroAPI.DTOs.Products
{
    public class ProductFilterDto
    {
        public string? Search { get; set; }        // search by title
        public string? CategorySlug { get; set; }  // filter by category
        public string? City { get; set; }          // filter by location
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Condition { get; set; }     // New, Refurbished etc
        public bool? IsUrgent { get; set; }        // urgent deals only
        public string SortBy { get; set; } = "newest"; // newest, price-asc, price-desc, discount
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}