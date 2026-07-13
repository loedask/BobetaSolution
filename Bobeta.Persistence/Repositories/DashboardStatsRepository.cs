using Bobeta.Application.Common;
using Bobeta.Application.DTOs.Portal;
using Bobeta.Application.Interfaces;
using Bobeta.Domain.Enums;
using Bobeta.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Bobeta.Persistence.Repositories;

public sealed class DashboardStatsRepository(BobetaDbContext db) : IDashboardStatsRepository
{
  public async Task<PlayerDashboardStatsDto> GetPlayerStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var query = FilterPlayersByCountry(filter);

    var newPlayersQuery = query;
    if (filter.FromUtc.HasValue)
      newPlayersQuery = newPlayersQuery.Where(p => p.CreatedAt >= filter.FromUtc.Value);
    if (filter.ToUtc.HasValue)
      newPlayersQuery = newPlayersQuery.Where(p => p.CreatedAt <= filter.ToUtc.Value);

    return new PlayerDashboardStatsDto
    {
      TotalPlayers = await query.CountAsync(cancellationToken),
      NewPlayers = filter.FromUtc.HasValue || filter.ToUtc.HasValue
        ? await newPlayersQuery.CountAsync(cancellationToken)
        : 0,
      VerifiedPlayers = await query.CountAsync(p => p.IsVerified, cancellationToken),
      ActivePlayers = await query.CountAsync(p => p.Status == PlayerStatus.Active, cancellationToken)
    };
  }

  public async Task<IReadOnlyList<CountryCountDto>> GetPlayersByCountryAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var rows = await FilterPlayersByCountry(filter)
      .GroupBy(p => p.CountryCode ?? string.Empty)
      .Select(g => new { CountryCode = g.Key, Count = g.Count() })
      .OrderByDescending(x => x.Count)
      .ToListAsync(cancellationToken);

    return rows
      .Select(x => new CountryCountDto
      {
        CountryCode = string.IsNullOrEmpty(x.CountryCode) ? "—" : x.CountryCode,
        CountryName = string.IsNullOrEmpty(x.CountryCode)
          ? "Unknown"
          : CountryCatalog.GetByCode(x.CountryCode)?.Name ?? x.CountryCode,
        Count = x.Count
      })
      .ToList();
  }

  public async Task<PaymentDashboardStatsDto> GetPaymentStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var query = FilterPayments(filter)
      .Where(p => p.Status == PaymentTransactionStatus.Success);

    var deposits = await query
      .Where(p => p.Type == PaymentTransactionType.Deposit)
      .GroupBy(_ => 1)
      .Select(g => new { Count = g.Count(), Volume = g.Sum(x => x.Amount) })
      .FirstOrDefaultAsync(cancellationToken);

    var withdrawals = await query
      .Where(p => p.Type == PaymentTransactionType.Withdrawal)
      .GroupBy(_ => 1)
      .Select(g => new { Count = g.Count(), Volume = g.Sum(x => x.Amount) })
      .FirstOrDefaultAsync(cancellationToken);

    return new PaymentDashboardStatsDto
    {
      SuccessfulDeposits = deposits?.Count ?? 0,
      DepositVolume = deposits?.Volume ?? 0,
      SuccessfulWithdrawals = withdrawals?.Count ?? 0,
      WithdrawalVolume = withdrawals?.Volume ?? 0
    };
  }

  public async Task<RevenueDashboardStatsDto> GetRevenueStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var query = FilterRevenueAllocations(filter);

    var summary = await query
      .GroupBy(_ => 1)
      .Select(g => new
      {
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Partner = g.Sum(x => x.PartnerAmount),
        Influencer = g.Sum(x => x.InfluencerAmount),
        Retained = g.Sum(x => x.PlatformRetainedAmount),
        Count = g.Count()
      })
      .FirstOrDefaultAsync(cancellationToken);

    // Authoritative influencer ledger for platform-wide totals (covers games with no partner row).
    var influencerShare = filter.LicensePartnerId.HasValue
      ? summary?.Influencer ?? 0
      : await FilterInfluencerCommissions(filter)
          .SumAsync(a => (decimal?)a.InfluencerAmount, cancellationToken) ?? 0;

    return new RevenueDashboardStatsDto
    {
      GrossPlatformRevenue = summary?.Gross ?? 0,
      PartnerSharePaid = summary?.Partner ?? 0,
      InfluencerSharePaid = influencerShare,
      PlatformRetained = summary?.Retained ?? 0,
      AllocationCount = summary?.Count ?? 0
    };
  }

  public async Task<GameDashboardStatsDto> GetGameStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var query = FilterGameResults(filter);

    var summary = await query
      .GroupBy(_ => 1)
      .Select(g => new
      {
        Count = g.Count(),
        Pot = g.Sum(x => x.TotalPot),
        Commission = g.Sum(x => x.PlatformCommission)
      })
      .FirstOrDefaultAsync(cancellationToken);

    return new GameDashboardStatsDto
    {
      GamesPlayed = summary?.Count ?? 0,
      TotalPot = summary?.Pot ?? 0,
      PlatformCommission = summary?.Commission ?? 0
    };
  }

  public async Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueByPartnerAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var rows = await FilterRevenueAllocations(filter)
      .GroupBy(a => new { a.LicensePartnerId, a.LicensePartner.LegalName })
      .Select(g => new
      {
        g.Key.LicensePartnerId,
        g.Key.LegalName,
        PartnerAmount = g.Sum(x => x.PartnerAmount),
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Count = g.Count()
      })
      .OrderByDescending(x => x.PartnerAmount)
      .ToListAsync(cancellationToken);

    return rows
      .Select(x => new PartnerRevenueBreakdownDto
      {
        Key = x.LicensePartnerId.ToString(),
        Label = x.LegalName,
        PartnerAmount = x.PartnerAmount,
        InfluencerAmount = x.InfluencerAmount,
        GrossPlatformRevenue = x.Gross,
        TransactionCount = x.Count
      })
      .ToList();
  }

  public async Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueBySourceAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var rows = await FilterRevenueAllocations(filter)
      .GroupBy(a => a.SourceType)
      .Select(g => new
      {
        Source = g.Key,
        PartnerAmount = g.Sum(x => x.PartnerAmount),
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Count = g.Count()
      })
      .OrderByDescending(x => x.PartnerAmount)
      .ToListAsync(cancellationToken);

    return rows
      .Select(x => new PartnerRevenueBreakdownDto
      {
        Key = x.Source.ToString(),
        Label = FormatSourceType(x.Source),
        PartnerAmount = x.PartnerAmount,
        InfluencerAmount = x.InfluencerAmount,
        GrossPlatformRevenue = x.Gross,
        TransactionCount = x.Count
      })
      .ToList();
  }

  public async Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueByInfluencerAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var rows = await FilterInfluencerCommissions(filter)
      .GroupBy(a => new { a.InfluencerId, a.Influencer.DisplayName, a.Influencer.Code })
      .Select(g => new
      {
        g.Key.InfluencerId,
        g.Key.DisplayName,
        g.Key.Code,
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Count = g.Count()
      })
      .OrderByDescending(x => x.InfluencerAmount)
      .ToListAsync(cancellationToken);

    return rows
      .Select(x => new PartnerRevenueBreakdownDto
      {
        Key = x.InfluencerId.ToString(),
        Label = $"{x.DisplayName} ({x.Code})",
        PartnerAmount = 0,
        InfluencerAmount = x.InfluencerAmount,
        GrossPlatformRevenue = x.Gross,
        TransactionCount = x.Count
      })
      .ToList();
  }

  public async Task<IReadOnlyList<PartnerRevenueAllocationItemDto>> GetRevenueAllocationsAsync(
      DashboardStatsFilter filter,
      int take,
      CancellationToken cancellationToken = default)
  {
    var allocations = await FilterRevenueAllocations(filter)
      .OrderByDescending(a => a.CreatedAt)
      .Take(take)
      .ToListAsync(cancellationToken);

    return allocations
      .Select(a => new PartnerRevenueAllocationItemDto
      {
        Id = a.Id,
        SourceType = a.SourceType,
        SourceId = a.SourceId,
        CountryCode = a.CountryCode,
        CountryName = CountryCatalog.GetByCode(a.CountryCode)?.Name ?? a.CountryCode,
        GrossPlatformRevenue = a.GrossPlatformRevenue,
        PartnerSharePercent = a.PartnerSharePercent,
        PartnerAmount = a.PartnerAmount,
        InfluencerAmount = a.InfluencerAmount,
        PlatformRetainedAmount = a.PlatformRetainedAmount,
        Currency = a.Currency,
        CreatedAt = a.CreatedAt
      })
      .ToList();
  }

  private IQueryable<Domain.Entities.Player> FilterPlayersByCountry(DashboardStatsFilter filter)
  {
    var query = db.Players.AsNoTracking();

    if (filter.CountryCodes is { Count: > 0 })
    {
      var codes = NormalizeCountryCodes(filter.CountryCodes);
      query = query.Where(p => p.CountryCode != null && codes.Contains(p.CountryCode));
    }

    return query;
  }

  private IQueryable<Domain.Entities.PaymentTransaction> FilterPayments(DashboardStatsFilter filter)
  {
    var query = db.PaymentTransactions.AsNoTracking();

    if (filter.FromUtc.HasValue)
      query = query.Where(p => p.CreatedAt >= filter.FromUtc.Value);
    if (filter.ToUtc.HasValue)
      query = query.Where(p => p.CreatedAt <= filter.ToUtc.Value);

    if (filter.CountryCodes is { Count: > 0 })
    {
      var codes = NormalizeCountryCodes(filter.CountryCodes);
      query = query.Where(p => p.Player.CountryCode != null && codes.Contains(p.Player.CountryCode));
    }

    return query;
  }

  private IQueryable<Domain.Entities.RevenueAllocation> FilterRevenueAllocations(DashboardStatsFilter filter)
  {
    var query = db.RevenueAllocations.AsNoTracking();

    if (filter.LicensePartnerId.HasValue)
      query = query.Where(a => a.LicensePartnerId == filter.LicensePartnerId.Value);

    if (filter.FromUtc.HasValue)
      query = query.Where(a => a.CreatedAt >= filter.FromUtc.Value);
    if (filter.ToUtc.HasValue)
      query = query.Where(a => a.CreatedAt <= filter.ToUtc.Value);

    if (filter.CountryCodes is { Count: > 0 })
    {
      var codes = NormalizeCountryCodes(filter.CountryCodes);
      query = query.Where(a => codes.Contains(a.CountryCode));
    }

    return query;
  }

  private IQueryable<Domain.Entities.InfluencerCommissionAllocation> FilterInfluencerCommissions(DashboardStatsFilter filter)
  {
    var query = db.InfluencerCommissionAllocations.AsNoTracking();

    if (filter.FromUtc.HasValue)
      query = query.Where(a => a.CreatedAt >= filter.FromUtc.Value);
    if (filter.ToUtc.HasValue)
      query = query.Where(a => a.CreatedAt <= filter.ToUtc.Value);

    if (filter.CountryCodes is { Count: > 0 })
    {
      var codes = NormalizeCountryCodes(filter.CountryCodes);
      query = query.Where(a => a.Player.CountryCode != null && codes.Contains(a.Player.CountryCode));
    }

    return query;
  }

  private IQueryable<Domain.Entities.GameResult> FilterGameResults(DashboardStatsFilter filter)
  {
    var query = db.GameResults.AsNoTracking();

    if (filter.FromUtc.HasValue)
      query = query.Where(g => g.CreatedAt >= filter.FromUtc.Value);
    if (filter.ToUtc.HasValue)
      query = query.Where(g => g.CreatedAt <= filter.ToUtc.Value);

    if (filter.CountryCodes is { Count: > 0 })
    {
      var codes = NormalizeCountryCodes(filter.CountryCodes);
      query = query.Where(g => g.WinnerPlayer.CountryCode != null && codes.Contains(g.WinnerPlayer.CountryCode));
    }

    return query;
  }

  private static List<string> NormalizeCountryCodes(IReadOnlyList<string> countryCodes) =>
    countryCodes
      .Where(c => !string.IsNullOrWhiteSpace(c))
      .Select(c => c.Trim().ToUpperInvariant())
      .Distinct()
      .ToList();

  private static string FormatSourceType(RevenueAllocationSourceType type) => type switch
  {
    RevenueAllocationSourceType.GameCommission => "Game commission",
    RevenueAllocationSourceType.MoMoDeposit => "MoMo deposit",
    RevenueAllocationSourceType.MoMoWithdrawal => "MoMo withdrawal",
    _ => type.ToString()
  };
}
