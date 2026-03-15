namespace Bobeta.Application.Interfaces;

/// <summary>Provides SMS message templates by key and optional language.</summary>
public interface ISmsTemplateProvider
{
    /// <summary>Gets the OTP SMS body template. Placeholder {0} is the OTP code.</summary>
    /// <param name="languageCode">Language code (e.g. "fr", "en"); used to choose OTP_FR vs OTP_LN (Lingala) or default.</param>
    string GetOtpMessageTemplate(string? languageCode = null);
}
