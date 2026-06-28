using Bobeta.Domain.Enums;

namespace Bobeta.Domain.Entities;

/// <summary>Employee account for Bobeta.Portal (support / operations).</summary>
public class PortalUser
{
  public Guid Id { get; set; }
  public string Email { get; set; } = string.Empty;
  public string DisplayName { get; set; } = string.Empty;
  public string PasswordHash { get; set; } = string.Empty;
  public PortalUserRole Role { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }
  public Guid? CreatedById { get; set; }
  public PortalUser? CreatedBy { get; set; }
  public LicensePartner? LicensePartner { get; set; }
}
