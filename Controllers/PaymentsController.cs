using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using InventoryZeroAPI.Data;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly InventoryZeroDbContext _context;

        public PaymentsController(InventoryZeroDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // POST api/payments/process/{orderId}
        [HttpPost("process/{orderId}")]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Payouts)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.BuyerId == GetUserId() &&
                    o.PaymentStatus == "Pending");

            if (order == null)
                return NotFound(new { message = "Order not found or already paid." });

            // Mark as paid
            order.PaymentStatus = "Paid";
            order.OrderStatus = "Processing";
            order.PaymentMethod = "card";
            order.PaidAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;

            // Update payout
            var payout = order.Payouts.FirstOrDefault();
            if (payout != null)
                payout.Status = "Processing";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment successful.",
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                amountPaid = order.TotalAmount
            });
        }
    }
}