using System.Security.Claims;
using Bobeta.Domain.Enums;

namespace Bobeta.Portal.Services;

public sealed class PortalUserContext(IHttpContextAccessor httpContextAccessor)
{
  public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

  public Guid? UserId
  {
    get
    {
      var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      return Guid.TryParse(raw, out var id) ? id : null;
    }
  }

  public PortalUserRole? Role
  {
    get
    {
      var role = User.FindFirst(ClaimTypes.Role)?.Value;
      return Enum.TryParse<PortalUserRole>(role, out var parsed) ? parsed : null;
    }
  }

  public bool IsPlatformOwner => User.IsInRole(nameof(PortalUserRole.PlatformOwner));
  public bool IsLicensePartner => User.IsInRole(nameof(PortalUserRole.LicensePartner));
}
