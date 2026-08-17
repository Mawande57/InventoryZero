using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using InventoryZeroAPI.Data;

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
            var passphrase = _config["PayFast:Passphrase"] ?? "";
            var isSandbox = bool.Parse(_config["PayFast:IsSandbox"]!);
            var baseUrl = _config["PayFast:BaseUrl"]!;

            var paymentUrl = isSandbox
                ? "https://sandbox.payfast.co.za/eng/process"
                : "https://www.payfast.co.za/eng/process";

            var productName = order.OrderItems.FirstOrDefault()?.Product.Title
                ?? "InventoryZero Order";

            if (productName.Length > 100)
                productName = productName[..100];

            var nameParts = order.Buyer.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : nameParts[0];

            var amount = order.TotalAmount.ToString("0.00",
                System.Globalization.CultureInfo.InvariantCulture);

            // 🔑 Use SortedDictionary for consistent ordering
            var pfData = new SortedDictionary<string, string>
    {
        { "merchant_id", merchantId },
        { "merchant_key", merchantKey },
        { "return_url", $"{baseUrl}/api/payments/return" },
        { "cancel_url", $"{baseUrl}/api/payments/cancel" },
        { "notify_url", $"{baseUrl}/api/payments/notify" },
        { "name_first", firstName },
        { "name_last", lastName },
        { "email_address", order.Buyer.Email },
        { "m_payment_id", order.OrderNumber },
        { "amount", amount },
        { "item_name", productName },
        { "item_description", $"Order {order.OrderNumber}" }
    };

            // Generate signature using the sorted data
            var signature = GeneratePayFastSignature(pfData, passphrase);

            // 🔑 Add signature to the sorted data
            pfData.Add("signature", signature);

            // Log for debugging
            Console.WriteLine("=== FORM DATA (sorted) ===");
            foreach (var kvp in pfData)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
            Console.WriteLine("==========================");

            return Ok(new
            {
                paymentUrl = paymentUrl,
                formData = pfData,  // This will now be in alphabetical order
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                amount = order.TotalAmount
            });
        }

        private string GeneratePayFastSignature(SortedDictionary<string, string> data, string passphrase)
        {
            // Step 1: Build the query string with URL ENCODING
            var queryString = string.Join("&", data
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

            // Step 2: Add passphrase
            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                queryString += $"&passphrase={Uri.EscapeDataString(passphrase)}";
            }

            // Step 3: Calculate MD5 hash
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(queryString);
            var hashBytes = md5.ComputeHash(inputBytes);
            var signature = string.Concat(hashBytes.Select(b => b.ToString("x2")));

            // Log for debugging
            Console.WriteLine("=== SIGNATURE DEBUG ===");
            Console.WriteLine("Query String: " + queryString);
            Console.WriteLine("Signature: " + signature);
            Console.WriteLine("=======================");

            return signature;
        }

        // 🔑 EXACT PayFast signature generation method
        private string GeneratePayFastSignature(Dictionary<string, string> data, string passphrase)
        {
            // Step 1: Sort the data alphabetically by key
            var sortedData = data
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToList();

            // Step 2: Build the query string with URL ENCODING
            var queryString = string.Join("&", sortedData
                .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));

            // Step 3: Add passphrase (CRITICAL - if set in PayFast)
            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                queryString += $"&passphrase={Uri.EscapeDataString(passphrase)}";
            }

            // Step 4: Calculate MD5 hash
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(queryString);
            var hashBytes = md5.ComputeHash(inputBytes);
            var signature = string.Concat(hashBytes.Select(b => b.ToString("x2")));

            // Log for debugging
            Console.WriteLine("=== SIGNATURE DEBUG ===");
            Console.WriteLine("Query String: " + queryString);
            Console.WriteLine("Signature: " + signature);
            Console.WriteLine("=======================");

            return signature;
        }
        [HttpPost("notify")]
        public async Task<IActionResult> Notify([FromForm] IFormCollection form)
        {
            try
            {
                var paymentStatus = form["payment_status"].ToString();
                var orderNumber = form["m_payment_id"].ToString();
                var pfPaymentId = form["pf_payment_id"].ToString();

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                if (order == null) return Ok();

                if (paymentStatus == "COMPLETE")
                {
                    order.PaymentStatus = "Paid";
                    order.OrderStatus = "Processing";
                    order.PaymentIntentId = pfPaymentId;
                    order.PaymentMethod = "payfast";
                    order.PaidAt = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    var payout = await _context.Payouts
                        .FirstOrDefaultAsync(p => p.OrderId == order.Id);

                    if (payout != null)
                        payout.Status = "Processing";

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
                return Ok();
            }
        }

        [HttpGet("return")]
        public IActionResult Return([FromQuery] string? m_payment_id)
        {
            var baseUrl = _config["PayFast:BaseUrl"]!;
            return Redirect($"{baseUrl}/pages/order-success.html?order={m_payment_id}");
        }

        [HttpGet("cancel")]
        public IActionResult Cancel()
        {
            var baseUrl = _config["PayFast:BaseUrl"]!;
            return Redirect($"{baseUrl}/pages/checkout.html");
        }

        private string GetMd5Hash(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return string.Concat(hashBytes.Select(b => b.ToString("x2")));
        }
    }
}