namespace InventoryZeroAPI.DTOs.Seller
{
    public class CreateShopDto
    {
        public string ShopName { get; set; } = null!;
        public string? ShopDescription { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PhoneNumber { get; set; }
    }
}