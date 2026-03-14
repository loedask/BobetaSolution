using Bobeta.Application.DTOs.Auth;
using Bobeta.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        await _authService.SendOtpAsync(request.PhoneNumber, cancellationToken);
        return Accepted();
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
