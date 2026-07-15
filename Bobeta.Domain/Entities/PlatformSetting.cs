namespace Bobeta.Domain.Entities;

/// <summary>Key/value platform configuration (e.g. influencer player discount %).</summary>
public class PlatformSetting
{
  public string Key { get; set; } = string.Empty;
  public string Value { get; set; } = string.Empty;
  public DateTime UpdatedAt { get; set; }
  public Guid? UpdatedByPortalUserId { get; set; }
}
