using Bobeta.Web.Shared.Services;
using Xunit;

namespace Bobeta.Web.Shared.Tests.I18n;

public sealed class LocaleResolutionTests
{
    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData("en-US", "en")]
    [InlineData("en_GB", "en")]
    [InlineData("fr", "fr")]
    [InlineData("fr-FR", "fr")]
    [InlineData("fr_CA", "fr")]
    [InlineData("sw", "sw")]
    [InlineData("sw-KE", "sw")]
    [InlineData("ln", "ln")]
    [InlineData("ln-CD", "ln")]
    [InlineData("kt", "kt")]
    [InlineData("kg", "kt")]
    [InlineData("kg-CG", "kt")]
    public void ResolveSupportedLocale_MapsKnownTags(string input, string expected) =>
        Assert.Equal(expected, I18nService.ResolveSupportedLocale(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("de")]
    [InlineData("de-DE")]
    [InlineData("pt-BR")]
    [InlineData("zh-CN")]
    public void ResolveSupportedLocale_UnknownOrEmpty_FallsBackToEnglish(string? input) =>
        Assert.Equal("en", I18nService.ResolveSupportedLocale(input));

    [Fact]
    public void ResolveSupportedLocale_Unknown_UsesCustomFallback() =>
        Assert.Equal("fr", I18nService.ResolveSupportedLocale("de-DE", fallback: "fr"));

    [Fact]
    public void SupportedLocales_ContainsExpectedCodes()
    {
        var codes = I18nService.SupportedLocales.Select(x => x.Code).ToArray();
        Assert.Equal(new[] { "en", "fr", "kt", "ln", "sw" }, codes);
    }
}
