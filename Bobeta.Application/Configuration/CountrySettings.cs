namespace Bobeta.Application.Configuration;

/// <summary>Default country/region settings for the Bobeta platform (e.g. Republic of the Congo launch).</summary>
public class CountrySettings
{
    public const string SectionName = "CountrySettings";

    /// <summary>Default country calling code (e.g. 242 for Republic of the Congo).</summary>
    public string DefaultCountryCode { get; set; } = "242";

    /// <summary>Default currency code (e.g. XAF for Central African CFA franc).</summary>
    public string DefaultCurrency { get; set; } = "XAF";

    /// <summary>Default language code (e.g. fr for French).</summary>
    public string DefaultLanguage { get; set; } = "fr";
}
