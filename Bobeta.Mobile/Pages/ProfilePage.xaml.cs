using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Profile;

namespace Bobeta.Mobile.Pages;

public partial class ProfilePage : ContentPage
{
    private ProfileViewModel? _vm;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<ProfileViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        var appState = MauiProgram.Services.GetRequiredService<AppStateService>();
        Title = i18n.T("profile");
        GamesLabel.Text = i18n.T("games");
        WinsLabel.Text = i18n.T("wins");
        WinRateLabel.Text = i18n.T("win_rate");
        LangSection.Text = i18n.T("language");
        WalletBtn.Text = i18n.T("wallet_settings");
        SignOutBtn.Text = i18n.T("sign_out");

        LanguagePicker.Items.Clear();
        foreach (var (code, label, _) in I18nService.SupportedLocales)
            LanguagePicker.Items.Add($"{label} ({code})");
        var idx = I18nService.SupportedLocales.ToList().FindIndex(x => x.Code == appState.State.SelectedLanguage);
        LanguagePicker.SelectedIndex = idx >= 0 ? idx : 0;
        LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
        LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
        base.OnDisappearing();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(() => { });

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        var appState = MauiProgram.Services.GetRequiredService<AppStateService>();
        var i = LanguagePicker.SelectedIndex;
        if (i < 0 || i >= I18nService.SupportedLocales.Count) return;
        appState.SetLanguage(I18nService.SupportedLocales[i].Code);
        await appState.PersistAsync();
    }

    private async void OnWallet(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//MainTabs/Dashboard");

    private async void OnSignOut(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        var ok = await DisplayAlertAsync(
            i18n.T("sign_out"),
            i18n.T("sign_out_confirm"),
            i18n.T("sign_out"),
            i18n.T("cancel"));
        if (ok)
            await _vm.SignOutAsync();
    }
}
