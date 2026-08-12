using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using InventoryZeroAPI.Data;
using InventoryZeroAPI.Models;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly InventoryZeroDbContext _context;
        private readonly IConfiguration _config;

        public PaymentsController(InventoryZeroDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // POST api/payments/initiate/{orderId}
        // Returns PayFast payment URL and form data
        [HttpPost("initiate/{orderId}")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.BuyerId == GetUserId() &&
                    o.PaymentStatus == "Pending");

            if (order == null)
                return NotFound(new { message = "Order not found or already paid." });

            var merchantId = _config["PayFast:MerchantId"]!;
            var merchantKey = _config["PayFast:MerchantKey"]!;
            var passphrase = _config["PayFast:Passphrase"]!;
            var isSandbox = bool.Parse(_config["PayFast:IsSandbox"]!);

            var baseUrl = isSandbox
                ? "https://sandbox.payfast.co.za/eng/process"
                : "https://www.payfast.co.za/eng/process";

            var productName = order.OrderItems.FirstOrDefault()?.Product.Title
                ?? "InventoryZero Order";

            // Build PayFast data dictionary — ORDER MATTERS
            var data = new Dictionary<string, string>
            {
                ["merchant_id"] = merchantId,
                ["merchant_key"] = merchantKey,
                ["return_url"] = "https://www.example.com/return",
                ["cancel_url"] = "https://www.example.com/cancel",
                ["notify_url"] = "https://www.example.com/notify",
                ["name_first"] = order.Buyer.FullName.Split(' ')[0],
                ["name_last"] = order.Buyer.FullName.Contains(' ')
                    ? order.Buyer.FullName.Substring(order.Buyer.FullName.IndexOf(' ') + 1)
                    : order.Buyer.FullName,
                ["email_address"] = order.Buyer.Email,
                ["m_payment_id"] = order.OrderNumber,
                ["amount"] = order.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ["item_name"] = productName.Length > 100
                    ? productName[..100]
                    : productName,
                ["item_description"] = $"InventoryZero order {order.OrderNumber}"
            };

            // Generate signature
            var signature = GenerateSignature(data, passphrase);
            data["signature"] = signature;

            return Ok(new
            {
                paymentUrl = baseUrl,
                formData = data,
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                amount = order.TotalAmount
            });
        }

        // POST api/payments/notify
        // PayFast calls this when payment is confirmed — the ITN webhook
        [HttpPost("notify")]
        public async Task<IActionResult> Notify([FromForm] IFormCollection form)
        {
            try
            {
                var paymentStatus = form["payment_status"].ToString();
                var orderNumber = form["m_payment_id"].ToString();
                var pfPaymentId = form["pf_payment_id"].ToString();

                // Find the order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                if (order == null) return Ok(); // PayFast expects 200 always

                if (paymentStatus == "COMPLETE")
                {
                    order.PaymentStatus = "Paid";
                    order.OrderStatus = "Processing";
                    order.PaymentIntentId = pfPaymentId;
                    order.PaymentMethod = "payfast";
                    order.PaidAt = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    // Update payout to processing
                    var payout = await _context.Payouts
                        .FirstOrDefaultAsync(p => p.OrderId == order.Id);

                    if (payout != null)
                    {
                        payout.Status = "Processing";
                    }

                    await _context.SaveChangesAsync();
                }
                else if (paymentStatus == "CANCELLED")
                {
                    order.PaymentStatus = "Failed";
                    order.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch
            {
                return Ok(); // Always return 200 to PayFast
            }
        }

        // GET api/payments/return
        // PayFast redirects buyer here after payment
        [HttpGet("return")]
        public IActionResult Return([FromQuery] string? m_payment_id)
        {
            // Redirect to success page
            return Redirect($"/pages/order-success.html?order={m_payment_id}");
        }

        // GET api/payments/cancel
        // PayFast redirects buyer here if they cancel
        [HttpGet("cancel")]
        public IActionResult Cancel()
        {
            return Redirect("/pages/checkout.html");
        }

        // ── SIGNATURE GENERATION ─────────────────────────────
        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            var sb = new StringBuilder();

            foreach (var kvp in data)
            {
                if (kvp.Key != "signature" && !string.IsNullOrEmpty(kvp.Value))
                {
                    sb.Append($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}&");
                }
            }

            // Add passphrase
            if (!string.IsNullOrEmpty(passphrase))
                sb.Append($"passphrase={Uri.EscapeDataString(passphrase)}");
            else
                sb.Length--; // remove trailing &

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}