using Bobeta.Application.DTOs.Payment;
using Bobeta.Application.Interfaces;
using Bobeta.Infrastructure.MoMo;
using Bobeta.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>API for MoMo payments: deposit (request-to-pay), withdrawal (disbursement), status. Callback validates headers and is unauthenticated.</summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService paymentService, Microsoft.Extensions.Options.IOptions<MoMoSettings> moMoSettings, ILogger<PaymentsController> logger) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly MoMoSettings _moMoSettings = moMoSettings.Value;
    private readonly ILogger<PaymentsController> _logger = logger;

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

    /// <summary>MoMo callback endpoint. Called by MTN when payment status changes. Validates X-Reference-Id, X-Target-Environment, Ocp-Apim-Subscription-Key; returns 401 if validation fails. Idempotent for duplicate callbacks.</summary>
    [HttpPost("momo/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoCallback(CancellationToken cancellationToken)
    {
        // Log callback headers and reference id (sanitize subscription key: do not log full value)
        string? referenceId = Request.Headers["X-Reference-Id"].FirstOrDefault();
        string? targetEnv = Request.Headers["X-Target-Environment"].FirstOrDefault();
        string? subscriptionKeyHeader = Request.Headers["Ocp-Apim-Subscription-Key"].FirstOrDefault();
        bool subscriptionKeyPresent = !string.IsNullOrEmpty(subscriptionKeyHeader);
        string subscriptionKeyHint = subscriptionKeyPresent && subscriptionKeyHeader!.Length >= 4
            ? "***" + subscriptionKeyHeader[^4..]
            : (subscriptionKeyPresent ? "***" : "missing");
        _logger.LogInformation(
            "MoMo callback received: X-Reference-Id={ReferenceId}, X-Target-Environment={TargetEnvironment}, Ocp-Apim-Subscription-Key={SubscriptionKeyHint}",
            referenceId ?? "(missing)",
            targetEnv ?? "(missing)",
            subscriptionKeyHint);

        // Validation 1: X-Reference-Id must exist
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            _logger.LogWarning("MoMo callback rejected: X-Reference-Id header missing.");
            return Unauthorized(new { error = "Callback validation failed." });
        }

        // Validation 2: X-Target-Environment must match configuration
        if (string.IsNullOrEmpty(_moMoSettings.TargetEnvironment) || !string.Equals(targetEnv, _moMoSettings.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("MoMo callback rejected: X-Target-Environment mismatch. Expected={Expected}, Received={Received}.", _moMoSettings.TargetEnvironment, targetEnv ?? "(missing)");
            return Unauthorized(new { error = "Callback validation failed." });
        }

        // Validation 3: Ocp-Apim-Subscription-Key must match configured key
        if (string.IsNullOrEmpty(_moMoSettings.CallbackSubscriptionKey) || !string.Equals(subscriptionKeyHeader, _moMoSettings.CallbackSubscriptionKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("MoMo callback rejected: Ocp-Apim-Subscription-Key mismatch for ReferenceId={ReferenceId}.", referenceId);
            return Unauthorized(new { error = "Callback validation failed." });
        }

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
