namespace Bobeta.Application.DTOs.Portal;

public sealed class InfluencerListItemDto
{
  public Guid Id { get; init; }
  public string DisplayName { get; init; } = string.Empty;
  public string ContactEmail { get; init; } = string.Empty;
  public string PortalEmail { get; init; } = string.Empty;
  public string Code { get; init; } = string.Empty;
  public decimal CommissionPercent { get; init; }
  public bool IsActive { get; init; }
  public DateTime CreatedAt { get; init; }
}

public sealed class RegisterInfluencerRequest
{
  public string DisplayName { get; set; } = string.Empty;
  public string ContactEmail { get; set; } = string.Empty;
  public string PortalEmail { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public decimal CommissionPercent { get; set; }
  /// <summary>Optional custom code; generated if empty.</summary>
  public string? Code { get; set; }
}

public sealed class UpdateInfluencerCommissionRequest
{
  public Guid InfluencerId { get; set; }
  public decimal CommissionPercent { get; set; }
}

public sealed class InfluencerRevenueReportDto
{
  public Guid InfluencerId { get; init; }
  public string DisplayName { get; init; } = string.Empty;
  public string Code { get; init; } = string.Empty;
  public DateTime? From { get; init; }
  public DateTime? To { get; init; }
  public decimal TotalInfluencerAmount { get; init; }
  public int TransactionCount { get; init; }
  public IReadOnlyList<InfluencerRevenueAllocationItemDto> RecentAllocations { get; init; } = [];
}

public sealed class InfluencerRevenueAllocationItemDto
{
  public Guid Id { get; init; }
  public Guid GameSessionId { get; init; }
  public Guid PlayerId { get; init; }
  public decimal GrossPlatformRevenue { get; init; }
  public decimal AttributionBase { get; init; }
  public decimal CommissionPercent { get; init; }
  public decimal InfluencerAmount { get; init; }
  public string Currency { get; init; } = "XAF";
  public DateTime CreatedAt { get; init; }
}

public sealed class InfluencerProgramSettingsDto
{
  public decimal PlayerDiscountPercent { get; init; }
}

public sealed class UpdateInfluencerProgramSettingsRequest
{
  public decimal PlayerDiscountPercent { get; set; }
}
