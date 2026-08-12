namespace InventoryZeroAPI.DTOs.User
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
}