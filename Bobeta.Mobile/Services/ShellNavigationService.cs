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

    public Task ToPhoneLoginAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(PhoneLoginPage)));

    public Task ToOtpVerificationAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(OtpVerificationPage)));

    public Task ToCreatePlayerAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(CreatePlayerPage)));

    public Task ToMainTabsAsync(string tabRoute = "Dashboard") =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"//MainTabs/{tabRoute}"));

    public Task ToWelcomeAsync() => RunOnMainThread(() => Shell.Current.GoToAsync("//MainTabs/Welcome"));

    public Task ToDepositAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(DepositPage)));

    public Task ToWithdrawAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(WithdrawPage)));

    public Task ToGamePlayAsync(Guid sessionId) =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"GamePlay?SessionId={sessionId}"));

    public Task BackAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(".."));
}
