using Bobeta.Mobile.Pages;
using Bobeta.Mobile.Services;

namespace Bobeta.Mobile;

public partial class AppShell : Shell
{
    public AppShell(AppStateService appState)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(PhoneLoginPage), typeof(PhoneLoginPage));
        Routing.RegisterRoute(nameof(OtpVerificationPage), typeof(OtpVerificationPage));
        Routing.RegisterRoute(nameof(CreatePlayerPage), typeof(CreatePlayerPage));
        Routing.RegisterRoute(nameof(DepositPage), typeof(DepositPage));
        Routing.RegisterRoute(nameof(WithdrawPage), typeof(WithdrawPage));
        Routing.RegisterRoute("GamePlay", typeof(GamePlayPage));

        Loaded += async (_, _) =>
        {
            await appState.LoadAsync();
            if (appState.State.IsAuthenticated)
                await GoToAsync("//MainTabs/Dashboard");
        };
    }
}
