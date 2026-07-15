using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.Tests.Infrastructure;
using Xunit;

namespace Bobeta.Web.Shared.Tests.I18n;

public sealed class I18nServiceTests
{
    [Fact]
    public void T_English_ReturnsEnglishCopy()
    {
        var i18n = CreateI18n("en");

        Assert.Equal("Choose Language", i18n.T("choose_language"));
        Assert.Equal("Language", i18n.T("language"));
        Assert.Equal("Get Started", i18n.T("get_started"));
        Assert.Equal("Play Smart. Win Real Money.", i18n.T("tagline"));
    }

    [Fact]
    public void T_French_ReturnsFrenchCopy()
    {
        var i18n = CreateI18n("fr");

        Assert.Equal("Choisir la langue", i18n.T("choose_language"));
        Assert.Equal("Langue", i18n.T("language"));
        Assert.Equal("Commencer", i18n.T("get_started"));
        Assert.Equal("Jouez malin. Gagnez de l'argent réel.", i18n.T("tagline"));
    }

    [Theory]
    [InlineData("kt")]
    [InlineData("ln")]
    [InlineData("sw")]
    public void T_LocalesWithoutOverrides_FallBackToEnglishCopy(string locale)
    {
        var i18n = CreateI18n(locale);

        Assert.Equal("Choose Language", i18n.T("choose_language"));
        Assert.Equal("Get Started", i18n.T("get_started"));
    }

    [Fact]
    public void T_MissingKey_ReturnsKey()
    {
        var i18n = CreateI18n("en");
        Assert.Equal("totally_missing_key", i18n.T("totally_missing_key"));
    }

    [Fact]
    public void T_AfterSetLanguage_UsesNewLocale()
    {
        var js = new FakeJsRuntime();
        var appState = new AppStateService(new LocalStorageService(js));
        var i18n = new I18nService(appState);

        Assert.Equal("Choose Language", i18n.T("choose_language"));

        appState.SetLanguage("fr");

        Assert.Equal("fr", i18n.Locale);
        Assert.Equal("Choisir la langue", i18n.T("choose_language"));
    }

    [Fact]
    public void T_EverySupportedLocale_ResolvesCoreKeys()
    {
        foreach (var (code, _, _) in I18nService.SupportedLocales)
        {
            var i18n = CreateI18n(code);
            Assert.False(string.IsNullOrWhiteSpace(i18n.T("app_name")), $"app_name missing for {code}");
            Assert.False(string.IsNullOrWhiteSpace(i18n.T("choose_language")), $"choose_language missing for {code}");
            Assert.False(string.IsNullOrWhiteSpace(i18n.T("language")), $"language missing for {code}");
            Assert.NotEqual("choose_language", i18n.T("choose_language"));
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
