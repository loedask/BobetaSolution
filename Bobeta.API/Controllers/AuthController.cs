using Bobeta.Application.DTOs.Auth;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>API for phone-based authentication: send OTP, verify OTP, register player.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        try
        {
            await _authService.SendOtpAsync(request.PhoneNumber, cancellationToken, clientIp);
            return Accepted();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Too many OTP", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(429, ex.Message);
        }
    }

    /// <summary>Verifies the OTP code; returns a JWT if the player is already registered.</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyOtpAsync(request.PhoneNumber, request.Code, cancellationToken);
        if (!result.Valid) return BadRequest("Invalid or expired OTP.");
        return Ok(new { Token = result.Token });
    }

    /// <summary>Registers a new player (phone + name) and returns JWT and profile.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterPlayerAsync(request.PhoneNumber, request.PlayerName, cancellationToken);
        return Ok(response);
    }
}
