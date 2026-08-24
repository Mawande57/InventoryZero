// Create Controllers/SellerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryZeroAPI.Services;
using InventoryZeroAPI.DTOs.Seller;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/seller")]
    [Authorize]
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;

        public SellerController(ISellerService sellerService)
        {
            _sellerService = sellerService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET api/seller/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _sellerService.GetStatsAsync(GetUserId());
            return Ok(stats);
        }

        // GET api/seller/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status)
        {
            var orders = await _sellerService.GetOrdersAsync(GetUserId(), status);
            return Ok(orders);
        }

        // PUT api/seller/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            try
            {
                await _sellerService.UpdateOrderStatusAsync(id, GetUserId(), dto.Status, dto.TrackingNumber);
                return Ok(new { message = "Order status updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/seller/orders/{id}/cancel
        [HttpPut("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                await _sellerService.CancelOrderAsync(id, GetUserId());
                return Ok(new { message = "Order cancelled." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/products
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _sellerService.GetProductsAsync(GetUserId());
            return Ok(products);
        }

        // Controllers/SellerController.cs

        // POST api/seller/products
        [HttpPost("products")]
        [Consumes("multipart/form-data")]  // ✅ Add this
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)  // ✅ Change [FromBody] to [FromForm]
        {
            try
            {
                Console.WriteLine($"📦 Creating product: {dto.Title}, ShopId: {dto.ShopId}, Images: {dto.Images?.Count ?? 0}");
                var result = await _sellerService.CreateProductAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/seller/products/{id}
        [HttpPut("products/{id}")]
        [Consumes("multipart/form-data")]  // ✅ Add this
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] CreateProductDto dto)  // ✅ Change [FromBody] to [FromForm]
        {
            try
            {
                Console.WriteLine($"📦 Updating product: {id}, Images: {dto.Images?.Count ?? 0}");
                await _sellerService.UpdateProductAsync(id, GetUserId(), dto);
                return Ok(new { message = "Product updated." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/seller/products/{id}
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _sellerService.DeleteProductAsync(id, GetUserId());
                return Ok(new { message = "Product deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/shops
        [HttpGet("shops")]
        public async Task<IActionResult> GetShops()
        {
            var shops = await _sellerService.GetShopsAsync(GetUserId());
            return Ok(shops);
        }

        // GET api/seller/shops/verified - ONLY verified shops (product creation)
        [HttpGet("shops/verified")]
        public async Task<IActionResult> GetVerifiedShops()
        {
            var shops = await _sellerService.GetVerifiedShopsAsync(GetUserId());
            return Ok(shops);
        }

        // POST api/seller/shops
        [HttpPost("shops")]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopDto dto)
        {
            try
            {
                var result = await _sellerService.CreateShopAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/payouts
        [HttpGet("payouts")]
        public async Task<IActionResult> GetPayouts()
        {
            var payouts = await _sellerService.GetPayoutsAsync(GetUserId());
            return Ok(payouts);
        }
    }
}