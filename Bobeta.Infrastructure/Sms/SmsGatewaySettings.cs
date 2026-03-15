namespace Bobeta.Infrastructure.Sms;

/// <summary>SendSMSGate HTTP API configuration.</summary>
public class SmsGatewaySettings
{
    public const string SectionName = "SmsGatewaySettings";

    /// <summary>Base URL for the API (e.g. https://cloud.sendsmsgate.com).</summary>
    public string BaseUrl { get; set; } = "https://cloud.sendsmsgate.com";

    /// <summary>SendSMSGate login (user).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SendSMSGate password (pwd).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Sender ID (sadr) - alphanumeric or digital, max 11 chars alphanumeric / 15 digital.</summary>
    public string SenderId { get; set; } = "Bobeta";

    /// <summary>Public URL where SendSMSGate will send delivery reports (DLR) - POST/GET callback.</summary>
    public string DeliveryReportUrl { get; set; } = string.Empty;
}
