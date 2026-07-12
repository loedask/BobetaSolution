namespace Bobeta.Application.Common;

/// <summary>Supported operating countries (ISO alpha-2). Shared by portal, auth, and license partner assignments.</summary>
public static class CountryCatalog
{
  public sealed record Country(string Code, string Name, string DialCode);

  private static readonly Country[] Countries =
  [
    new("CG", "Congo (Brazzaville)", "242"),
    new("CD", "Congo (Kinshasa)", "243"),
    new("CM", "Cameroon", "237"),
    new("GA", "Gabon", "241"),
    new("CF", "Central African Republic", "236"),
    new("TD", "Chad", "235"),
    new("GQ", "Equatorial Guinea", "240"),
    new("CI", "Côte d'Ivoire", "225"),
    new("SN", "Senegal", "221"),
    new("ML", "Mali", "223"),
    new("BF", "Burkina Faso", "226"),
    new("GN", "Guinea", "224"),
    new("BJ", "Benin", "229"),
    new("TG", "Togo", "228"),
    new("NE", "Niger", "227"),
    new("NG", "Nigeria", "234"),
    new("GH", "Ghana", "233"),
    new("KE", "Kenya", "254"),
    new("TZ", "Tanzania", "255"),
    new("UG", "Uganda", "256"),
    new("RW", "Rwanda", "250"),
    new("BI", "Burundi", "257"),
    new("ZA", "South Africa", "27"),
    new("MG", "Madagascar", "261"),
    new("AO", "Angola", "244"),
    new("MZ", "Mozambique", "258"),
  ];

  public static IReadOnlyList<Country> All => Countries;

  public static Country? GetByCode(string? code) =>
    string.IsNullOrWhiteSpace(code)
      ? null
      : Countries.FirstOrDefault(c => c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase));

  public static Country? GetByDialCode(string dialDigits)
  {
    if (string.IsNullOrWhiteSpace(dialDigits))
      return null;

    return Countries
      .OrderByDescending(c => c.DialCode.Length)
      .FirstOrDefault(c => dialDigits.StartsWith(c.DialCode, StringComparison.Ordinal));
  }

  /// <summary>Resolves ISO country from a normalized phone (digits only, e.g. 24267123456).</summary>
  public static string? ResolveCountryCodeFromPhone(string? normalizedPhone)
  {
    if (string.IsNullOrWhiteSpace(normalizedPhone))
      return null;

    var digits = normalizedPhone.Trim().TrimStart('+');
    return GetByDialCode(digits)?.Code;
  }
}
