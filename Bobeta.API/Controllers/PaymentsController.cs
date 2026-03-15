using Bobeta.Application.DTOs.Payment;
using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>API for MoMo payments: deposit (request-to-pay), withdrawal (disbursement), status. Callback is unauthenticated.</summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private Guid PlayerId => Guid.Parse(User.FindFirst("playerId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

    /// <summary>Initiates a deposit (request-to-pay). Returns pending payment transaction; user confirms on their phone. Wallet is credited on callback when successful.</summary>
    [HttpPost("deposit")]
    [Authorize]
    public async Task<ActionResult<PaymentTransactionDto>> Deposit([FromBody] PaymentDepositRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.RequestDepositAsync(PlayerId, request.PhoneNumber, request.Amount, cancellationToken);
            return Ok(result);
        }
        catch (MoMoApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new { error = ex.Message, details = ex.ResponseBody });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Initiates a withdrawal (disbursement). Validates wallet balance and creates pending transaction; wallet is debited only when MoMo confirms success.</summary>
    [HttpPost("withdraw")]
    [Authorize]
    public async Task<ActionResult<PaymentTransactionDto>> Withdraw([FromBody] PaymentWithdrawRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _paymentService.RequestWithdrawalAsync(PlayerId, request.PhoneNumber, request.Amount, cancellationToken);
            return Ok(result);
        }
        catch (MoMoApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new { error = ex.Message, details = ex.ResponseBody });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Gets the status of a payment transaction by id. May poll MoMo and update wallet on success.</summary>
    [HttpGet("status/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PaymentTransactionDto>> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CheckTransactionStatusAsync(id, cancellationToken);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>MoMo callback endpoint. Called by MTN when payment status changes. Requires X-Reference-Id; rejects if transaction does not exist. Idempotent for duplicate callbacks.</summary>
    [HttpPost("momo/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoCallback(CancellationToken cancellationToken)
    {
        // Part 1 — Callback security: require X-Reference-Id header
        string? referenceId = Request.Headers["X-Reference-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(referenceId))
            return BadRequest(new { error = "X-Reference-Id header is required." });

        MoMoCallbackRequest model;
        try
        {
            await using var stream = Request.Body;
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                model = new MoMoCallbackRequest { ReferenceId = referenceId };
            }
            else
            {
                model = System.Text.Json.JsonSerializer.Deserialize<MoMoCallbackRequest>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new MoMoCallbackRequest();
                if (string.IsNullOrEmpty(model.ReferenceId))
                    model.ReferenceId = referenceId;
                if (model.ReasonCode == null && body.Contains("reason", StringComparison.OrdinalIgnoreCase))
                {
                    var reason = System.Text.Json.JsonDocument.Parse(body);
                    if (reason.RootElement.TryGetProperty("reason", out var r))
                    {
                        if (r.TryGetProperty("code", out var c)) model.ReasonCode = c.GetString();
                        if (r.TryGetProperty("message", out var m)) model.ReasonMessage = m.GetString();
                    }
                }
            }
        }
        catch
        {
            model = new MoMoCallbackRequest { ReferenceId = referenceId };
        }

        var result = await _paymentService.HandleMoMoCallbackAsync(model, cancellationToken);
        // Reject if transaction does not exist (validation: confirm PaymentTransaction with that reference exists)
        if (result == CallbackHandleResult.NotFound)
            return NotFound(new { error = "Payment transaction not found for the given reference." });
        // Part 2 — Idempotency: AlreadyProcessed and Processed both return success
        return Ok();
    }
}
