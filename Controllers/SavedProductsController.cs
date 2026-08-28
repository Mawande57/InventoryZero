using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryZeroAPI.Services;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/saved-products")]
    [Authorize]
    public class SavedProductsController : ControllerBase
    {
        private readonly ISavedProductService _savedProductService;

        public SavedProductsController(ISavedProductService savedProductService)
        {
            _savedProductService = savedProductService;
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

        // GET api/saved-products
        [HttpGet]
        public async Task<IActionResult> GetSaved()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var result = await _savedProductService.GetSavedAsync(userId);
            return Ok(result);
        }

        // POST api/saved-products
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] int productId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _savedProductService.SaveAsync(userId, productId);
                return Ok(new { message = "Product saved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/saved-products/{productId}
        [HttpDelete("{productId}")]
        public async Task<IActionResult> Unsave(int productId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _savedProductService.UnsaveAsync(userId, productId);
                return Ok(new { message = "Product removed from saved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}