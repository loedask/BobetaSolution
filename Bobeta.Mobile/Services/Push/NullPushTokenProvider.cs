namespace Bobeta.Mobile.Services.Push;

/// <summary>No FCM SDK wired (Windows / missing google-services.json).</summary>
public sealed class NullPushTokenProvider : IPushTokenProvider
{
    public Bobeta.Domain.Enums.DevicePlatform Platform =>
        Microsoft.Maui.Devices.DeviceInfo.Current.Platform == Microsoft.Maui.Devices.DevicePlatform.iOS
            ? Bobeta.Domain.Enums.DevicePlatform.Ios
            : Bobeta.Domain.Enums.DevicePlatform.Android;

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
