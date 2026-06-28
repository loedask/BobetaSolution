namespace Bobeta.Application.Configuration;

/// <summary>Portal bootstrap settings. Platform owner emails are provisioned on startup; they register other staff in the UI.</summary>
public class PortalSettings
{
  public const string SectionName = "Portal";

  /// <summary>Email addresses that receive PlatformOwner role when the portal starts (production: set in App Service configuration).</summary>
  public List<string> PlatformOwnerEmails { get; set; } = [];

  /// <summary>Initial password for newly provisioned admin accounts. Change after first login in production.</summary>
  public string BootstrapPassword { get; set; } = string.Empty;
}
