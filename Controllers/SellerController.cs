// Controllers/SellerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SellerController> _logger;

        public SellerController(ISellerService sellerService, ILogger<SellerController> logger)
        {
            _sellerService = sellerService;
            _logger = logger;
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

        // GET api/seller/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var stats = await _sellerService.GetStatsAsync(userId);
            return Ok(stats);
        }

        // GET api/seller/orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] string? status)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var orders = await _sellerService.GetOrdersAsync(userId, status);
            return Ok(orders);
        }

        // PUT api/seller/orders/{id}/status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _sellerService.UpdateOrderStatusAsync(id, userId, dto.Status, dto.TrackingNumber);
                _logger.LogInformation("Seller {UserId} updated order {OrderId} to status {Status}.", userId, id, dto.Status);
                return Ok(new { message = "Order status updated." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Seller {UserId} failed to update order {OrderId} to status {Status}: {Reason}",
                    userId, id, dto?.Status, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/seller/orders/{id}/cancel
        [HttpPut("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _sellerService.CancelOrderAsync(id, userId);
                _logger.LogInformation("Seller {UserId} cancelled order {OrderId}.", userId, id);
                return Ok(new { message = "Order cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Seller {UserId} failed to cancel order {OrderId}: {Reason}", userId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/products
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var products = await _sellerService.GetProductsAsync(userId);
            return Ok(products);
        }

        // GET api/seller/products/{id} - Get single product for editing
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var product = await _sellerService.GetProductByIdAsync(id, userId);
                if (product == null)
                    return NotFound(new { message = "Product not found." });
                return Ok(product);
            }
            catch (Exception ex)
            {
                // Covers the ownership check inside GetProductByIdAsync - a seller
                // requesting a product they don't own lands here too. Worth a
                // warning either way: either a genuinely missing product, or
                // someone probing another seller's listing.
                _logger.LogWarning(ex, "Seller {UserId} could not access product {ProductId}: {Reason}", userId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/seller/products - Create product with images
        [HttpPost("products")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var result = await _sellerService.CreateProductAsync(userId, dto);
                _logger.LogInformation("Seller {UserId} created product {@Result} in shop {ShopId} with {ImageCount} image(s).",
                    userId, result, dto.ShopId, dto.Images?.Count ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Seller {UserId} failed to create product '{Title}' in shop {ShopId}: {Reason}",
                    userId, dto?.Title, dto?.ShopId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/seller/products/{id} - Update product with images
        [HttpPut("products/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] CreateProductDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var updatedProduct = await _sellerService.UpdateProductAsync(id, userId, dto);
                _logger.LogInformation("Seller {UserId} updated product {ProductId}.", userId, id);
                return Ok(new
                {
                    message = "Product updated.",
                    product = updatedProduct
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Seller {UserId} failed to update product {ProductId}: {Reason}", userId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/seller/products/{id}
        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                await _sellerService.DeleteProductAsync(id, userId);
                // Destructive and irreversible (also deletes the image files on
                // disk) - worth a record even on the happy path, not just failures.
                _logger.LogInformation("Seller {UserId} deleted product {ProductId}.", userId, id);
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Seller {UserId} failed to delete product {ProductId}: {Reason}", userId, id, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/shops
        [HttpGet("shops")]
        public async Task<IActionResult> GetShops()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var shops = await _sellerService.GetShopsAsync(userId);
            return Ok(shops);
        }

        // GET api/seller/shops/verified - ONLY verified shops (product creation)
        [HttpGet("shops/verified")]
        public async Task<IActionResult> GetVerifiedShops()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var shops = await _sellerService.GetVerifiedShopsAsync(userId);
            return Ok(shops);
        }

        // POST api/seller/shops
        [HttpPost("shops")]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            try
            {
                var result = await _sellerService.CreateShopAsync(userId, dto);
                _logger.LogInformation("User {UserId} created a new shop: {@Result}", userId, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "User {UserId} failed to create a shop: {Reason}", userId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/seller/payouts
        [HttpGet("payouts")]
        public async Task<IActionResult> GetPayouts()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var payouts = await _sellerService.GetPayoutsAsync(userId);
            return Ok(payouts);
        }
    }
}