using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Bobeta.Application.Common;

/// <summary>Demo auth helpers: environment-gated seeding vs config-gated static OTP.</summary>
public static class DemoEnvironmentHelper
{
    /// <summary>Returns true for Development or Staging; false for Production and any other name.</summary>
    public static bool AllowsDemoAuthFeatures(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment(Environments.Staging);

    /// <summary>
    /// When true, configured demo numbers skip SMS on send-otp and accept <c>DemoAuth:StaticOtp</c> on verify.
    /// Works in any environment (including Production) when explicitly enabled via configuration.
    /// </summary>
    public static bool IsStaticOtpEnabled(IConfiguration configuration) =>
        configuration.GetValue("DemoAuth:EnableStaticOtp", false);

    /// <summary>Returns true when <paramref name="normalizedPhone"/> is listed under <c>DemoAuth:PhoneNumbers</c>.</summary>
    public static bool IsConfiguredDemoPhoneNumber(IConfiguration configuration, string normalizedPhone)
    {
        foreach (var child in configuration.GetSection("DemoAuth:PhoneNumbers").GetChildren())
        {
            var configured = child.Value;
            if (string.IsNullOrEmpty(configured))
                continue;
            if (PhoneNumberHelper.Normalize(configured) == normalizedPhone)
                return true;
        }

        return false;
    }
}
