// Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
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
            var result = await _adminService.GetShopsAsync(status, page, pageSize);
            return Ok(result);
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
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _adminService.ApproveShopAsync(id, adminId, dto);
                return Ok(new { message = "Shop approved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("shops/{id}/reject")]
        public async Task<IActionResult> RejectShop(int id, [FromBody] ShopRejectionDto dto)
        {
            try
            {
                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _adminService.RejectShopAsync(id, adminId, dto.Reason);
                return Ok(new { message = "Shop rejected." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── USER MANAGEMENT ──────────────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetUsersAsync(role, page, pageSize);
            return Ok(result);
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                await _adminService.ToggleUserStatusAsync(id);
                return Ok(new { message = "User status updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            try
            {
                await _adminService.ChangeUserRoleAsync(id, dto.Role);
                return Ok(new { message = "User role updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── PRODUCT MANAGEMENT ──────────────────────────────────
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetProductsAsync(status, page, pageSize);
            return Ok(result);
        }

        [HttpPut("products/{id}/toggle")]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            try
            {
                await _adminService.ToggleProductStatusAsync(id);
                return Ok(new { message = "Product status updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── ORDERS ────────────────────────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetOrdersAsync(status, page, pageSize);
            return Ok(result);
        }

        // ── PAYOUTS ──────────────────────────────────────────────
        [HttpGet("payouts")]
        public async Task<IActionResult> GetPayouts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetPayoutsAsync(status, page, pageSize);
            return Ok(result);
        }

        [HttpPost("payouts/process")]
        public async Task<IActionResult> ProcessPayouts()
        {
            try
            {
                var result = await _adminService.ProcessPendingPayoutsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}