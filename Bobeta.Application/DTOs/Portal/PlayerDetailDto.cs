using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Portal;

public sealed class PlayerDetailDto
{
  public Guid Id { get; init; }
  public string PhoneNumber { get; init; } = string.Empty;
  public string PlayerName { get; init; } = string.Empty;
  public string Language { get; init; } = string.Empty;
  public string? CountryCode { get; init; }
  public string? CountryName { get; init; }
  public DateTime CreatedAt { get; init; }
  public bool IsVerified { get; init; }
  public PlayerStatus Status { get; init; }
  public PlayerWalletSummaryDto? Wallet { get; init; }
}

public sealed class PlayerWalletSummaryDto
{
  public decimal Balance { get; init; }
  public decimal LockedBalance { get; init; }
  public DateTime UpdatedAt { get; init; }
}
