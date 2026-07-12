namespace Bobeta.Client.Models.Influencer;

public sealed class InfluencerCodeStatusViewModel
{
  public bool HasPendingCode { get; set; }
  public string? Code { get; set; }
  public string? InfluencerName { get; set; }
  public decimal DiscountPercent { get; set; }
}

public sealed class ApplyInfluencerCodeApiRequest
{
  public string Code { get; set; } = string.Empty;
}

internal sealed class InfluencerCodeStatusDto
{
  public bool HasPendingCode { get; set; }
  public string? Code { get; set; }
  public string? InfluencerName { get; set; }
  public decimal DiscountPercent { get; set; }
}
