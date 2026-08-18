using InventoryZeroAPI.Data;
using InventoryZeroAPI.DTOs.Auth;
using InventoryZeroAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly InventoryZeroDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(InventoryZeroDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var exists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (exists)
                throw new Exception("Email already registered.");

            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = hash,
                PhoneNumber = dto.PhoneNumber,
                Role = "Buyer",
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Brand new user, so this is always false — but keep it explicit for clarity
            var hasShop = await _context.Shops.AnyAsync(s => s.UserId == user.Id);

            return GenerateResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                throw new Exception("Invalid email or password.");

            var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!valid)
                throw new Exception("Invalid email or password.");

            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var hasShop = await _context.Shops.AnyAsync(s => s.UserId == user.Id);

            return GenerateResponse(user);
        }

        // Builds the JWT token and response
        private AuthResponseDto GenerateResponse(User user)
        {
            var hasShop =  _context.Shops.Any(s => s.UserId == user.Id);
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims are what we EMBED inside the token
            // This means we can read UserId and Role from the token later
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("hasShop", hasShop.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                HasShop = hasShop
            };
        }
    }
}