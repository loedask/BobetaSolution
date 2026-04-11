using Microsoft.Extensions.Hosting;

namespace Bobeta.Application.Common;

/// <summary>Non-production environments where demo seeding and static demo OTP are allowed.</summary>
public static class DemoEnvironmentHelper
{
    /// <summary>Returns true for Development or Staging; false for Production and any other name.</summary>
    public static bool AllowsDemoAuthFeatures(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment(Environments.Staging);
}
