namespace InventoryZeroAPI.DTOs.Auth
{
    public class RegisterDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = "Buyer"; // default Buyer
    }
}