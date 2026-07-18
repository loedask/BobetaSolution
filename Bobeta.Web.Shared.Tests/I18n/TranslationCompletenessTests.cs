using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Tests.Infrastructure;
using Xunit;

namespace Bobeta.Web.Shared.Tests.I18n;

public sealed class TranslationCompletenessTests
{
    // Keys that legitimately stay the same across locales (brand / game names / shared tokens).
    private static readonly HashSet<string> AllowedSameAsEnglish = new(StringComparer.Ordinal)
    {
        "app_name",
        "phone_placeholder",
        "landing_game_makopa_title",
        "landing_game_kopo_title",
        "landing_game_ngola_title",
        "landing_nav_faq",
        "live",
        "min_withdrawal",
        "max_withdrawal",
        "payment_method_momo",
        "momo_number",
        "amount_fcfa",
    };

    public static IEnumerable<object[]> NonEnglishLocales() =>
        I18nService.SupportedLocales
            .Where(x => x.Code != "en")
            .Select(x => new object[] { x.Code });

    [Theory]
    [MemberData(nameof(NonEnglishLocales))]
    public void Locale_OverridesMostEnglishStrings(string locale)
    {
        var en = CreateI18n("en");
        var localized = CreateI18n(locale);

        var stillEnglish = I18nService.EnglishKeys
            .Where(key => !AllowedSameAsEnglish.Contains(key))
            .Where(key => string.Equals(en.T(key), localized.T(key), StringComparison.Ordinal))
            .OrderBy(k => k)
            .ToList();

        Assert.True(
            stillEnglish.Count == 0,
            $"{locale} still uses English for: {string.Join(", ", stillEnglish)}");
    }

    [Theory]
    [MemberData(nameof(NonEnglishLocales))]
    public void Locale_HasDistinctCoreUiLabels(string locale)
    {
        var en = CreateI18n("en");
        var localized = CreateI18n(locale);

        string[] core =
        [
            "choose_language", "language", "get_started", "sign_out", "wallet_settings",
            "home", "profile", "deposit", "withdraw", "invite_code",
        ];

        foreach (var key in core)
        {
            Assert.NotEqual(en.T(key), localized.T(key));
            Assert.False(string.IsNullOrWhiteSpace(localized.T(key)));
            Assert.NotEqual(key, localized.T(key));
        }
    }

    private static I18nService CreateI18n(string locale)
    {
        var js = new FakeJsRuntime();
        var appState = new AppStateService(new LocalStorageService(js));
        appState.SetLanguage(locale);
        return new I18nService(appState);
    }
}
