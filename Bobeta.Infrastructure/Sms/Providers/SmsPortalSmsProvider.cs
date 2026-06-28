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

/// <summary>SMSPortal REST API v3 (https://cp.smsportal.com, https://docs.smsportal.com).</summary>
public class SmsPortalSmsProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<SmsPortalSettings> settings,
    ILogger<SmsPortalSmsProvider> logger) : ISmsProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly SmsPortalSettings _settings = settings.Value;

    public string ProviderName => SmsProviderNames.SmsPortal;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.ApiKey) &&
        !string.IsNullOrWhiteSpace(_settings.ApiSecret);

    public async Task<SmsSendResult> SendAsync(SmsSendContext context, CancellationToken cancellationToken = default)
    {
        var destination = PhoneNumberHelper.ToE164(context.NormalizedPhone);
        var payload = new SmsPortalSendRequest
        {
            SendOptions = new SmsPortalSendOptions
            {
                SenderId = _settings.SenderId,
                CampaignName = _settings.CampaignName,
                TestMode = _settings.TestMode
            },
            Messages =
            [
                new SmsPortalMessage
                {
                    Content = context.Message,
                    Destination = destination,
                    CustomerId = context.RecordId.ToString("D")
                }
            ]
        };

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var requestUri = $"{baseUrl}/v3/BulkMessages";
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        try
        {
            var client = httpClientFactory.CreateClient(InfrastructureServiceCollectionExtensions.SmsPortalHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials(_settings.ApiKey, _settings.ApiSecret));

            var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SMSPortal HTTP failure: PhoneNumber={PhoneNumber}, StatusCode={StatusCode}, Response={Response}",
                    context.NormalizedPhone, response.StatusCode, body);
                var ex = MapHttpError(response.StatusCode, body);
                return SmsSendResult.Failed(ex.Message, ex);
            }

            var parsed = JsonSerializer.Deserialize<SmsPortalSendResponse>(body, JsonOptions);
            if (parsed?.SendResponse?.Messages > 0)
            {
                var providerMessageId = parsed.SendResponse.EventId?.ToString() ?? context.RecordId.ToString("D");
                logger.LogInformation("SMSPortal sent: PhoneNumber={PhoneNumber}, EventId={EventId}, MessageType={MessageType}, TestMode={TestMode}",
                    context.NormalizedPhone, providerMessageId, context.MessageType, _settings.TestMode);
                return SmsSendResult.Succeeded(providerMessageId);
            }

            var faultMessage = BuildFaultMessage(parsed, body);
            logger.LogWarning("SMSPortal send rejected: PhoneNumber={PhoneNumber}, Response={Response}", context.NormalizedPhone, faultMessage);
            var rejected = new SmsProviderException(faultMessage, HttpStatusCode.UnprocessableEntity, body);
            return SmsSendResult.Failed(rejected.Message, rejected);
        }
        catch (SmsProviderException ex)
        {
            return SmsSendResult.Failed(ex.Message, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMSPortal error: PhoneNumber={PhoneNumber}, MessageType={MessageType}", context.NormalizedPhone, context.MessageType);
            var wrapped = new SmsProviderException("SMS gateway error. Please try again later.", ex);
            return SmsSendResult.Failed(wrapped.Message, wrapped);
        }
    }

    public SmsMessageStatus? MapDeliveryStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        return status.Trim().ToUpperInvariant() switch
        {
            "DELIVRD" => SmsMessageStatus.Delivered,
            "UNDELIV" or "EXPIRED" or "BLIST" or "NOROUTE" or "CANCELLED" => SmsMessageStatus.Failed,
            "SUBMITD" or "STAGED" => SmsMessageStatus.Sent,
            _ => null
        };
    }

    private static string EncodeCredentials(string apiKey, string apiSecret) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

    private static SmsProviderException MapHttpError(HttpStatusCode statusCode, string body)
    {
        var msg = statusCode switch
        {
            HttpStatusCode.BadRequest => "Invalid request to SMSPortal.",
            HttpStatusCode.Unauthorized => "SMSPortal authentication failed.",
            HttpStatusCode.PaymentRequired => "Insufficient SMSPortal balance.",
            _ => "SMS gateway error. Please try again later."
        };
        return new SmsProviderException(msg + " " + body, statusCode, body);
    }

    private static string BuildFaultMessage(SmsPortalSendResponse? parsed, string rawBody)
    {
        var errors = parsed?.Errors?
            .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
            .Select(e => e.ErrorMessage)
            .ToList();
        if (errors is { Count: > 0 })
            return "SMSPortal error: " + string.Join("; ", errors);

        var faults = parsed?.SendResponse?.ErrorReport?.Faults?
            .Where(f => !string.IsNullOrWhiteSpace(f.ErrorMessage))
            .Select(f => f.ErrorMessage)
            .ToList();
        if (faults is { Count: > 0 })
            return "SMSPortal error: " + string.Join("; ", faults);

        return string.IsNullOrWhiteSpace(rawBody)
            ? "SMSPortal rejected the message."
            : "SMSPortal error: " + rawBody.Trim();
    }

    private sealed class SmsPortalSendRequest
    {
        [JsonPropertyName("sendOptions")]
        public SmsPortalSendOptions? SendOptions { get; set; }

        [JsonPropertyName("messages")]
        public List<SmsPortalMessage> Messages { get; set; } = [];
    }

    private sealed class SmsPortalSendOptions
    {
        [JsonPropertyName("senderId")]
        public string? SenderId { get; set; }

        [JsonPropertyName("campaignName")]
        public string? CampaignName { get; set; }

        [JsonPropertyName("testMode")]
        public bool TestMode { get; set; }
    }

    private sealed class SmsPortalMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public string Destination { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;
    }

    private sealed class SmsPortalSendResponse
    {
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

        [JsonPropertyName("sendResponse")]
        public SmsPortalSendResponseBody? SendResponse { get; set; }

        [JsonPropertyName("errors")]
        public List<SmsPortalSendingError>? Errors { get; set; }
    }

    private sealed class SmsPortalSendResponseBody
    {
        [JsonPropertyName("eventId")]
        public long? EventId { get; set; }

        [JsonPropertyName("messages")]
        public int? Messages { get; set; }

        [JsonPropertyName("errorReport")]
        public SmsPortalFaultReport? ErrorReport { get; set; }
    }

    private sealed class SmsPortalSendingError
    {
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    private sealed class SmsPortalFaultReport
    {
        [JsonPropertyName("faults")]
        public List<SmsPortalMessageFault>? Faults { get; set; }
    }

    private sealed class SmsPortalMessageFault
    {
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
}
