using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Portal;

public sealed class RegisterPortalUserRequest
{
  public string Email { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public PortalUserRole Role { get; set; } = PortalUserRole.Member;
}
