using System.Net;
using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Sms;

/// <summary>SendSMSGate HTTP API implementation of ISmsService. Sends SMS and records for DLR tracking.</summary>
public class SmsGatewayService : ISmsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SmsGatewaySettings _settings;
    private readonly ISmsMessageRepository _smsRepository;
    private readonly ILogger<SmsGatewayService> _logger;

    public SmsGatewayService(
        IHttpClientFactory httpClientFactory,
        IOptions<SmsGatewaySettings> settings,
        ISmsMessageRepository smsRepository,
        ILogger<SmsGatewayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _smsRepository = smsRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> SendOtpAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        return await SendAsync(normalized, message, "Otp", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> SendNotificationAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        return await SendAsync(normalized, message, "Notification", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> CheckDeliveryStatusAsync(string providerMessageId, CancellationToken cancellationToken = default)
    {
        var sms = await _smsRepository.GetByProviderMessageIdAsync(providerMessageId, cancellationToken);
        return sms?.Status switch
        {
            SmsMessageStatus.Delivered => "deliver",
            SmsMessageStatus.Failed => "not_deliver",
            SmsMessageStatus.Sent => "send",
            _ => null
        };
    }

    private async Task<string?> SendAsync(string normalizedPhone, string message, string messageType, CancellationToken cancellationToken)
    {
        var smsRecord = new SmsMessage
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalizedPhone,
            Message = message,
            ProviderMessageId = null,
            Status = SmsMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _smsRepository.AddAsync(smsRecord, cancellationToken);

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var sendUrl = $"{baseUrl}/sendsms.php";
        var query = new List<KeyValuePair<string, string>>
        {
            new("user", _settings.Username),
            new("pwd", _settings.Password),
            new("sadr", _settings.SenderId),
            new("dadr", normalizedPhone),
            new("text", message)
        };
        var requestUri = new Uri(sendUrl + "?" + string.Join("&", query.Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}")));

        const int maxAttempts = 3;
        const int retryDelaySeconds = 3;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                    _logger.LogWarning("SMS retry attempt: Attempt={Attempt}, PhoneNumber={PhoneNumber}, MessageType={MessageType}", attempt + 1, normalizedPhone, messageType);

                var client = _httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.SmsGatewayHttpClientName);
                var response = await client.GetAsync(requestUri, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var isRetryable = response.StatusCode == HttpStatusCode.RequestTimeout ||
                                      response.StatusCode == HttpStatusCode.GatewayTimeout ||
                                      (int)response.StatusCode >= 500;
                    if (isRetryable && attempt < maxAttempts - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                        continue;
                    }
                    _logger.LogWarning("SMS failure: PhoneNumber={PhoneNumber}, MessageType={MessageType}, StatusCode={StatusCode}, Response={Response}",
                        normalizedPhone, messageType, response.StatusCode, body);
                    UpdateToFailed(smsRecord, "HTTP " + (int)response.StatusCode);
                    await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                    throw MapError(response.StatusCode, body);
                }

                var providerId = ParseProviderMessageId(body);
                if (string.IsNullOrEmpty(providerId))
                {
                    var err = MapErrorFromBody(body);
                    _logger.LogWarning("SMS failure: PhoneNumber={PhoneNumber}, MessageType={MessageType}, ProviderResponse={Response}",
                        normalizedPhone, messageType, body);
                    UpdateToFailed(smsRecord, body);
                    await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                    throw err;
                }

                smsRecord.ProviderMessageId = providerId;
                smsRecord.Status = SmsMessageStatus.Sent;
                smsRecord.UpdatedAt = DateTime.UtcNow;
                await _smsRepository.UpdateAsync(smsRecord, cancellationToken);

                _logger.LogInformation("SMS sent: PhoneNumber={PhoneNumber}, ProviderMessageId={ProviderMessageId}, MessageType={MessageType}",
                    normalizedPhone, providerId, messageType);
                return providerId;
            }
            catch (SmsGatewayException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts - 1)
                {
                    _logger.LogWarning(ex, "SMS retry attempt: network error, Attempt={Attempt}, PhoneNumber={PhoneNumber}, MessageType={MessageType}", attempt + 1, normalizedPhone, messageType);
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                    continue;
                }
                _logger.LogError(ex, "SMS gateway error: PhoneNumber={PhoneNumber}, MessageType={MessageType}", normalizedPhone, messageType);
                UpdateToFailed(smsRecord, ex.Message);
                await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                throw new SmsGatewayException("SMS gateway error. Please try again later.", ex);
            }
            catch (TaskCanceledException ex)
            {
                if (attempt < maxAttempts - 1)
                {
                    _logger.LogWarning("SMS retry attempt: timeout/cancelled, Attempt={Attempt}, PhoneNumber={PhoneNumber}, MessageType={MessageType}", attempt + 1, normalizedPhone, messageType);
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
                    continue;
                }
                _logger.LogError(ex, "SMS gateway error (timeout): PhoneNumber={PhoneNumber}, MessageType={MessageType}", normalizedPhone, messageType);
                UpdateToFailed(smsRecord, ex.Message);
                await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                throw new SmsGatewayException("SMS gateway error. Please try again later.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS gateway error: PhoneNumber={PhoneNumber}, MessageType={MessageType}", normalizedPhone, messageType);
                UpdateToFailed(smsRecord, ex.Message);
                await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                throw new SmsGatewayException("SMS gateway error. Please try again later.", ex);
            }
        }

        UpdateToFailed(smsRecord, "Max retries exceeded");
        await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
        throw new SmsGatewayException("SMS gateway error. Please try again later.");
    }

    private static string? ParseProviderMessageId(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        var trimmed = body.Trim();
        if (trimmed.Length == 0) return null;
        var parts = trimmed.Split(',', StringSplitOptions.TrimEntries);
        var first = parts[0];
        return long.TryParse(first, out _) ? first : null;
    }

    private static void UpdateToFailed(SmsMessage sms, string reason)
    {
        sms.Status = SmsMessageStatus.Failed;
        sms.UpdatedAt = DateTime.UtcNow;
    }

    private static SmsGatewayException MapError(HttpStatusCode statusCode, string body)
    {
        var msg = statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request to SMS gateway.",
            HttpStatusCode.Unauthorized => "SMS gateway authentication failed.",
            HttpStatusCode.PaymentRequired => "Insufficient balance for SMS.",
            _ => "SMS gateway error. Please try again later."
        };
        return new SmsGatewayException(msg + " " + body, statusCode, body);
    }

    private static SmsGatewayException MapErrorFromBody(string body)
    {
        var b = (body ?? string.Empty).Trim();
        if (b.Contains("phone number", StringComparison.OrdinalIgnoreCase) || b.Contains("Enter a phone number", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("Invalid phone number.", HttpStatusCode.BadRequest, body);
        if (b.Contains("run out of SMS", StringComparison.OrdinalIgnoreCase) || b.Contains("You have run out of SMS", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("Insufficient SMS balance. Contact support.", HttpStatusCode.PaymentRequired, body);
        if (b.Contains("blocked", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("SMS account is blocked.", HttpStatusCode.Forbidden, body);
        if (b.Contains("stop list", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("Phone number is in the stop list.", HttpStatusCode.BadRequest, body);
        if (b.Contains("direction is closed", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("This direction is closed for SMS.", HttpStatusCode.BadRequest, body);
        if (b.Contains("rejected by the moderator", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("SMS content was rejected.", HttpStatusCode.BadRequest, body);
        if (b.Contains("No sender", StringComparison.OrdinalIgnoreCase) || b.Contains("sender did not pass", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("Invalid sender ID configuration.", HttpStatusCode.BadRequest, body);
        if (b.Contains("No SMS content", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("No SMS content.", HttpStatusCode.BadRequest, body);
        if (b.Contains("must be less than 15 characters", StringComparison.OrdinalIgnoreCase))
            return new SmsGatewayException("Phone number must be less than 15 characters.", HttpStatusCode.BadRequest, body);
        return new SmsGatewayException("SMS gateway error: " + b, HttpStatusCode.UnprocessableEntity, body);
    }
}

/// <summary>Thrown when SendSMSGate API returns an error or send fails.</summary>
public class SmsGatewayException : InvalidOperationException
{
    public HttpStatusCode? StatusCode { get; }
    public string? ResponseBody { get; }

    public SmsGatewayException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public SmsGatewayException(string message, Exception inner)
        : base(message, inner)
    {
        StatusCode = null;
        ResponseBody = null;
    }
}
