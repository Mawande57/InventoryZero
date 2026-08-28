using InventoryZeroAPI.DTOs.Auth;
using InventoryZeroAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                _logger.LogInformation("New account registered: {Email} (UserId {UserId}) from {RemoteIp}.",
                    dto?.Email, result.UserId, HttpContext.Connection.RemoteIpAddress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Warning, not Error - a duplicate email or bad input is expected
                // user behavior, not a system fault. Repeated attempts against
                // different emails from the same IP is the kind of pattern worth
                // watching for (account enumeration probing).
                _logger.LogWarning("Registration failed for {Email} from {RemoteIp}: {Reason}",
                    dto?.Email, HttpContext.Connection.RemoteIpAddress, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                _logger.LogInformation("Successful login: {Email} (UserId {UserId}) from {RemoteIp}.",
                    dto?.Email, result.UserId, HttpContext.Connection.RemoteIpAddress);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // The client always gets the same generic message regardless of
                // whether the email or the password was wrong - that's intentional,
                // it stops the response itself from confirming which accounts
                // exist. The email + IP still get logged server-side though, so
                // credential stuffing against one account or brute forcing from
                // one IP shows up in the logs even though the HTTP response
                // stays deliberately vague.
                _logger.LogWarning("Login failed for {Email} from {RemoteIp}: {Reason}",
                    dto?.Email, HttpContext.Connection.RemoteIpAddress, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}