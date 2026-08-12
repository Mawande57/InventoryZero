using Microsoft.EntityFrameworkCore;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.User;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Services
{
    public class UserService : IUserService
    {
        private readonly InventoryZeroDbContext _context;

        public UserService(InventoryZeroDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserAddresses)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return null;

            return MapToDto(user);
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserAddresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new Exception("User not found.");

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;

            await _context.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task<UserAddressDto> AddAddressAsync(int userId, AddAddressDto dto)
        {
            // If this is set as default, remove default from others first
            if (dto.IsDefault)
            {
                var existing = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                existing.ForEach(a => a.IsDefault = false);
            }

            var address = new UserAddress
            {
                UserId = userId,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                Province = dto.Province,
                PostalCode = dto.PostalCode,
                Country = "South Africa",
                PhoneNumber = dto.PhoneNumber,
                RecipientName = dto.RecipientName,
                IsDefault = dto.IsDefault,
                AddressType = dto.AddressType,
                CreatedAt = DateTime.Now
            };

            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();

            return new UserAddressDto
            {
                Id = address.Id,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Country = address.Country,
                PhoneNumber = address.PhoneNumber,
                RecipientName = address.RecipientName,
                IsDefault = address.IsDefault,
                AddressType = address.AddressType
            };
        }

        public async Task DeleteAddressAsync(int userId, int addressId)
        {
            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) throw new Exception("Address not found.");

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
        }

        public async Task SetDefaultAddressAsync(int userId, int addressId)
        {
            // Remove default from all
            var all = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .ToListAsync();

            all.ForEach(a => a.IsDefault = false);

            // Set new default
            var target = all.FirstOrDefault(a => a.Id == addressId);
            if (target == null) throw new Exception("Address not found.");

            target.IsDefault = true;
            await _context.SaveChangesAsync();
        }

        private UserProfileDto MapToDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsEmailVerified = user.IsEmailVerified,
                Rating = user.Rating,
                TotalReviews = user.TotalReviews,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Addresses = user.UserAddresses.Select(a => new UserAddressDto
                {
                    Id = a.Id,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    Province = a.Province,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    PhoneNumber = a.PhoneNumber,
                    RecipientName = a.RecipientName,
                    IsDefault = a.IsDefault,
                    AddressType = a.AddressType
                }).ToList()
            };
        }
    }
}