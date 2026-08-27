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
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // AnyAsync translates straight to an EXISTS query - cheap, and lets us
            // fail before we ever touch BCrypt or open an insert.
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

            // A brand-new account can't own a shop yet - pass false straight in
            // instead of running a Shops query that can only ever come back empty.
            return GenerateResponse(user, hasShop: false);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                throw new Exception("Invalid email or password.");

            var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!valid)
                throw new Exception("Invalid email or password.");

            if (!user.IsActive)
                throw new Exception("The Admin deActivated your account for malicious activity contact the admin");

            user.LastLoginAt = DateTime.Now;

            // Computed once here and passed into GenerateResponse - previously this was
            // queried a second time (synchronously, blocking a thread) inside that method.
            var hasShop = await _context.Shops.AnyAsync(s => s.UserId == user.Id);

            await _context.SaveChangesAsync();

            return GenerateResponse(user, hasShop);
        }

        // Builds the JWT token and response.
        // hasShop is passed in rather than queried here - this method used to run
        // _context.Shops.Any(...) synchronously (sync-over-async), which blocks a
        // thread pool thread on every login/register and duplicated a query the
        // callers had usually already made (or, for Register, didn't need at all).
        private AuthResponseDto GenerateResponse(User user, bool hasShop)
        {
            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT signing key is not configured (Jwt:Key).");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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