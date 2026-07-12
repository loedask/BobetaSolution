using System.Net;
using Bobeta.Application.Common;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Sms.Providers;

/// <summary>SendSMSGate HTTP API (https://sendsmsgate.com).</summary>
public class SendSmsGateSmsProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<SmsGatewaySettings> settings,
    ILogger<SendSmsGateSmsProvider> logger) : ISmsProvider
{
    private readonly SmsGatewaySettings _settings = settings.Value;

    public string ProviderName => SmsProviderNames.SendSmsGate;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Username) &&
        !string.IsNullOrWhiteSpace(_settings.Password);

    public async Task<SmsSendResult> SendAsync(SmsSendContext context, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = context.NormalizedPhone;
        var message = context.Message;
        var messageType = context.MessageType;

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

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 0)
                    logger.LogWarning("SendSMSGate retry: Attempt={Attempt}, PhoneNumber={PhoneNumber}, MessageType={MessageType}",
                        attempt + 1, normalizedPhone, messageType);

                var client = httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.SendSmsGateHttpClientName);
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

                    logger.LogWarning("SendSMSGate failure: PhoneNumber={PhoneNumber}, StatusCode={StatusCode}, Response={Response}",
                        normalizedPhone, response.StatusCode, body);
                    var ex = MapError(response.StatusCode, body);
                    return SmsSendResult.Failed(ex.Message, ex);
                }

                var providerId = ParseProviderMessageId(body);
                if (string.IsNullOrEmpty(providerId))
                {
                    var err = MapErrorFromBody(body);
                    logger.LogWarning("SendSMSGate failure: PhoneNumber={PhoneNumber}, Response={Response}", normalizedPhone, body);
                    return SmsSendResult.Failed(err.Message, err);
                }

                logger.LogInformation("SendSMSGate sent: PhoneNumber={PhoneNumber}, ProviderMessageId={ProviderMessageId}, MessageType={MessageType}",
                    normalizedPhone, providerId, messageType);
                return SmsSendResult.Succeeded(providerId);
            }
            catch (SmsProviderException ex)
            {
                return SmsSendResult.Failed(ex.Message, ex);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts - 1)
            {
                logger.LogWarning(ex, "SendSMSGate network retry: Attempt={Attempt}, PhoneNumber={PhoneNumber}", attempt + 1, normalizedPhone);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
            }
            catch (TaskCanceledException) when (attempt < maxAttempts - 1)
            {
                logger.LogWarning("SendSMSGate timeout retry: Attempt={Attempt}, PhoneNumber={PhoneNumber}", attempt + 1, normalizedPhone);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SendSMSGate error: PhoneNumber={PhoneNumber}, MessageType={MessageType}", normalizedPhone, messageType);
                var wrapped = new SmsProviderException("SMS gateway error. Please try again later.", ex);
                return SmsSendResult.Failed(wrapped.Message, wrapped);
            }
        }

        var fallback = new SmsProviderException("SMS gateway error. Please try again later.");
        return SmsSendResult.Failed(fallback.Message, fallback);
    }

    public SmsMessageStatus? MapDeliveryStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.Trim().ToLowerInvariant() switch
        {
            "deliver" => SmsMessageStatus.Delivered,
            "not_deliver" => SmsMessageStatus.Failed,
            "expired" => SmsMessageStatus.Failed,
            "send" => SmsMessageStatus.Sent,
            _ => null
        };
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

    private static SmsProviderException MapError(HttpStatusCode statusCode, string body)
    {
        var msg = statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request to SMS gateway.",
            HttpStatusCode.Unauthorized => "SMS gateway authentication failed.",
            HttpStatusCode.PaymentRequired => "Insufficient balance for SMS.",
            _ => "SMS gateway error. Please try again later."
        };
        return new SmsProviderException(msg + " " + body, statusCode, body);
    }

    private static SmsProviderException MapErrorFromBody(string body)
    {
        var b = (body ?? string.Empty).Trim();
        if (b.Contains("phone number", StringComparison.OrdinalIgnoreCase) || b.Contains("Enter a phone number", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("Invalid phone number.", HttpStatusCode.BadRequest, body);
        if (b.Contains("run out of SMS", StringComparison.OrdinalIgnoreCase) || b.Contains("You have run out of SMS", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("Insufficient SMS balance. Contact support.", HttpStatusCode.PaymentRequired, body);
        if (b.Contains("blocked", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("SMS account is blocked.", HttpStatusCode.Forbidden, body);
        if (b.Contains("stop list", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("Phone number is in the stop list.", HttpStatusCode.BadRequest, body);
        if (b.Contains("direction is closed", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("This direction is closed for SMS.", HttpStatusCode.BadRequest, body);
        if (b.Contains("rejected by the moderator", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("SMS content was rejected.", HttpStatusCode.BadRequest, body);
        if (b.Contains("No sender", StringComparison.OrdinalIgnoreCase) || b.Contains("sender did not pass", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("Invalid sender ID configuration.", HttpStatusCode.BadRequest, body);
        if (b.Contains("No SMS content", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("No SMS content.", HttpStatusCode.BadRequest, body);
        if (b.Contains("must be less than 15 characters", StringComparison.OrdinalIgnoreCase))
            return new SmsProviderException("Phone number must be less than 15 characters.", HttpStatusCode.BadRequest, body);
        return new SmsProviderException("SMS gateway error: " + b, HttpStatusCode.UnprocessableEntity, body);
    }
}
