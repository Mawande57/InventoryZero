using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        // Was int.Parse(...) with the null-forgiving operator - if that claim was
        // ever missing (malformed/tampered token), it throws an unhandled
        // exception instead of a clean response. TryGetUserId fails safely instead.
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim) && int.TryParse(claim, out userId);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Order placement blocked - could not resolve user id from token claims.");
                return Unauthorized(new { message = "Could not identify the current user." });
            }

            try
            {
                var result = await _orderService.PlaceOrderAsync(userId, dto);
                _logger.LogInformation(
                    "User {UserId} placed order {OrderId} ({OrderNumber}) for product {ProductId}, total {TotalAmount}.",
                    userId, result.Id, result.OrderNumber, dto?.ProductId, result.TotalAmount);
                return Ok(result);
            }
            catch (DbUpdateException dbEx)
            {
                // Logged in full server-side (the exception object itself, not
                // just a message) so the real cause is traceable - but the raw
                // DB error never goes back to the client. The original code
                // returned dbEx.InnerException?.Message straight into the
                // response body, which can leak schema/constraint details to
                // whoever calls this endpoint - that's an information-disclosure
                // issue, not just a logging one, so it's fixed here rather than
                // just logged and left as-is.
                _logger.LogError(dbEx, "Database error placing order for user {UserId}, product {ProductId}.",
                    userId, dto?.ProductId);
                return BadRequest(new { message = "We couldn't place your order due to a database error. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Order placement failed for user {UserId}, product {ProductId}: {Reason}",
                    userId, dto?.ProductId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/orders
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Order list request blocked - could not resolve user id from token claims.");
                return Unauthorized(new { message = "Could not identify the current user." });
            }

            var result = await _orderService.GetMyOrdersAsync(userId);
            return Ok(result);
        }

        // GET api/orders/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Order detail request blocked for order {OrderId} - could not resolve user id from token claims.", id);
                return Unauthorized(new { message = "Could not identify the current user." });
            }

            var result = await _orderService.GetOrderDetailAsync(id, userId);
            if (result == null)
                return NotFound(new { message = "Order not found." });
            return Ok(result);
        }
    }
}