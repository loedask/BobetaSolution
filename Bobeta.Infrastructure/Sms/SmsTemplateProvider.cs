using Bobeta.Application.Interfaces;

namespace Bobeta.Infrastructure.Sms;

/// <summary>Provides SMS templates OTP_FR (French) and OTP_LN (Lingala). Placeholder {0} = OTP code.</summary>
public class SmsTemplateProvider : ISmsTemplateProvider
{
    /// <summary>French: "Votre code de vérification Bobeta est {0}. Ne partagez pas ce code."</summary>
    private const string OtpFr = "Votre code de vérification Bobeta est {0}. Ne partagez pas ce code.";

    /// <summary>Lingala: "Kode ya Bobeta ezali {0}. Kopesa kode oyo te."</summary>
    private const string OtpLn = "Kode ya Bobeta ezali {0}. Kopesa kode oyo te.";

    /// <summary>Default (English) when language not matched.</summary>
    private const string OtpDefault = "Your Bobeta verification code is {0}. Do not share this code.";

    /// <inheritdoc />
    public string GetOtpMessageTemplate(string? languageCode = null)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return OtpDefault;
        return languageCode.Trim().ToLowerInvariant() switch
        {
            "fr" => OtpFr,
            "ln" => OtpLn,
            "lingala" => OtpLn,
            _ => OtpDefault
        };
    }
}
