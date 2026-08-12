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

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET api/saved-products
        [HttpGet]
        public async Task<IActionResult> GetSaved()
        {
            var result = await _savedProductService.GetSavedAsync(GetUserId());
            return Ok(result);
        }

        // POST api/saved-products
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] int productId)
        {
            try
            {
                await _savedProductService.SaveAsync(GetUserId(), productId);
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
            try
            {
                await _savedProductService.UnsaveAsync(GetUserId(), productId);
                return Ok(new { message = "Product removed from saved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}