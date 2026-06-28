namespace Bobeta.Application.Configuration;

/// <summary>Portal bootstrap settings. Admin emails are provisioned on startup; admins register other staff in the UI.</summary>
public class PortalSettings
{
  public const string SectionName = "Portal";

  /// <summary>Email addresses that receive Admin role when the portal starts (production: set in App Service configuration).</summary>
  public List<string> AdminEmails { get; set; } = [];

  /// <summary>Initial password for newly provisioned admin accounts. Change after first login in production.</summary>
  public string BootstrapPassword { get; set; } = string.Empty;
}
