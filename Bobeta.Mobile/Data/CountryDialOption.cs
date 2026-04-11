namespace Bobeta.Mobile.Data;

public record CountryDialOption(string CountryCode, string Name, string Dial, int Digits)
{
    public static CountryDialOption[] All { get; } =
    [
        new("CG", "Congo (Brazzaville)", "+242", 9),
        new("CD", "Congo (Kinshasa)", "+243", 9),
        new("CM", "Cameroon", "+237", 9),
        new("GA", "Gabon", "+241", 7),
        new("CF", "Central African Republic", "+236", 8),
        new("TD", "Chad", "+235", 8),
        new("GQ", "Equatorial Guinea", "+240", 9),
        new("CI", "Côte d'Ivoire", "+225", 10),
        new("SN", "Senegal", "+221", 9),
        new("ML", "Mali", "+223", 8),
        new("BF", "Burkina Faso", "+226", 8),
        new("GN", "Guinea", "+224", 9),
        new("BJ", "Benin", "+229", 8),
        new("TG", "Togo", "+228", 8),
        new("NE", "Niger", "+227", 8),
        new("NG", "Nigeria", "+234", 10),
        new("GH", "Ghana", "+233", 9),
        new("KE", "Kenya", "+254", 9),
        new("TZ", "Tanzania", "+255", 9),
        new("UG", "Uganda", "+256", 9),
        new("RW", "Rwanda", "+250", 9),
        new("BI", "Burundi", "+257", 8),
        new("ZA", "South Africa", "+27", 9),
        new("MG", "Madagascar", "+261", 9),
        new("AO", "Angola", "+244", 9),
        new("MZ", "Mozambique", "+258", 9),
        new("FR", "France", "+33", 9),
        new("BE", "Belgium", "+32", 9),
        new("US", "United States", "+1", 10),
        new("GB", "United Kingdom", "+44", 10),
    ];

    public const string DefaultCountryCode = "CG";

    public static CountryDialOption Default => All.First(c => c.CountryCode == DefaultCountryCode);

    public static CountryDialOption? FindByDial(string dial) => All.FirstOrDefault(c => c.Dial == dial);

    public static string FlagUrl(string code) => $"https://flagcdn.com/w40/{code.ToLowerInvariant()}.png";
}
