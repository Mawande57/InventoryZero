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

        // Gets the logged in user's ID from the JWT token
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await _userService.GetProfileAsync(GetUserId());
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        // PUT api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var result = await _userService.UpdateProfileAsync(GetUserId(), dto);
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
            try
            {
                var result = await _userService.AddAddressAsync(GetUserId(), dto);
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
            try
            {
                await _userService.DeleteAddressAsync(GetUserId(), id);
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
            try
            {
                await _userService.SetDefaultAddressAsync(GetUserId(), id);
                return Ok(new { message = "Default address updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}