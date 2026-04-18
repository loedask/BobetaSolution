using Bobeta.Mobile.Pages;

namespace Bobeta.Mobile.Services;

public class ShellNavigationService : INavigationService
{
    private static Task RunOnMainThread(Func<Task> action)
    {
        if (MainThread.IsMainThread)
            return action();
        return MainThread.InvokeOnMainThreadAsync(action);
    }

    /// <summary>Welcome is a root ShellContent (sibling of TabBar). GoToAsync to a registered route fails on some Android versions; push onto the shell navigation stack instead.</summary>
    public Task ToPhoneLoginAsync() => RunOnMainThread(async () =>
    {
        if (Shell.Current?.Navigation is null)
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync(nameof(PhoneLoginPage));
            return;
        }

        await Shell.Current.Navigation.PushAsync(new PhoneLoginPage());
    });

    public Task ToOtpVerificationAsync() => RunOnMainThread(async () =>
    {
        if (Shell.Current?.Navigation is null)
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync(nameof(OtpVerificationPage));
            return;
        }

        await Shell.Current.Navigation.PushAsync(new OtpVerificationPage());
    });

    public Task ToCreatePlayerAsync() => RunOnMainThread(async () =>
    {
        if (Shell.Current?.Navigation is null)
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync(nameof(CreatePlayerPage));
            return;
        }

        await Shell.Current.Navigation.PushAsync(new CreatePlayerPage());
    });

    public Task ToMainTabsAsync(string tabRoute = "Dashboard") =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"//MainTabs/{tabRoute}"));

    public Task ToWelcomeAsync() => RunOnMainThread(() => Shell.Current.GoToAsync("//Welcome"));

    public Task ToDepositAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(DepositPage)));

    public Task ToWithdrawAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(WithdrawPage)));

    public Task ToGamePlayAsync(Guid sessionId) =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"GamePlay?SessionId={sessionId}"));

    public Task BackAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(".."));
}
