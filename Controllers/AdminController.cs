// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using InventoryZeroAPI.Services;
using InventoryZeroAPI.DTOs.Admin;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // Pulls the acting admin's id out of the token. [Authorize(Roles = "Admin")]
        // means this should always succeed for a legitimate request - if it doesn't,
        // that's a malformed/tampered token slipping past auth, worth logging and
        // refusing rather than crashing on a null-forgiving operator further down.
        private bool TryGetAdminId(out int adminId)
        {
            adminId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out adminId);
        }

        // ── DASHBOARD STATS ──────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _adminService.GetStatsAsync();
            return Ok(stats);
        }

        // ── SHOP MANAGEMENT ──────────────────────────────────────
        [HttpGet("shops")]
        public async Task<IActionResult> GetShops([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetShopsAsync(status, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Covers the page/pageSize guard clauses in AdminService - anything
                // else is unexpected and should still surface as a 500.
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("shops/{id}")]
        public async Task<IActionResult> GetShopDetail(int id)
        {
            var shop = await _adminService.GetShopDetailAsync(id);
            if (shop == null)
                return NotFound(new { message = "Shop not found." });
            return Ok(shop);
        }

        [HttpPut("shops/{id}/approve")]
        public async Task<IActionResult> ApproveShop(int id, [FromBody] ShopApprovalDto dto)
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("Shop approval blocked for shop {ShopId} - could not resolve admin id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            try
            {
                await _adminService.ApproveShopAsync(id, adminId, dto);
                _logger.LogInformation("Admin {AdminId} approved shop {ShopId}.", adminId, id);
                return Ok(new { message = "Shop approved." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin {AdminId} failed to approve shop {ShopId}: {Reason}", adminId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("shops/{id}/reject")]
        public async Task<IActionResult> RejectShop(int id, [FromBody] ShopRejectionDto dto)
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("Shop rejection blocked for shop {ShopId} - could not resolve admin id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            try
            {
                await _adminService.RejectShopAsync(id, adminId, dto.Reason);
                _logger.LogInformation("Admin {AdminId} rejected shop {ShopId}. Reason: {Reason}", adminId, id, dto.Reason);
                return Ok(new { message = "Shop rejected." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin {AdminId} failed to reject shop {ShopId}: {Reason}", adminId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── USER MANAGEMENT ──────────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetUsersAsync(role, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("User status toggle blocked for user {UserId} - could not resolve admin id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            try
            {
                await _adminService.ToggleUserStatusAsync(id);
                _logger.LogInformation("Admin {AdminId} toggled active status for user {UserId}.", adminId, id);
                return Ok(new { message = "User status updated." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin {AdminId} failed to toggle status for user {UserId}: {Reason}", adminId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("Role change blocked for user {UserId} - could not resolve admin id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            try
            {
                await _adminService.ChangeUserRoleAsync(id, dto.Role);
                // Role changes are a privilege-escalation vector, worth an audit
                // trail even on success, not just on failure.
                _logger.LogInformation("Admin {AdminId} changed role of user {UserId} to {Role}.", adminId, id, dto.Role);
                return Ok(new { message = "User role updated." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin {AdminId} failed to change role of user {UserId} to {Role}: {Reason}", adminId, id, dto.Role, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── PRODUCT MANAGEMENT ──────────────────────────────────
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetProductsAsync(status, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("products/{id}/toggle")]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("Product status toggle blocked for product {ProductId} - could not resolve admin id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            try
            {
                await _adminService.ToggleProductStatusAsync(id);
                _logger.LogInformation("Admin {AdminId} toggled active status for product {ProductId}.", adminId, id);
                return Ok(new { message = "Product status updated." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin {AdminId} failed to toggle status for product {ProductId}: {Reason}", adminId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── ORDERS ────────────────────────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetOrdersAsync(status, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── PAYOUTS ──────────────────────────────────────────────
        [HttpGet("payouts")]
        public async Task<IActionResult> GetPayouts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _adminService.GetPayoutsAsync(status, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("payouts/process")]
        public async Task<IActionResult> ProcessPayouts()
        {
            if (!TryGetAdminId(out var adminId))
            {
                _logger.LogWarning("Payout processing blocked - could not resolve admin id from token claims.");
                return Unauthorized(new { message = "Could not identify the current admin user." });
            }

            // This one moves money - log the attempt, not just the outcome.
            _logger.LogInformation("Admin {AdminId} initiated payout processing.", adminId);

            try
            {
                var result = await _adminService.ProcessPendingPayoutsAsync();
                _logger.LogInformation("Admin {AdminId} completed payout processing: {@Result}", adminId, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Error, not Warning - a payout run failing is more likely to be a
                // real problem (DB, payment provider) than a bad request.
                _logger.LogError(ex, "Admin {AdminId} payout processing failed: {Reason}", adminId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}