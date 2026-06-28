namespace Bobeta.Infrastructure.Sms;

/// <summary>Top-level SMS routing: default provider and optional fallbacks when a provider is down.</summary>
public class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Primary provider name. See <see cref="SmsProviderNames"/>.</summary>
    public string DefaultProvider { get; set; } = SmsProviderNames.SmsPortal;

    /// <summary>When true, tries <see cref="FallbackProviders"/> after the default provider fails.</summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>Ordered list of fallback provider names after the default fails.</summary>
    public List<string> FallbackProviders { get; set; } = [SmsProviderNames.SendSmsGate];
}
