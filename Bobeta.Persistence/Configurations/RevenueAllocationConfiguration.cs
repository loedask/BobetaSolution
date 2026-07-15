using Bobeta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bobeta.Persistence.Configurations;

public class RevenueAllocationConfiguration : IEntityTypeConfiguration<RevenueAllocation>
{
  public void Configure(EntityTypeBuilder<RevenueAllocation> builder)
  {
    builder.ToTable("RevenueAllocations");
    builder.HasKey(a => a.Id);
    builder.Property(a => a.CountryCode).HasMaxLength(2).IsRequired();
    builder.Property(a => a.GrossPlatformRevenue).HasPrecision(18, 2);
    builder.Property(a => a.PartnerSharePercent).HasPrecision(5, 2);
    builder.Property(a => a.PartnerAmount).HasPrecision(18, 2);
    builder.Property(a => a.InfluencerAmount).HasPrecision(18, 2);
    builder.Property(a => a.PlatformRetainedAmount).HasPrecision(18, 2);
    builder.Property(a => a.Currency).HasMaxLength(3);
    builder.HasIndex(a => new { a.SourceType, a.SourceId });
    builder.HasOne(a => a.LicensePartner)
      .WithMany()
      .HasForeignKey(a => a.LicensePartnerId)
      .OnDelete(DeleteBehavior.Restrict);
  }
}
