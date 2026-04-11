namespace Bobeta.Mobile.Services;

public interface INavigationService
{
    Task ToPhoneLoginAsync();
    Task ToOtpVerificationAsync();
    Task ToCreatePlayerAsync();
    Task ToMainTabsAsync(string tabRoute = "Dashboard");
    Task ToWelcomeAsync();
    Task ToDepositAsync();
    Task ToWithdrawAsync();
    Task ToGamePlayAsync(Guid sessionId);
    Task BackAsync();
}
