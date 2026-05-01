using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>SMS delivery report (DLR) webhook and status. SendSMSGate calls POST /api/sms/dlr with provider message ID and status.</summary>
[ApiController]
[Route("api/[controller]")]
public class SmsController(ISmsMessageRepository smsRepository, ILogger<SmsController> logger) : ControllerBase
{
    private readonly ISmsMessageRepository _smsRepository = smsRepository;
    private readonly ILogger<SmsController> _logger = logger;

    /// <summary>Receives delivery report from SendSMSGate. Updates SmsMessage status. Supports GET (query) or POST (body) with smsid and status (send, deliver, not_deliver, expired).</summary>
    [AcceptVerbs("GET", "POST")]
    [Route("dlr")]
    public async Task<IActionResult> DeliveryReport(
        [FromQuery] string? smsid,
        [FromQuery] string? status,
        [FromBody] DlrRequest? body,
        CancellationToken cancellationToken)
    {
        var providerMessageId = body?.SmsId ?? smsid;
        var statusValue = body?.Status ?? status;

        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            _logger.LogWarning("SMS DLR received without smsid");
            return BadRequest("Missing smsid");
        }

        var sms = await _smsRepository.GetByProviderMessageIdAsync(providerMessageId.Trim(), cancellationToken);
        if (sms == null)
        {
            _logger.LogWarning("SMS DLR: unknown ProviderMessageId={ProviderMessageId}", providerMessageId);
            return Ok();
        }

        var newStatus = MapDlrStatus(statusValue);
        sms.Status = newStatus;
        sms.UpdatedAt = DateTime.UtcNow;
        await _smsRepository.UpdateAsync(sms, cancellationToken);

        _logger.LogInformation("SMS delivery: PhoneNumber={PhoneNumber}, ProviderMessageId={ProviderMessageId}, MessageType=DLR, Status={Status}",
            sms.PhoneNumber, providerMessageId, statusValue ?? newStatus.ToString());

        return Ok();
    }

    private static SmsMessageStatus MapDlrStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return SmsMessageStatus.Sent;
        return status.Trim().ToLowerInvariant() switch
        {
            "deliver" => SmsMessageStatus.Delivered,
            "not_deliver" => SmsMessageStatus.Failed,
            "expired" => SmsMessageStatus.Failed,
            "send" => SmsMessageStatus.Sent,
            _ => SmsMessageStatus.Sent
        };
    }
}

/// <summary>Request body for DLR webhook (when SendSMSGate sends POST with JSON).</summary>
public class DlrRequest
{
    public string? SmsId { get; set; }
    public string? Status { get; set; }
}
