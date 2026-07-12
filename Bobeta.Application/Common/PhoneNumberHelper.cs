namespace Bobeta.Application.Common;

/// <summary>Utility for normalizing phone numbers. Default launch country: Republic of the Congo (242).</summary>
public static class PhoneNumberHelper
{
    /// <summary>Default country code for Republic of the Congo (Congo-Brazzaville).</summary>
    public const string DefaultCountryCode = "242";

    /// <summary>
    /// Normalizes a phone number for SMS and MoMo: 0 → 242, +242 → 242, 242 → keep.
    /// Example: 067123456 → 24267123456; +24267123456 → 24267123456.
    /// </summary>
    /// <param name="phoneNumber">Raw phone number (e.g. 067123456, +24267123456, 24267123456).</param>
    /// <param name="defaultCountryCode">Country code to use when number starts with 0. Default 242.</param>
    /// <returns>Digits only with country code (e.g. 24267123456).</returns>
    public static string Normalize(string? phoneNumber, string? defaultCountryCode = null)
    {
        var countryCode = defaultCountryCode ?? DefaultCountryCode;
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var p = phoneNumber.Trim();
        if (p.StartsWith("0"))
            return countryCode + p.TrimStart('0');
        if (p.StartsWith("+"))
            p = p.TrimStart('+');
        if (p.StartsWith(countryCode))
            return p;
        // If it doesn't start with country code, prepend it (e.g. 67123456 → 24267123456)
        if (p.Length > 0 && !p.StartsWith(countryCode))
            return countryCode + p;
        return p;
    }

    /// <summary>Formats a normalized number (e.g. 24267123456) as E.164 (+24267123456).</summary>
    public static string ToE164(string normalizedPhone)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            return string.Empty;
        var digits = normalizedPhone.Trim().TrimStart('+');
        return "+" + digits;
    }
}
