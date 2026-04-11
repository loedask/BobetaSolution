using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Auth;

namespace Bobeta.Mobile.Pages;

public partial class CreatePlayerPage : ContentPage
{
    private CreatePlayerViewModel? _vm;

    public CreatePlayerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<CreatePlayerViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        HeaderLabel.Text = i18n.T("choose_player_name");
        DescLabel.Text = i18n.T("player_name_desc");
        CreateButton.Text = i18n.T("create_account");
        NameEntry.Placeholder = i18n.T("player_name_placeholder");
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        base.OnDisappearing();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        Busy.IsRunning = _vm.IsLoading;
        CreateButton.IsEnabled = _vm.CanSubmit && !_vm.IsLoading;
    }

    private async void OnCreate(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.RegisterAsync();
    }
}
