using Bobeta.Mobile.Services;

namespace Bobeta.Mobile.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var appState = MauiProgram.Services.GetRequiredService<AppStateService>();
        LanguagePicker.SelectedIndexChanged -= OnLanguageChanged;
        LanguagePicker.Items.Clear();
        foreach (var (code, label, _) in I18nService.SupportedLocales)
            LanguagePicker.Items.Add($"{label} ({code})");
        var idx = I18nService.SupportedLocales.ToList().FindIndex(x => x.Code == appState.State.SelectedLanguage);
        LanguagePicker.SelectedIndex = idx >= 0 ? idx : 0;
        LanguagePicker.SelectedIndexChanged += OnLanguageChanged;
        RefreshTexts();
    }

    private void RefreshTexts()
    {
        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        TitleLabel.Text = i18n.T("app_name");
        TaglineLabel.Text = i18n.T("tagline");
        SecureLabel.Text = i18n.T("secure_momo");
        LangHeader.Text = i18n.T("choose_language");
        GetStartedButton.Text = i18n.T("get_started");
        GetStartedButton.BackgroundColor = Color.FromArgb("#f0c040");
        Footer1.Text = i18n.T("secure");
        Footer2.Text = i18n.T("fair_randomized");
        Footer3.Text = i18n.T("instant_payouts");
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        var appState = MauiProgram.Services.GetRequiredService<AppStateService>();
        var i = LanguagePicker.SelectedIndex;
        if (i < 0 || i >= I18nService.SupportedLocales.Count) return;
        var code = I18nService.SupportedLocales[i].Code;
        appState.SetLanguage(code);
        await appState.PersistAsync();
        RefreshTexts();
    }

    private async void OnGetStarted(object? sender, EventArgs e)
    {
        var nav = MauiProgram.Services.GetRequiredService<INavigationService>();
        await nav.ToPhoneLoginAsync();
    }
}
