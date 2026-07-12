namespace Bobeta.Application.DTOs.Influencer;

public sealed record ApplyInfluencerCodeRequest(string Code);

public sealed record InfluencerCodeStatusDto(
    bool HasPendingCode,
    string? Code,
    string? InfluencerName,
    decimal DiscountPercent);
