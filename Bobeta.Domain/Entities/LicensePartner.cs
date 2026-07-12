namespace Bobeta.Domain.Entities;

/// <summary>Contracted license holder for one or more operating countries.</summary>
public class LicensePartner
{
  public Guid Id { get; set; }
  public string LegalName { get; set; } = string.Empty;
  public string ContactEmail { get; set; } = string.Empty;
  public Guid PortalUserId { get; set; }
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAt { get; set; }

  public PortalUser PortalUser { get; set; } = null!;
  public ICollection<LicensePartnerCountryAssignment> CountryAssignments { get; set; } = [];
}
