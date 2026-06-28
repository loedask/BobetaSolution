namespace Bobeta.Infrastructure.Sms;

/// <summary>SMSPortal REST API configuration (https://cp.smsportal.com).</summary>
public class SmsPortalSettings
{
    public const string SectionName = "SmsPortalSettings";

    /// <summary>REST API base URL (default https://rest.smsportal.com).</summary>
    public string BaseUrl { get; set; } = "https://rest.smsportal.com";

    /// <summary>API key (Client ID) from SMSPortal control panel.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>API secret from SMSPortal control panel.</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Sender ID (max 11 alphanumeric). Best-effort per operator.</summary>
    public string SenderId { get; set; } = "Bobeta";

    /// <summary>Campaign name for reporting in SMSPortal.</summary>
    public string CampaignName { get; set; } = "Bobeta";

    /// <summary>When true, SMSPortal validates the request but does not send (API test mode).</summary>
    public bool TestMode { get; set; }
}
