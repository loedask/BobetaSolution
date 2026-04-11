namespace Bobeta.Mobile.Services;

public class ShellNavigationService : INavigationService
{
    private static Task RunOnMainThread(Func<Task> action)
    {
        if (MainThread.IsMainThread)
            return action();
        return MainThread.InvokeOnMainThreadAsync(action);
    }

    public Task ToPhoneLoginAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(Pages.PhoneLoginPage)));

    public Task ToOtpVerificationAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(Pages.OtpVerificationPage)));

    public Task ToCreatePlayerAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(Pages.CreatePlayerPage)));

    public Task ToMainTabsAsync(string tabRoute = "Dashboard") =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"//MainTabs/{tabRoute}"));

    public Task ToWelcomeAsync() => RunOnMainThread(() => Shell.Current.GoToAsync("//Welcome"));

    public Task ToDepositAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(Pages.DepositPage)));

    public Task ToWithdrawAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(nameof(Pages.WithdrawPage)));

    public Task ToGamePlayAsync(Guid sessionId) =>
        RunOnMainThread(() => Shell.Current.GoToAsync($"GamePlay?SessionId={sessionId}"));

    public Task BackAsync() => RunOnMainThread(() => Shell.Current.GoToAsync(".."));
}
