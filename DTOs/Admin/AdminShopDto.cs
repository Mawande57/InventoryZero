namespace InventoryZeroAPI.DTOs.Admin
{
    public class AdminShopDto
    {
        public int Id { get; set; }
        public string ShopName { get; set; } = null!;
        public string? ShopDescription { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string Status { get; set; } = null!;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalProducts { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public string OwnerName { get; set; } = null!;
        public string OwnerEmail { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VerificationNotes { get; set; }
        public DateTime? VerificationDate { get; set; }
    }
}