using Bobeta.Application.DTOs.Auth;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Bobeta.API.Controllers;

/// <summary>API for phone-based authentication: send OTP, verify OTP, register player.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger,
    IHostEnvironment hostEnvironment) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.PhoneNumber))
            return BadRequest("Phone number is required.");

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        try
        {
            await _authService.SendOtpAsync(request.PhoneNumber, cancellationToken, clientIp);
            // Must be 200 OK: the generated API client (NSwag) only treats 200 as success for this operation.
            return Ok();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Too many OTP", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(429, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. SMS gateway failures (SmsGatewayException)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
        catch (Exception ex)
        {
            // DB errors, unexpected failures — were returning 500 with no useful body for the mobile client.
            _logger.LogError(ex, "SendOtp failed for {PhoneNumber}", request.PhoneNumber);
            var safeDetail = _hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment(Environments.Staging)
                ? ex.Message
                : "An unexpected error occurred. Please try again later.";
            return StatusCode(StatusCodes.Status500InternalServerError, safeDetail);
        }
    }

    /// <summary>Verifies the OTP code; returns a JWT if the player is already registered.</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyOtpAsync(request.PhoneNumber, request.Code, cancellationToken);
        if (!result.Valid) return BadRequest(result.ErrorMessage ?? "Invalid or expired OTP.");
        return Ok(new { result.Token, result.PlayerId, result.PlayerName });
    }

    /// <summary>Registers a new player (phone + name) and returns JWT and profile.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterPlayerAsync(request.PhoneNumber, request.PlayerName, cancellationToken);
        return Ok(response);
    }
}
