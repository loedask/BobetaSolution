using Bobeta.Domain.Enums;

namespace Bobeta.Application.DTOs.Portal;

public sealed class PartnerRevenueReportDto
{
  public Guid LicensePartnerId { get; init; }
  public string LegalName { get; init; } = string.Empty;
  public DateTime? From { get; init; }
  public DateTime? To { get; init; }
  public decimal TotalPartnerAmount { get; init; }
  public decimal TotalGrossPlatformRevenue { get; init; }
  public int TransactionCount { get; init; }
  public IReadOnlyList<PartnerRevenueBreakdownDto> ByCountry { get; init; } = [];
  public IReadOnlyList<PartnerRevenueBreakdownDto> BySource { get; init; } = [];
  public IReadOnlyList<PartnerRevenueAllocationItemDto> RecentAllocations { get; init; } = [];
}

public sealed class PartnerRevenueBreakdownDto
{
  public string Key { get; init; } = string.Empty;
  public string Label { get; init; } = string.Empty;
  public decimal PartnerAmount { get; init; }
  public decimal GrossPlatformRevenue { get; init; }
  public int TransactionCount { get; init; }
}

public sealed class PartnerRevenueAllocationItemDto
{
  public Guid Id { get; init; }
  public RevenueAllocationSourceType SourceType { get; init; }
  public Guid SourceId { get; init; }
  public string CountryCode { get; init; } = string.Empty;
  public string CountryName { get; init; } = string.Empty;
  public decimal GrossPlatformRevenue { get; init; }
  public decimal PartnerSharePercent { get; init; }
  public decimal PartnerAmount { get; init; }
  public string Currency { get; init; } = string.Empty;
  public DateTime CreatedAt { get; init; }
}
