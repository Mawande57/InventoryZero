using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using InventoryZeroAPI.Data;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly InventoryZeroDbContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(InventoryZeroDbContext context, ILogger<PaymentsController> logger)
        {
            _context = context;
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

        // POST api/payments/process/{orderId}
        //
        // IMPORTANT - NOT ACTUALLY WIRED UP: this endpoint marks the order as paid
        // without verifying that a payment happened anywhere. There's no call to a
        // payment gateway, no payment token/intent verification, and no webhook
        // confirmation. As it stands, any authenticated buyer can call this on
        // their own pending order and get it marked "Paid" for free. Before this
        // goes anywhere near production, this needs to either verify a payment
        // token against the provider's API, or (better) only ever be triggered by
        // a server-to-server webhook from the payment provider after it confirms
        // the charge - not by the client simply calling this route.
        [HttpPost("process/{orderId}")]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            if (!TryGetUserId(out var userId))
            {
                _logger.LogWarning("Payment processing blocked for order {OrderId} - could not resolve user id from token claims.", orderId);
                return Unauthorized(new { message = "Could not identify the current user." });
            }

            // Tracked - PaymentStatus/OrderStatus/PaidAt etc. are set directly
            // below and saved. Scoping to BuyerId == userId here already prevents
            // paying for (or marking paid) someone else's order.
            var order = await _context.Orders
                .Include(o => o.Payouts)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.BuyerId == userId &&
                    o.PaymentStatus == "Pending");

            if (order == null)
            {
                _logger.LogWarning(
                    "Payment attempt rejected: order {OrderId} for user {UserId} was not found, not owned by this user, or already paid.",
                    orderId, userId);
                return NotFound(new { message = "Order not found or already paid." });
            }

            try
            {
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

                _logger.LogInformation("User {UserId} paid order {OrderId} ({OrderNumber}), amount {Amount}.",
                    userId, order.Id, order.OrderNumber, order.TotalAmount);

                return Ok(new
                {
                    message = "Payment successful.",
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    amountPaid = order.TotalAmount
                });
            }
            catch (DbUpdateException dbEx)
            {
                // No exception handling existed here before at all - a DB failure
                // mid-payment would have surfaced as an unhandled 500 with nothing
                // recorded server-side. Logged in full here; the client only gets
                // a generic message, not the raw DB error.
                _logger.LogError(dbEx, "Database error marking order {OrderId} as paid for user {UserId}.", orderId, userId);
                return BadRequest(new { message = "We couldn't process your payment due to a database error. Please try again." });
            }
        }
    }
}