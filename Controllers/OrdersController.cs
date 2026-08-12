using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InventoryZeroAPI.DTOs.Orders;
using InventoryZeroAPI.Services;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // POST api/orders
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
        {
            try
            {
                var result = await _orderService.PlaceOrderAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/orders
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var result = await _orderService.GetMyOrdersAsync(GetUserId());
            return Ok(result);
        }

        // GET api/orders/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var result = await _orderService.GetOrderDetailAsync(id, GetUserId());
            if (result == null)
                return NotFound(new { message = "Order not found." });
            return Ok(result);
        }
    }
}