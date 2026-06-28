using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bobeta.Application.Common;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Sms.Providers;

/// <summary>Twilio Programmable SMS (https://www.twilio.com/docs/sms/send-messages).</summary>
public class TwilioSmsProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<TwilioSettings> settings,
    ILogger<TwilioSmsProvider> logger) : ISmsProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TwilioSettings _settings = settings.Value;

    public string ProviderName => SmsProviderNames.Twilio;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.AccountSid) &&
        !string.IsNullOrWhiteSpace(_settings.AuthToken) &&
        (!string.IsNullOrWhiteSpace(_settings.From) || !string.IsNullOrWhiteSpace(_settings.MessagingServiceSid));

    public async Task<SmsSendResult> SendAsync(SmsSendContext context, CancellationToken cancellationToken = default)
    {
        var to = PhoneNumberHelper.ToE164(context.NormalizedPhone);
        var requestUri = $"{_settings.BaseUrl.TrimEnd('/')}/2010-04-01/Accounts/{Uri.EscapeDataString(_settings.AccountSid)}/Messages.json";

        var form = new List<KeyValuePair<string, string>>
        {
            new("To", to),
            new("Body", context.Message)
        };

        if (!string.IsNullOrWhiteSpace(_settings.MessagingServiceSid))
            form.Add(new("MessagingServiceSid", _settings.MessagingServiceSid));
        else
            form.Add(new("From", _settings.From));

        if (!string.IsNullOrWhiteSpace(_settings.StatusCallbackUrl))
            form.Add(new("StatusCallback", _settings.StatusCallbackUrl));

        try
        {
            var client = httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.TwilioHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new FormUrlEncodedContent(form)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}")));

            var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Twilio HTTP failure: PhoneNumber={PhoneNumber}, StatusCode={StatusCode}, Response={Response}",
                    context.NormalizedPhone, response.StatusCode, body);
                var ex = MapHttpError(response.StatusCode, body);
                return SmsSendResult.Failed(ex.Message, ex);
            }

            var parsed = JsonSerializer.Deserialize<TwilioMessageResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(parsed?.Sid))
            {
                logger.LogInformation("Twilio sent: PhoneNumber={PhoneNumber}, MessageSid={MessageSid}, MessageType={MessageType}, Status={Status}",
                    context.NormalizedPhone, parsed.Sid, context.MessageType, parsed.Status);
                return SmsSendResult.Succeeded(parsed.Sid);
            }

            var rejected = new SmsProviderException("Twilio did not return a message SID.", HttpStatusCode.UnprocessableEntity, body);
            return SmsSendResult.Failed(rejected.Message, rejected);
        }
        catch (SmsProviderException ex)
        {
            return SmsSendResult.Failed(ex.Message, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twilio error: PhoneNumber={PhoneNumber}, MessageType={MessageType}", context.NormalizedPhone, context.MessageType);
            var wrapped = new SmsProviderException("SMS gateway error. Please try again later.", ex);
            return SmsSendResult.Failed(wrapped.Message, wrapped);
        }
    }

    public SmsMessageStatus? MapDeliveryStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.Trim().ToLowerInvariant() switch
        {
            "delivered" => SmsMessageStatus.Delivered,
            "failed" or "undelivered" or "canceled" => SmsMessageStatus.Failed,
            "queued" or "sending" or "sent" or "accepted" or "scheduled" or "receiving" => SmsMessageStatus.Sent,
            _ => null
        };
    }

    private static SmsProviderException MapHttpError(HttpStatusCode statusCode, string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<TwilioErrorResponse>(body, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                var mappedStatus = error.Status is >= 400 and <= 599
                    ? (HttpStatusCode)error.Status
                    : statusCode;
                return new SmsProviderException($"Twilio error: {error.Message}", mappedStatus, body);
            }
        }
        catch (JsonException)
        {
            // Fall through to generic message.
        }

        var msg = statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request to Twilio.",
            HttpStatusCode.Unauthorized => "Twilio authentication failed.",
            HttpStatusCode.PaymentRequired => "Insufficient Twilio balance.",
            _ => "SMS gateway error. Please try again later."
        };
        return new SmsProviderException(msg + " " + body, statusCode, body);
    }

    private sealed class TwilioMessageResponse
    {
        [JsonPropertyName("sid")]
        public string? Sid { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private sealed class TwilioErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}
