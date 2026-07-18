using Bobeta.Client.Services;
using Bobeta.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Bobeta.Mobile.Services.Push;

/// <summary>Requests notification permission and registers the FCM token with the API after login.</summary>
public sealed class PushRegistrationService(
    IPushTokenProvider tokenProvider,
    DeviceApiService deviceApi,
    AppStateService appState,
    ILogger<PushRegistrationService> logger)
{
    private string? _lastRegisteredToken;

    public async Task RegisterIfPossibleAsync(CancellationToken cancellationToken = default)
    {
        if (!appState.State.IsAuthenticated)
            return;

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                logger.LogInformation("Notification permission not granted; phone push skipped.");
                return;
            }
        }
        catch (Exception ex)
        {
            // PostNotifications may be unsupported on older APIs / non-Android.
            logger.LogDebug(ex, "Notification permission check skipped.");
        }

        string? token;
        try
        {
            token = await tokenProvider.GetTokenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not obtain FCM token.");
            return;
        }

        if (string.IsNullOrWhiteSpace(token))
            return;

        if (string.Equals(_lastRegisteredToken, token, StringComparison.Ordinal))
            return;

        var res = await deviceApi.RegisterAsync(token, tokenProvider.Platform, cancellationToken);
        if (res.IsSuccess)
        {
            _lastRegisteredToken = token;
            Preferences.Default.Set("fcm_token", token);
            logger.LogInformation("Device push token registered.");
        }
        else
            logger.LogWarning("Device push token registration failed: {Error}", res.ErrorMessage);
    }

    public async Task UnregisterLocalTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = Preferences.Default.Get("fcm_token", string.Empty);
        if (string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            await deviceApi.UnregisterAsync(token, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unregister device token failed.");
        }

        Preferences.Default.Remove("fcm_token");
        _lastRegisteredToken = null;
    }
}
