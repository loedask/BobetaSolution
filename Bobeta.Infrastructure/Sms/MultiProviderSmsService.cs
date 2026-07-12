using Bobeta.Application.Common;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Entities;
using Bobeta.Domain.Enums;
using Bobeta.Infrastructure.Sms.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bobeta.Infrastructure.Sms;

/// <summary>Routes outbound SMS through a configurable default provider with optional fallbacks.</summary>
public class MultiProviderSmsService : ISmsService
{
    private readonly IReadOnlyDictionary<string, ISmsProvider> _providersByName;
    private readonly SmsOptions _options;
    private readonly ISmsMessageRepository _smsRepository;
    private readonly ILogger<MultiProviderSmsService> _logger;

    public MultiProviderSmsService(
        IEnumerable<ISmsProvider> providers,
        IOptions<SmsOptions> options,
        ISmsMessageRepository smsRepository,
        ILogger<MultiProviderSmsService> logger)
    {
        _providersByName = providers.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
        _smsRepository = smsRepository;
        _logger = logger;
    }

    public Task<string?> SendOtpAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        SendAsync(phoneNumber, message, "Otp", cancellationToken);

    public Task<string?> SendNotificationAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
        SendAsync(phoneNumber, message, "Notification", cancellationToken);

    public async Task<string?> CheckDeliveryStatusAsync(string providerMessageId, CancellationToken cancellationToken = default)
    {
        var sms = await _smsRepository.GetByProviderMessageIdAsync(providerMessageId, cancellationToken);
        if (sms == null) return null;

        return sms.Status switch
        {
            SmsMessageStatus.Delivered => "deliver",
            SmsMessageStatus.Failed => "not_deliver",
            SmsMessageStatus.Sent => "send",
            _ => null
        };
    }

    private async Task<string?> SendAsync(string phoneNumber, string message, string messageType, CancellationToken cancellationToken)
    {
        var normalized = PhoneNumberHelper.Normalize(phoneNumber);
        var smsRecord = new SmsMessage
        {
            Id = Guid.NewGuid(),
            PhoneNumber = normalized,
            Message = message,
            Provider = null,
            ProviderMessageId = null,
            Status = SmsMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _smsRepository.AddAsync(smsRecord, cancellationToken);

        var context = new SmsSendContext
        {
            RecordId = smsRecord.Id,
            NormalizedPhone = normalized,
            Message = message,
            MessageType = messageType
        };

        var orderedProviders = ResolveProviderOrder();
        if (orderedProviders.Count == 0)
            throw new SmsProviderException("No SMS providers are configured. Set Sms:DefaultProvider and provider credentials.");

        SmsProviderException? lastException = null;

        foreach (var provider in orderedProviders)
        {
            if (!provider.IsConfigured)
            {
                _logger.LogWarning("Skipping SMS provider {Provider}: not configured", provider.ProviderName);
                continue;
            }

            _logger.LogInformation("Sending SMS via {Provider}: PhoneNumber={PhoneNumber}, MessageType={MessageType}",
                provider.ProviderName, normalized, messageType);

            var result = await provider.SendAsync(context, cancellationToken);
            if (result.Success && !string.IsNullOrWhiteSpace(result.ProviderMessageId))
            {
                smsRecord.Provider = provider.ProviderName;
                smsRecord.ProviderMessageId = result.ProviderMessageId;
                smsRecord.Status = SmsMessageStatus.Sent;
                smsRecord.UpdatedAt = DateTime.UtcNow;
                await _smsRepository.UpdateAsync(smsRecord, cancellationToken);
                return result.ProviderMessageId;
            }

            lastException = result.Exception ?? new SmsProviderException(result.ErrorMessage ?? "SMS send failed.");
            _logger.LogWarning("SMS provider {Provider} failed: {Error}", provider.ProviderName, lastException.Message);

            if (!lastException.AllowProviderFallback || !_options.EnableFallback)
                break;
        }

        smsRecord.Status = SmsMessageStatus.Failed;
        smsRecord.UpdatedAt = DateTime.UtcNow;
        await _smsRepository.UpdateAsync(smsRecord, cancellationToken);

        throw lastException ?? new SmsProviderException("SMS gateway error. Please try again later.");
    }

    private IReadOnlyList<ISmsProvider> ResolveProviderOrder()
    {
        var ordered = new List<ISmsProvider>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                return;
            if (_providersByName.TryGetValue(name, out var provider))
                ordered.Add(provider);
            else
                _logger.LogWarning("Unknown SMS provider in configuration: {Provider}", name);
        }

        TryAdd(_options.DefaultProvider);
        if (_options.EnableFallback)
        {
            foreach (var fallback in _options.FallbackProviders)
                TryAdd(fallback);
        }

        return ordered;
    }
}
