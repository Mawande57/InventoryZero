using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryZeroAPI.DTOs.User;
using InventoryZeroAPI.Services;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize] // ALL endpoints here require login
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Was int.Parse(...) with the null-forgiving operator - a missing or
        // malformed claim would throw an unhandled exception instead of a clean
        // response. TryGetUserId fails safely instead.
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
        }

        // GET api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        // PUT api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var result = await _userService.UpdateProfileAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/user/addresses
        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var result = await _userService.AddAddressAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/user/addresses/3
        [HttpDelete("addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _userService.DeleteAddressAsync(userId, id);
                return Ok(new { message = "Address deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/user/addresses/3/default
        [HttpPut("addresses/{id}/default")]
        public async Task<IActionResult> SetDefault(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _userService.SetDefaultAddressAsync(userId, id);
                return Ok(new { message = "Default address updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}