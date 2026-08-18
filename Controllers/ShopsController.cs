using InventoryZeroAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/shops")]
    public class ShopsController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopsController(IShopService shopService)
        {
            _shopService = shopService;
        }

        // GET api/shops/1
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shop = await _shopService.GetByIdAsync(id);

            if (shop == null)
                return NotFound(new { message = "Shop not found." });

            return Ok(shop);
        }

        // GET api/shops/1/products
        [HttpGet("{id}/products")]
        public async Task<IActionResult> GetShopProducts(int id)
        {
            var products = await _shopService.GetShopProductsAsync(id);
            return Ok(products);
        }
        // In ShopsController.cs
        /*[HttpPost("{id}/contact")]
        [Authorize]
        public async Task<IActionResult> ContactShop(int id, [FromBody] ContactMessageDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                await _shopService.ContactShopAsync(id, userId, dto.Message);
                return Ok(new { message = "Message sent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }*/
    }
}