using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Games;

namespace Bobeta.Mobile.Pages;

public partial class CreateGamePage : ContentPage
{
    private CreateGameViewModel? _vm;

    public CreateGamePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<CreateGameViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        HeaderLabel.Text = i18n.T("select_bet_amount");
        DescLabel.Text = i18n.T("choose_bet_desc");
        BetLabel.Text = i18n.T("your_bet");
        CreateBtn.Text = i18n.T("create_game_session");
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
        CreateBtn.IsEnabled = _vm.CanSubmit && !_vm.IsLoading;
    }

    private void OnPreset(object? sender, EventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Text, out var n))
            _vm?.SetPresetAmount(n);
    }

    private async void OnCreate(object? sender, EventArgs e)
    {
        if (_vm == null) return;
        await _vm.CreateAsync();
    }
}
