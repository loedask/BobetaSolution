using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Games;

namespace Bobeta.Mobile.Pages;

public partial class MyWaitingTablesPage : ContentPage
{
    private MyWaitingTablesViewModel? _vm;

    public MyWaitingTablesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = MauiProgram.Services.GetRequiredService<MyWaitingTablesViewModel>();
        BindingContext = _vm;
        _vm.StateChanged -= OnVmChanged;
        _vm.StateChanged += OnVmChanged;

        var i18n = MauiProgram.Services.GetRequiredService<I18nService>();
        Title = i18n.T("my_waiting_tables");
        TitleLabel.Text = i18n.T("my_waiting_tables");
        HintLabel.Text = i18n.T("my_waiting_hint");
        EmptyTitle.Text = i18n.T("my_waiting_empty_title");
        EmptyBody.Text = i18n.T("my_waiting_empty_body");
        CreateBtn.Text = i18n.T("create_game");

        _ = LoadAsync();
        SyncUi();
    }

    protected override void OnDisappearing()
    {
        if (_vm != null)
            _vm.StateChanged -= OnVmChanged;
        base.OnDisappearing();
    }

    private async Task LoadAsync()
    {
        if (_vm != null)
            await _vm.LoadAsync();
        SyncUi();
    }

    private void OnVmChanged() => MainThread.BeginInvokeOnMainThread(SyncUi);

    private void SyncUi()
    {
        if (_vm == null) return;
        ErrorLabel.Text = _vm.ErrorMessage ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_vm.ErrorMessage);
        TablesList.ItemsSource = null;
        TablesList.ItemsSource = _vm.WaitingTables;
        Refresh.IsRefreshing = _vm.IsLoading;
    }

    private async void OnRefresh(object? sender, EventArgs e) => await LoadAsync();

    private async void OnOpen(object? sender, EventArgs e)
    {
        if (_vm == null || sender is not Button { CommandParameter: Guid id }) return;
        await _vm.OpenAsync(id);
    }

    private async void OnCancel(object? sender, EventArgs e)
    {
        if (_vm == null || sender is not Button { CommandParameter: Guid id }) return;
        await _vm.CancelAsync(id);
        SyncUi();
    }

    private async void OnCreate(object? sender, EventArgs e)
    {
        await MauiProgram.Services.GetRequiredService<INavigationService>().ToMainTabsAsync("CreateGame");
    }
}
