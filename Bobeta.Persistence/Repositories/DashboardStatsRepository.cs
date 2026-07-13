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
    var query = FilterPlayers(filter);

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
    var rows = await FilterPlayers(filter)
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

    var deposits = query.Where(p => p.Type == PaymentTransactionType.Deposit);
    var withdrawals = query.Where(p => p.Type == PaymentTransactionType.Withdrawal);

    return new PaymentDashboardStatsDto
    {
      SuccessfulDeposits = await deposits.CountAsync(cancellationToken),
      DepositVolume = await deposits.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0,
      SuccessfulWithdrawals = await withdrawals.CountAsync(cancellationToken),
      WithdrawalVolume = await withdrawals.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0
    };
  }

  public async Task<RevenueDashboardStatsDto> GetRevenueStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    if (filter.InfluencerId.HasValue)
    {
      var influencerQuery = FilterInfluencerCommissions(filter);

      return new RevenueDashboardStatsDto
      {
        GrossPlatformRevenue = await influencerQuery.SumAsync(x => (decimal?)x.GrossPlatformRevenue, cancellationToken) ?? 0,
        PartnerSharePaid = 0,
        InfluencerSharePaid = await influencerQuery.SumAsync(x => (decimal?)x.InfluencerAmount, cancellationToken) ?? 0,
        PlatformRetained = 0,
        AllocationCount = await influencerQuery.CountAsync(cancellationToken)
      };
    }

    var query = FilterRevenueAllocations(filter);

    var gross = await query.SumAsync(x => (decimal?)x.GrossPlatformRevenue, cancellationToken) ?? 0;
    var partner = await query.SumAsync(x => (decimal?)x.PartnerAmount, cancellationToken) ?? 0;
    var retained = await query.SumAsync(x => (decimal?)x.PlatformRetainedAmount, cancellationToken) ?? 0;
    var count = await query.CountAsync(cancellationToken);

    // Authoritative influencer ledger for platform-wide totals (covers games with no partner row).
    var influencerShare = filter.LicensePartnerId.HasValue
      ? await query.SumAsync(x => (decimal?)x.InfluencerAmount, cancellationToken) ?? 0
      : await FilterInfluencerCommissions(filter)
          .SumAsync(a => (decimal?)a.InfluencerAmount, cancellationToken) ?? 0;

    return new RevenueDashboardStatsDto
    {
      GrossPlatformRevenue = gross,
      PartnerSharePaid = partner,
      InfluencerSharePaid = influencerShare,
      PlatformRetained = retained,
      AllocationCount = count
    };
  }

  public async Task<GameDashboardStatsDto> GetGameStatsAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    if (filter.InfluencerId.HasValue)
    {
      var influencerQuery = FilterInfluencerCommissions(filter);
      var gamesPlayed = await influencerQuery
        .Select(x => x.GameSessionId)
        .Distinct()
        .CountAsync(cancellationToken);
      var attributedFees = await influencerQuery
        .SumAsync(x => (decimal?)x.GrossPlatformRevenue, cancellationToken) ?? 0;

      return new GameDashboardStatsDto
      {
        GamesPlayed = gamesPlayed,
        TotalPot = 0,
        PlatformCommission = attributedFees
      };
    }

    var query = FilterGameResults(filter);

    return new GameDashboardStatsDto
    {
      GamesPlayed = await query.CountAsync(cancellationToken),
      TotalPot = await query.SumAsync(x => (decimal?)x.TotalPot, cancellationToken) ?? 0,
      PlatformCommission = await query.SumAsync(x => (decimal?)x.PlatformCommission, cancellationToken) ?? 0
    };
  }

  public async Task<IReadOnlyList<PartnerRevenueBreakdownDto>> GetRevenueByPartnerAsync(
      DashboardStatsFilter filter,
      CancellationToken cancellationToken = default)
  {
    var rows = await FilterRevenueAllocations(filter)
      .GroupBy(a => a.LicensePartnerId)
      .Select(g => new
      {
        LicensePartnerId = g.Key,
        PartnerAmount = g.Sum(x => x.PartnerAmount),
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Count = g.Count()
      })
      .OrderByDescending(x => x.PartnerAmount)
      .ToListAsync(cancellationToken);

    if (rows.Count == 0)
      return [];

    var partnerIds = rows.Select(r => r.LicensePartnerId).ToList();
    var names = await db.LicensePartners.AsNoTracking()
      .Where(p => partnerIds.Contains(p.Id))
      .ToDictionaryAsync(p => p.Id, p => p.LegalName, cancellationToken);

    return rows
      .Select(x => new PartnerRevenueBreakdownDto
      {
        Key = x.LicensePartnerId.ToString(),
        Label = names.GetValueOrDefault(x.LicensePartnerId) ?? x.LicensePartnerId.ToString(),
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
    if (filter.InfluencerId.HasValue)
    {
      var influencerQuery = FilterInfluencerCommissions(filter);
      var count = await influencerQuery.CountAsync(cancellationToken);
      if (count == 0)
        return [];

      return
      [
        new PartnerRevenueBreakdownDto
        {
          Key = "GameCommission",
          Label = "Game commission",
          PartnerAmount = 0,
          InfluencerAmount = await influencerQuery.SumAsync(x => (decimal?)x.InfluencerAmount, cancellationToken) ?? 0,
          GrossPlatformRevenue = await influencerQuery.SumAsync(x => (decimal?)x.GrossPlatformRevenue, cancellationToken) ?? 0,
          TransactionCount = count
        }
      ];
    }

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
      .GroupBy(a => a.InfluencerId)
      .Select(g => new
      {
        InfluencerId = g.Key,
        InfluencerAmount = g.Sum(x => x.InfluencerAmount),
        Gross = g.Sum(x => x.GrossPlatformRevenue),
        Count = g.Count()
      })
      .OrderByDescending(x => x.InfluencerAmount)
      .ToListAsync(cancellationToken);

    if (rows.Count == 0)
      return [];

    var influencerIds = rows.Select(r => r.InfluencerId).ToList();
    var labels = await db.Influencers.AsNoTracking()
      .Where(i => influencerIds.Contains(i.Id))
      .Select(i => new { i.Id, i.DisplayName, i.Code })
      .ToDictionaryAsync(i => i.Id, cancellationToken);

    return rows
      .Select(x =>
      {
        var label = labels.TryGetValue(x.InfluencerId, out var info)
          ? $"{info.DisplayName} ({info.Code})"
          : x.InfluencerId.ToString();

        return new PartnerRevenueBreakdownDto
        {
          Key = x.InfluencerId.ToString(),
          Label = label,
          PartnerAmount = 0,
          InfluencerAmount = x.InfluencerAmount,
          GrossPlatformRevenue = x.Gross,
          TransactionCount = x.Count
        };
      })
      .ToList();
  }

  public async Task<IReadOnlyList<PartnerRevenueAllocationItemDto>> GetRevenueAllocationsAsync(
      DashboardStatsFilter filter,
      int take,
      CancellationToken cancellationToken = default)
  {
    if (filter.InfluencerId.HasValue)
    {
      var influencerAllocations = await FilterInfluencerCommissions(filter)
        .OrderByDescending(a => a.CreatedAt)
        .Take(take)
        .Select(a => new
        {
          a.Id,
          a.GameSessionId,
          a.GrossPlatformRevenue,
          a.CommissionPercent,
          a.InfluencerAmount,
          a.Currency,
          a.CreatedAt,
          CountryCode = a.Player.CountryCode ?? string.Empty
        })
        .ToListAsync(cancellationToken);

      return influencerAllocations
        .Select(a => new PartnerRevenueAllocationItemDto
        {
          Id = a.Id,
          SourceType = RevenueAllocationSourceType.GameCommission,
          SourceId = a.GameSessionId,
          CountryCode = a.CountryCode,
          CountryName = CountryCatalog.GetByCode(a.CountryCode)?.Name ?? a.CountryCode,
          GrossPlatformRevenue = a.GrossPlatformRevenue,
          PartnerSharePercent = a.CommissionPercent,
          PartnerAmount = 0,
          InfluencerAmount = a.InfluencerAmount,
          PlatformRetainedAmount = 0,
          Currency = a.Currency,
          CreatedAt = a.CreatedAt
        })
        .ToList();
    }

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

  private IQueryable<Domain.Entities.Player> FilterPlayers(DashboardStatsFilter filter)
  {
    var query = db.Players.AsNoTracking();

    if (filter.InfluencerId.HasValue)
    {
      var influencerId = filter.InfluencerId.Value;
      query = query.Where(p => db.InfluencerCodeRedemptions.Any(r =>
          r.InfluencerId == influencerId && r.PlayerId == p.Id));
    }

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

    if (filter.InfluencerId.HasValue)
    {
      var influencerId = filter.InfluencerId.Value;
      query = query.Where(p => db.InfluencerCodeRedemptions.Any(r =>
          r.InfluencerId == influencerId && r.PlayerId == p.PlayerId));
    }

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

    if (filter.InfluencerId.HasValue)
      query = query.Where(a => a.InfluencerId == filter.InfluencerId.Value);

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
