namespace InventoryZeroAPI.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;      // JWT token
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;        // Buyer/Seller/Admin
        public int UserId { get; set; }
    }
}