using Bobeta.Mobile.Pages;
using Bobeta.Mobile.Services;
using Bobeta.Mobile.ViewModels.Notifications;

namespace Bobeta.Mobile;

public partial class AppShell : Shell
{
    public AppShell(AppStateService appState, NotificationInboxViewModel inbox)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(PhoneLoginPage), typeof(PhoneLoginPage));
        Routing.RegisterRoute(nameof(OtpVerificationPage), typeof(OtpVerificationPage));
        Routing.RegisterRoute(nameof(CreatePlayerPage), typeof(CreatePlayerPage));
        Routing.RegisterRoute(nameof(DepositPage), typeof(DepositPage));
        Routing.RegisterRoute(nameof(WithdrawPage), typeof(WithdrawPage));
        Routing.RegisterRoute(nameof(MyWaitingTablesPage), typeof(MyWaitingTablesPage));
        Routing.RegisterRoute("GamePlay", typeof(GamePlayPage));

        Loaded += async (_, _) =>
        {
            await appState.LoadAsync();
            if (appState.State.IsAuthenticated)
            {
                await inbox.InitializeAsync();
                try
                {
                    var push = MauiProgram.Services.GetService<Bobeta.Mobile.Services.Push.PushRegistrationService>();
                    if (push is not null)
                        await push.RegisterIfPossibleAsync();
                }
                catch
                {
                    // Push is optional until Firebase is configured.
                }
                await GoToAsync("//MainTabs/Dashboard");
            }
        };
    }
}
