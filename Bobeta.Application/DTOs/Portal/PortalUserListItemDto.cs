using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Portal;

public sealed class PortalUserListItemDto
{
  public Guid Id { get; init; }
  public string Email { get; init; } = string.Empty;
  public string DisplayName { get; init; } = string.Empty;
  public PortalUserRole Role { get; init; }
  public bool IsActive { get; init; }
  public DateTime CreatedAt { get; init; }
}
