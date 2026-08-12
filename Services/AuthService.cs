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
            // 1. Check if email already exists
            var exists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email);

            if (exists)
                throw new Exception("Email already registered.");

            // 2. Hash the password — NEVER store plain text
            var hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Create the user
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = hash,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 4. Generate JWT token and return
            return GenerateResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // 1. Find the user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                throw new Exception("Invalid email or password.");

            // 2. Verify password against stored hash
            var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!valid)
                throw new Exception("Invalid email or password.");

            // 3. Update last login
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // 4. Return token
            return GenerateResponse(user);
        }

        // Builds the JWT token and response
        private AuthResponseDto GenerateResponse(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims are what we EMBED inside the token
            // This means we can read UserId and Role from the token later
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
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
                Role = user.Role
            };
        }
    }
}