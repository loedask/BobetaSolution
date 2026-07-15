using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class InfluencerCommissionAllocationConfiguration : IEntityTypeConfiguration<InfluencerCommissionAllocation>
{
  public void Configure(EntityTypeBuilder<InfluencerCommissionAllocation> builder)
  {
    builder.ToTable("InfluencerCommissionAllocations");
    builder.HasKey(a => a.Id);
    builder.Property(a => a.GrossPlatformRevenue).HasPrecision(18, 2);
    builder.Property(a => a.AttributionBase).HasPrecision(18, 2);
    builder.Property(a => a.CommissionPercent).HasPrecision(5, 2);
    builder.Property(a => a.InfluencerAmount).HasPrecision(18, 2);
    builder.Property(a => a.Currency).HasMaxLength(3);
    builder.HasIndex(a => new { a.GameSessionId, a.PlayerId }).IsUnique();
    builder.HasIndex(a => a.InfluencerId);
    builder.HasOne(a => a.Influencer)
      .WithMany()
      .HasForeignKey(a => a.InfluencerId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(a => a.Player)
      .WithMany()
      .HasForeignKey(a => a.PlayerId)
      .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(a => a.GameSession)
      .WithMany()
      .HasForeignKey(a => a.GameSessionId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
