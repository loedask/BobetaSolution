using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.Sms;
using Bobeta.Infrastructure.Sms.Providers;
using Microsoft.AspNetCore.Mvc;

namespace Bobeta.API.Controllers;

/// <summary>SMS delivery report (DLR) webhooks for SendSMSGate and SMSPortal.</summary>
[ApiController]
[Route("api/[controller]")]
public class SmsController(
    ISmsMessageRepository smsRepository,
    IEnumerable<ISmsProvider> smsProviders,
    ILogger<SmsController> logger) : ControllerBase
{
    private readonly ISmsMessageRepository _smsRepository = smsRepository;
    private readonly IReadOnlyDictionary<string, ISmsProvider> _providersByName =
        smsProviders.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<SmsController> _logger = logger;

    /// <summary>Receives delivery report from SendSMSGate. Supports GET (query) or POST (body) with smsid and status.</summary>
    [AcceptVerbs("GET", "POST")]
    [Route("dlr")]
    public async Task<IActionResult> SendSmsGateDeliveryReport(
        [FromQuery] string? smsid,
        [FromQuery] string? status,
        [FromBody] SendSmsGateDlrRequest? body,
        CancellationToken cancellationToken)
    {
        var providerMessageId = body?.SmsId ?? smsid;
        var statusValue = body?.Status ?? status;

        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            _logger.LogWarning("SendSMSGate DLR received without smsid");
            return BadRequest("Missing smsid");
        }

        var provider = GetProvider(SmsProviderNames.SendSmsGate);
        var mapped = provider?.MapDeliveryStatus(statusValue) ?? MapSendSmsGateDlrStatus(statusValue);
        return await ApplyDeliveryUpdateAsync(
            providerMessageId.Trim(),
            customerId: null,
            mapped,
            statusValue,
            cancellationToken);
    }

    /// <summary>Receives delivery report from SMSPortal (configure webhook in cp.smsportal.com).</summary>
    [HttpPost("dlr/smsportal")]
    public async Task<IActionResult> SmsPortalDeliveryReport(
        [FromBody] SmsPortalDlrRequest? body,
        CancellationToken cancellationToken)
    {
        if (body == null)
            return BadRequest("Missing body");

        var provider = GetProvider(SmsProviderNames.SmsPortal);
        var mapped = provider?.MapDeliveryStatus(body.Status) ?? MapSmsPortalDlrStatus(body.Status);

        if (string.IsNullOrWhiteSpace(body.CustomerId) && body.Id == null)
        {
            _logger.LogWarning("SMSPortal DLR received without customerId or id");
            return BadRequest("Missing customerId or id");
        }

        return await ApplyDeliveryUpdateAsync(
            body.Id?.ToString(),
            body.CustomerId,
            mapped,
            body.Status,
            cancellationToken);
    }

    private ISmsProvider? GetProvider(string name) =>
        _providersByName.TryGetValue(name, out var provider) ? provider : null;

    private async Task<IActionResult> ApplyDeliveryUpdateAsync(
        string? providerMessageId,
        string? customerId,
        SmsMessageStatus newStatus,
        string? rawStatus,
        CancellationToken cancellationToken)
    {
        SmsMessage? sms = null;

        if (!string.IsNullOrWhiteSpace(customerId) && Guid.TryParse(customerId, out var recordId))
            sms = await _smsRepository.GetByIdAsync(recordId, cancellationToken);

        if (sms == null && !string.IsNullOrWhiteSpace(providerMessageId))
            sms = await _smsRepository.GetByProviderMessageIdAsync(providerMessageId.Trim(), cancellationToken);

        if (sms == null)
        {
            _logger.LogWarning("SMS DLR: unknown message. ProviderMessageId={ProviderMessageId}, CustomerId={CustomerId}",
                providerMessageId, customerId);
            return Ok();
        }

        sms.Status = newStatus;
        sms.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(providerMessageId) && string.IsNullOrWhiteSpace(sms.ProviderMessageId))
            sms.ProviderMessageId = providerMessageId.Trim();

        await _smsRepository.UpdateAsync(sms, cancellationToken);

        _logger.LogInformation("SMS delivery: PhoneNumber={PhoneNumber}, Provider={Provider}, ProviderMessageId={ProviderMessageId}, Status={Status}",
            sms.PhoneNumber, sms.Provider, providerMessageId ?? sms.ProviderMessageId, rawStatus ?? newStatus.ToString());

        return Ok();
    }

    private static SmsMessageStatus MapSendSmsGateDlrStatus(string? status)
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

    private static SmsMessageStatus MapSmsPortalDlrStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return SmsMessageStatus.Sent;
        return status.Trim().ToUpperInvariant() switch
        {
            "DELIVRD" => SmsMessageStatus.Delivered,
            "UNDELIV" or "EXPIRED" or "BLIST" or "NOROUTE" or "CANCELLED" => SmsMessageStatus.Failed,
            "SUBMITD" or "STAGED" => SmsMessageStatus.Sent,
            _ => SmsMessageStatus.Sent
        };
    }
}

/// <summary>Request body for SendSMSGate DLR webhook (when POST with JSON).</summary>
public class SendSmsGateDlrRequest
{
    public string? SmsId { get; set; }
    public string? Status { get; set; }
}

/// <summary>Request body for SMSPortal DLR webhook (default template fields).</summary>
public class SmsPortalDlrRequest
{
    public string? CustomerId { get; set; }
    public long? Id { get; set; }
    public long? EventId { get; set; }
    public string? Status { get; set; }
    public string? PhoneNumber { get; set; }
}
