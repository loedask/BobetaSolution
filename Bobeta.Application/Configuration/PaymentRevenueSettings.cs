namespace Bobeta.Application.Configuration;

/// <summary>Platform fee taken on MoMo deposits and withdrawals; partner share applies to this fee.</summary>
public class PaymentRevenueSettings
{
  public const string SectionName = "PaymentRevenue";

  /// <summary>Platform fee as % of deposit amount (e.g. 2.5 = 2.5%).</summary>
  public decimal DepositFeePercent { get; set; } = 2.5m;

  /// <summary>Platform fee as % of withdrawal amount.</summary>
  public decimal WithdrawalFeePercent { get; set; } = 2.5m;
}
