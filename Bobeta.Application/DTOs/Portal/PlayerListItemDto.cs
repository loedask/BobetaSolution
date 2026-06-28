using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Portal;

public sealed class PlayerListItemDto
{
  public Guid Id { get; init; }
  public string PhoneNumber { get; init; } = string.Empty;
  public string PlayerName { get; init; } = string.Empty;
  public string Language { get; init; } = string.Empty;
  public DateTime CreatedAt { get; init; }
  public bool IsVerified { get; init; }
  public PlayerStatus Status { get; init; }
}
