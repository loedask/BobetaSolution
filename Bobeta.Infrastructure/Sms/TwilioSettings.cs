namespace Bobeta.Infrastructure.Sms;

/// <summary>Twilio Programmable SMS configuration (https://www.twilio.com/docs/sms).</summary>
public class TwilioSettings
{
    public const string SectionName = "TwilioSettings";

    /// <summary>Twilio REST API base URL (default https://api.twilio.com).</summary>
    public string BaseUrl { get; set; } = "https://api.twilio.com";

    /// <summary>Twilio Account SID.</summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Twilio Auth Token.</summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>Sender phone number in E.164 (e.g. +15017122661). Use this or <see cref="MessagingServiceSid"/>.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Optional Messaging Service SID (MG...). When set, used instead of <see cref="From"/>.</summary>
    public string MessagingServiceSid { get; set; } = string.Empty;

    /// <summary>Public URL for Twilio status callbacks (e.g. https://your-api-host/api/sms/dlr/twilio).</summary>
    public string StatusCallbackUrl { get; set; } = string.Empty;
}
