using InventoryZeroAPI.DTOs.User;

namespace InventoryZeroAPI.Services
{
    public interface IUserService
    {
        Task<UserProfileDto?> GetProfileAsync(int userId);
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<UserAddressDto> AddAddressAsync(int userId, AddAddressDto dto);
        Task DeleteAddressAsync(int userId, int addressId);
        Task SetDefaultAddressAsync(int userId, int addressId);
    }
}