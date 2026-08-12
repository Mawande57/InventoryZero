namespace InventoryZeroAPI.DTOs.User
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = null!;
        public string? ProfilePictureUrl { get; set; }
        public bool IsEmailVerified { get; set; }
        public decimal Rating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<UserAddressDto> Addresses { get; set; } = new();
    }
}