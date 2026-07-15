using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class InfluencerConfiguration : IEntityTypeConfiguration<Influencer>
{
  public void Configure(EntityTypeBuilder<Influencer> builder)
  {
    builder.ToTable("Influencers");
    builder.HasKey(i => i.Id);
    builder.Property(i => i.DisplayName).HasMaxLength(200).IsRequired();
    builder.Property(i => i.ContactEmail).HasMaxLength(256).IsRequired();
    builder.Property(i => i.Code).HasMaxLength(32).IsRequired();
    builder.Property(i => i.CommissionPercent).HasPrecision(5, 2);
    builder.HasIndex(i => i.Code).IsUnique();
    builder.HasIndex(i => i.PortalUserId).IsUnique();
    builder.HasOne(i => i.PortalUser)
      .WithOne(u => u.Influencer)
      .HasForeignKey<Influencer>(i => i.PortalUserId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
