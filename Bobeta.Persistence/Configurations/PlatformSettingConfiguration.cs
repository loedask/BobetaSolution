using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
  public void Configure(EntityTypeBuilder<PlatformSetting> builder)
  {
    builder.ToTable("PlatformSettings");
    builder.HasKey(s => s.Key);
    builder.Property(s => s.Key).HasMaxLength(100);
    builder.Property(s => s.Value).HasMaxLength(500).IsRequired();
  }
}
