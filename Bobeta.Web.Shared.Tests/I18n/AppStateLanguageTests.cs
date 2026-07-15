using Bobeta.Web.Shared.Services;
using Bobeta.Web.Shared.State;
using Bobeta.Web.Shared.Tests.Infrastructure;
using Xunit;

namespace Bobeta.Web.Shared.Tests.I18n;

public sealed class AppStateLanguageTests
{
    private const string StateKey = "bobeta_app_state";

    [Theory]
    [InlineData("fr-FR", "fr")]
    [InlineData("en-US", "en")]
    [InlineData("sw-TZ", "sw")]
    [InlineData("ln-CD", "ln")]
    [InlineData("kg-CG", "kt")]
    [InlineData("de-DE", "en")]
    public async Task LoadAsync_WhenNoSavedState_UsesBrowserLanguage(string browserLanguage, string expected)
    {
        var js = new FakeJsRuntime { BrowserLanguage = browserLanguage };
        var appState = new AppStateService(new LocalStorageService(js));

        await appState.LoadAsync();

        Assert.Equal(expected, appState.State.SelectedLanguage);
    }

    [Fact]
    public async Task LoadAsync_WhenSavedLanguageExists_PrefersPersistedChoiceOverBrowser()
    {
        var js = new FakeJsRuntime { BrowserLanguage = "fr-FR" };
        js.SetStoredJson(StateKey, new AppState { SelectedLanguage = "sw" });
        var appState = new AppStateService(new LocalStorageService(js));

        await appState.LoadAsync();

        Assert.Equal("sw", appState.State.SelectedLanguage);
    }

    [Fact]
    public async Task LoadAsync_WhenSavedLanguageIsRegionalTag_NormalizesToSupportedCode()
    {
        var js = new FakeJsRuntime { BrowserLanguage = "en-US" };
        js.SetStoredJson(StateKey, new AppState { SelectedLanguage = "fr-FR" });
        var appState = new AppStateService(new LocalStorageService(js));

        await appState.LoadAsync();

        Assert.Equal("fr", appState.State.SelectedLanguage);
    }

    [Fact]
    public async Task SetLanguage_ThenPersist_RoundTripsManualSelection()
    {
        var js = new FakeJsRuntime { BrowserLanguage = "en-US" };
        var appState = new AppStateService(new LocalStorageService(js));

        appState.SetLanguage("fr");
        await appState.PersistAsync();

        var reloaded = new AppStateService(new LocalStorageService(js));
        await reloaded.LoadAsync();

        Assert.Equal("fr", reloaded.State.SelectedLanguage);
    }

    [Fact]
    public void SetLanguage_RaisesStateChanged()
    {
        var js = new FakeJsRuntime();
        var appState = new AppStateService(new LocalStorageService(js));
        var raised = 0;
        appState.StateChanged += () => raised++;

        appState.SetLanguage("fr");

        Assert.Equal(1, raised);
        Assert.Equal("fr", appState.State.SelectedLanguage);
    }

    [Fact]
    public void SetLanguage_NormalizesUnsupportedCodeToEnglish()
    {
        var js = new FakeJsRuntime();
        var appState = new AppStateService(new LocalStorageService(js));

        appState.SetLanguage("de");

        Assert.Equal("en", appState.State.SelectedLanguage);
    }
}
