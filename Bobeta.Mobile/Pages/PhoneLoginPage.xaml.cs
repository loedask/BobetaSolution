using Bobeta.Mobile.Data;
using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Auth;

namespace Bobeta.Mobile.Pages;

public partial class PhoneLoginPage : ContentPage
{
    private PhoneLoginViewModel? _vm;

    public PhoneLoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<PhoneLoginViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        HeaderLabel.Text = i18n.T("enter_number");
        DescLabel.Text = i18n.T("send_code_desc");
        PhoneEntry.Placeholder = i18n.T("phone_placeholder");
        SendButton.Text = i18n.T("send_code");

        CountryPicker.Items.Clear();
        foreach (var c in CountryDialOption.All)
            CountryPicker.Items.Add($"{c.Name} ({c.Dial})");
        var defaultIdx = Array.FindIndex(CountryDialOption.All, x => x.CountryCode == CountryDialOption.DefaultCountryCode);
        CountryPicker.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;
        CountryPicker.SelectedIndexChanged -= OnCountryChanged;
        CountryPicker.SelectedIndexChanged += OnCountryChanged;
        OnCountryChanged(null, EventArgs.Empty);
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        base.OnDisappearing();
    }

    private void OnCountryChanged(object? sender, EventArgs e)
    {
        if (_vm == null || CountryPicker.SelectedIndex < 0) return;
        var c = CountryDialOption.All[CountryPicker.SelectedIndex];
        _vm.SetCountry(c.Dial, c.Digits);
        PhoneEntry.MaxLength = c.Digits;
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading;
        SendButton.IsEnabled = _vm.CanSubmit && !_vm.IsLoading;
    }

    private async void OnSend(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.SendOtpAsync();
    }
}
