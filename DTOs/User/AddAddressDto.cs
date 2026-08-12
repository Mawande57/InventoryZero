namespace InventoryZeroAPI.DTOs.User
{
    public class AddAddressDto
    {
        public string AddressLine1 { get; set; } = null!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string RecipientName { get; set; } = null!;
        public bool IsDefault { get; set; }
        public string AddressType { get; set; } = "Home";
    }
}